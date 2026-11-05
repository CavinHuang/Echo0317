#!/usr/bin/env python3
"""Validate artifacts of an actual Unity Windows build; never simulate success.

This script cannot run the build. It fails closed unless Editor XML, EXE,
Player runtime QA, bundle hash manifest and optionally GUI smoke ALL exist.
"""
import argparse
import datetime as dt
import hashlib
import json
import pathlib
import re
import subprocess
import sys
import zipfile
import xml.etree.ElementTree as ET


def require(condition, reason):
    if not condition:
        raise ValueError(reason)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--root', type=pathlib.Path, default=pathlib.Path(__file__).parents[1])
    parser.add_argument('--require-smoke', action='store_true')
    args = parser.parse_args()
    root = args.root.resolve()
    builds = root / 'Builds'
    builds.mkdir(parents=True, exist_ok=True)
    exe = builds / 'Windows' / 'Echo0317.exe'
    report_out = builds / 'stage16-alpha-evidence.json'
    try:
        editor = json.loads((builds / 'unity-import-verification.json').read_text(encoding='utf-8-sig'))
        require(editor.get('nativeEditorLoaded') is True and editor.get('nodes') == 55,
                'Editor import never produced a valid content report')
        require(re.match(r'^6000\.3\.', editor.get('unityVersion', '')) is not None,
                'Unity Editor is not 6.3 LTS')
        suites = {}
        for name, minimum in [('EditMode', 5), ('PlayMode', 2)]:
            suite = ET.parse(builds / (name + '-results.xml')).getroot()
            require(suite.tag == 'test-run', 'Unexpected ' + name + ' NUnit report')
            passed, failed = int(suite.attrib.get('passed', '-1')), int(suite.attrib.get('failed', '-1'))
            require(passed >= minimum and failed == 0, name + ' tests did not pass')
            suites[name] = {'passed': passed, 'failed': failed}
        require(exe.is_file() and exe.stat().st_size > 100 * 1024,
                'The native Windows player EXE has not been built')
        dll = builds / 'Windows' / 'UnityPlayer.dll'
        require(dll.is_file(), 'UnityPlayer.dll absent')
        assets = builds / 'Windows' / 'Echo0317_Data' / 'StreamingAssets' / 'Data'
        require((assets / 'story.json').is_file(), 'Built player missing its story database')
        require((assets / 'cases.json').is_file(), 'Built player missing its evidence cases')
        require((assets / 'verified-ending-paths.json').is_file(), 'Built player missing QA routes')
        native = builds / 'native-game-qa.json'
        validator = pathlib.Path(__file__).parent / 'check-native-qa.py'
        result = subprocess.run([sys.executable, str(validator), str(native)],
                                capture_output=True, text=True, timeout=20)
        require(result.returncode == 0, 'Compiled player runtime QA failed: ' + result.stderr.strip())
        require('ECHO_NATIVE_QA_PASS' in (builds / 'native-game-qa.log').read_text(encoding='utf-8-sig', errors='replace'),
                'No success marker from compiled native player')
        manifest = builds / 'windows-build-sha256.txt'
        lines = manifest.read_text(encoding='utf-8-sig').splitlines()
        require(len(lines) > 6, 'Distribution manifest is missing or unexpectedly short')
        seen = set()
        for line in lines:
            sha, separator, relative = line.partition('  ')
            require(separator == '  ' and re.fullmatch(r'[A-Fa-f0-9]{64}', sha) and relative,
                    'Malformed SHA-256 manifest line')
            require('..' not in pathlib.PurePosixPath(relative).parts, 'Unsafe manifest path')
            file = builds / 'Windows' / pathlib.Path(*pathlib.PurePosixPath(relative).parts)
            require(file.is_file(), 'Distribution missing ' + relative)
            require(hashlib.sha256(file.read_bytes()).hexdigest() == sha.lower(),
                    'Distribution checksum mismatch: ' + relative)
            seen.add(relative)
        require('Echo0317.exe' in seen and 'UnityPlayer.dll' in seen,
                'Player files not included in distribution checksum manifest')
        archive = builds / 'Echo0317-Windows-x64-internal.zip'
        require(archive.is_file() and archive.stat().st_size > 1024 * 1024,
                'Windows Alpha distributable ZIP was not generated')
        require(archive.stat().st_mtime >= exe.stat().st_mtime,
                'Windows ZIP is older than the freshly built EXE')
        with zipfile.ZipFile(archive) as packaged:
            zipped = {x.filename.rstrip('/').replace('\\', '/') for x in packaged.infolist() if not x.is_dir()}
            require(seen.issubset(zipped), 'Distribution ZIP is missing files in SHA256 manifest')
            require(packaged.testzip() is None, 'Windows ZIP has a corrupt member')
        smoke = None
        smoke_file = builds / 'player-smoke-success.json'
        if smoke_file.exists():
            smoke = json.loads(smoke_file.read_text(encoding='utf-8-sig'))
            require(smoke.get('schema') == 'echo0317-interactive-smoke-v1' and smoke.get('result') == 'pass',
                    'Invalid interactive desktop smoke report')
            require(smoke_file.stat().st_mtime >= exe.stat().st_mtime,
                    'Interactive smoke report is older than the Windows build')
        if args.require_smoke:
            require(smoke is not None, 'Real GUI startup has not passed on the new EXE')
        evidence = {
            'schema': 'echo0317-stage16-alpha-evidence-v1',
            'result': 'pass',
            'nativeWindowsBuildVerified': True,
            'compiledPlayerFourEndingQaPassed': True,
            'interactiveDesktopStartupVerified': smoke is not None,
            'unityVersion': editor['unityVersion'],
            'editorTests': suites,
            'fileCount': len(seen),
            'sha256Exe': hashlib.sha256(exe.read_bytes()).hexdigest(),
            'sha256Bundle': hashlib.sha256(archive.read_bytes()).hexdigest(),
            'completedUtc': dt.datetime.now(dt.timezone.utc).isoformat(),
            'evidenceSource': 'real Windows Unity Editor/Player files (validated, not synthesized)'
        }
        report_out.write_text(json.dumps(evidence, ensure_ascii=False, indent=2) + '\n', encoding='utf8')
        print('PASS: actual Unity Windows Alpha build, Editor tests and compiled Player QA verified')
        if smoke is None: print('PENDING: GUI startup smoke; Windows Player QA is headless')
        else: print('PASS: interactive Windows game startup smoke')
        return 0
    except (OSError, KeyError, ValueError, ET.ParseError, json.JSONDecodeError, subprocess.TimeoutExpired) as e:
        # If a previous valid report exists, invalidate it rather than leave stale evidence.
        report_out.write_text(json.dumps({
            'schema': 'echo0317-stage16-alpha-evidence-v1', 'result': 'fail',
            'nativeWindowsBuildVerified': False, 'error': str(e),
            'checkedUtc': dt.datetime.now(dt.timezone.utc).isoformat()
        }, ensure_ascii=False, indent=2) + '\n', encoding='utf8')
        print('BLOCKED: ' + str(e), file=sys.stderr)
        return 1


if __name__ == '__main__':
    sys.exit(main())
