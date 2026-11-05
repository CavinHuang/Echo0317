#!/usr/bin/env python3
"""Package a REAL GameCI-built Windows Unity Player after its EXE native QA.

A cross-built EXE is not a native-verified Alpha until the downloaded EXE
runs under Windows and produces the expected diagnostics. This script
intentionally cannot emulate Unity or synthesize the QA inputs.
GUI/audio/video remain pending an interactive Windows 11 session.
"""
import argparse
import datetime as dt
import hashlib
import json
import pathlib
import struct
import subprocess
import sys
import zipfile

REQUIRED_FILES = (
    'Echo0317.exe', 'UnityPlayer.dll',
    'Echo0317_Data/StreamingAssets/Data/story.json',
    'Echo0317_Data/StreamingAssets/Data/cases.json',
    'Echo0317_Data/StreamingAssets/Data/shots.json',
    'Echo0317_Data/StreamingAssets/Data/verified-ending-paths.json',
)


def require(value, message):
    if not value:
        raise ValueError(message)


def digest(path):
    result = hashlib.sha256()
    with path.open('rb') as stream:
        for chunk in iter(lambda: stream.read(1 << 20), b''):
            result.update(chunk)
    return result.hexdigest()


def validate_pe_amd64(file):
    with file.open('rb') as source:
        header = source.read(64)
        require(len(header) == 64 and header[:2] == b'MZ', 'EXE is not a Windows PE program')
        pe_offset = struct.unpack_from('<I', header, 60)[0]
        require(64 <= pe_offset < 4096, 'Invalid Windows PE header offset')
        source.seek(pe_offset)
        pe_header = source.read(6)
    require(pe_header[:4] == b'PE\0\0', 'EXE lacks Windows PE signature')
    require(struct.unpack_from('<H', pe_header, 4)[0] == 0x8664, 'EXE is not x64 (AMD64)')


def run(root):
    output = root / 'Builds'
    source = output / 'Windows'
    exe = source / 'Echo0317.exe'
    evidence = output / 'hosted-alpha-evidence.json'
    output.mkdir(parents=True, exist_ok=True)
    try:
        require(exe.is_file() and exe.stat().st_size >= 102400, 'No genuine compiled EXE')
        validate_pe_amd64(exe)
        for rel in REQUIRED_FILES:
            require((source / rel).is_file(), 'Windows build missing: ' + rel)
        for filename in ('story.json', 'cases.json', 'shots.json', 'verified-ending-paths.json'):
            built = source / 'Echo0317_Data/StreamingAssets/Data' / filename
            original = root / 'Assets/StreamingAssets/Data' / filename
            require(original.is_file() and digest(built) == digest(original),
                    'Built game data differ from checked sources: ' + filename)
        for name in ('V01', 'V02', 'V03', 'V04', 'V05', 'V23', 'V24', 'V24B'):
            file = source / 'Echo0317_Data/StreamingAssets/Media/Previs' / (name + '.mp4')
            require(file.is_file() and file.stat().st_size >= 1024,
                    'Preview video absent from compiled player: ' + name)
        qa = output / 'native-game-qa.json'
        log = output / 'native-game-qa.log'
        require(qa.is_file() and log.is_file(), 'Compiled EXE has not written a native QA report')
        require('ECHO_NATIVE_QA_PASS' in log.read_text(encoding='utf-8-sig', errors='replace'),
                'Missing success marker from compiled Windows Player')
        validation = subprocess.run([sys.executable, str(root / 'Tools/check-native-qa.py'), str(qa)],
                                    capture_output=True, text=True, timeout=30)
        require(validation.returncode == 0,
                'Native EXE game QA did not pass: ' + validation.stderr)
        native = json.loads(qa.read_text(encoding='utf-8-sig'))
        require(native.get('result') == 'pass', 'Native EXE report not successful')
        require(native.get('endingsVerified') == 4 and native.get('deadEndStates') == 0,
                'Native game route or softlock audit incomplete')
        files = sorted(file for file in source.rglob('*') if file.is_file())
        require(len(files) >= 8, 'Unity player runtime lacks expected files')
        manifest = output / 'hosted-alpha-sha256.txt'
        lines = []
        for file in files:
            rel = file.relative_to(source).as_posix()
            lines.append(digest(file) + '  ' + rel)
        manifest.write_text('\n'.join(lines) + '\n', encoding='utf-8')
        archive = output / 'Echo0317-Windows-x64-internal-hosted.zip'
        with zipfile.ZipFile(archive, 'w', allowZip64=True) as package:
            for file in files:
                rel = file.relative_to(source).as_posix()
                compression = zipfile.ZIP_STORED if file.suffix.lower() in ('.mp4', '.png', '.dll') else zipfile.ZIP_DEFLATED
                package.write(file, arcname=rel, compress_type=compression)
        with zipfile.ZipFile(archive) as package:
            require(package.testzip() is None, 'ZIP package contains a corrupt member')
            require({p.relative_to(source).as_posix() for p in files} == set(package.namelist()),
                    'Distributable differs from compiled Player directory')
        result = {
            'schema': 'echo0317-hosted-alpha-v1', 'result': 'pass',
            'compiledWindowsPlayerPresent': True, 'compiledPlayerFourEndingQaPassed': True,
            'interactiveDesktopStartupVerified': False, 'releaseContentApproved': False,
            'unityVersion': native.get('unityVersion'), 'gameplayStates': native.get('graphStatesVerified'),
            'endingsVerified': native.get('endingsVerified'), 'files': len(files),
            'exeSha256': digest(exe), 'zipSha256': digest(archive),
            'verifiedUtc': dt.datetime.now(dt.timezone.utc).isoformat(),
            'notice': 'GameCI Unity x64 Player + native Windows headless QA; real GUI/video/audio still pending.'
        }
        evidence.write_text(json.dumps(result, ensure_ascii=False, indent=2)+'\n', encoding='utf-8')
        print('PASS: compiled Unity Windows EXE, headless story QA, checksums and distributable ZIP')
        print('PENDING: live desktop GUI and film/audio playback (requires interactive Windows).')
        return 0
    except (ValueError, OSError, KeyError, json.JSONDecodeError, subprocess.TimeoutExpired) as error:
        evidence.write_text(json.dumps({
            'schema': 'echo0317-hosted-alpha-v1', 'result': 'fail',
            'compiledPlayerFourEndingQaPassed': False, 'error': str(error),
            'checkedUtc': dt.datetime.now(dt.timezone.utc).isoformat()
        }, ensure_ascii=False, indent=2)+'\n', encoding='utf-8')
        print('BLOCKED: '+str(error), file=sys.stderr)
        return 1


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--root', type=pathlib.Path, default=pathlib.Path(__file__).resolve().parents[1])
    args = parser.parse_args()
    sys.exit(run(args.root.resolve()))
