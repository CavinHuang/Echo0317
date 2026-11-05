#!/usr/bin/env python3
"""Auditable offline preflight for the Unity project, NOT a Unity compilation."""
import datetime as dt
import hashlib
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[1]
DATA = ROOT / 'Assets/StreamingAssets/Data'


def checked(command):
    run = subprocess.run([sys.executable, str(ROOT / command)], cwd=ROOT,
                         capture_output=True, text=True, timeout=150)
    lines = [x for x in (run.stdout + run.stderr).splitlines() if x.strip()]
    return {'passed': run.returncode == 0, 'output': lines[-12:]}


def main():
    editor = os.environ.get('UNITY_EDITOR_PATH')
    can_run_unity = bool(editor and Path(editor).is_file())
    checks = {
        'story_graph': checked('Tests/test_export.py'),
        'media_and_prompts': checked('Tests/test_release_assets.py'),
        'csharp_syntax_surface_not_compile': checked('Tests/test_csharp_structure.py'),
        'internal_release_gate': checked('Tests/test_release_gate.py'),
    }
    tests = subprocess.run([sys.executable, '-m', 'unittest', 'discover', '-s', 'Tests',
                            '-p', 'test_stage11_*.py', '-v'], cwd=ROOT, capture_output=True, text=True, timeout=40)
    checks['release_gate_unit_tests'] = {'passed': tests.returncode == 0,
                                         'output': (tests.stdout + tests.stderr).splitlines()[-12:]}
    contracts = subprocess.run([sys.executable, '-m', 'unittest', 'discover', '-s', 'Tests',
                                '-p', 'test_stage12_*.py', '-v'], cwd=ROOT,
                               capture_output=True, text=True, timeout=40)
    checks['stage12_native_contracts'] = {'passed': contracts.returncode == 0,
                                          'output': (contracts.stdout + contracts.stderr).splitlines()[-15:]}
    qa_contracts = subprocess.run([sys.executable, '-m', 'unittest', 'discover', '-s', 'Tests',
                                   '-p', 'test_stage13_*.py', '-v'], cwd=ROOT,
                                  capture_output=True, text=True, timeout=40)
    checks['stage13_compiled_player_gate_contracts'] = {'passed': qa_contracts.returncode == 0,
        'output': (qa_contracts.stdout + qa_contracts.stderr).splitlines()[-17:]}
    for label, pattern in [('stage14_player_contracts', 'test_stage14_*.py'),
                           ('stage15_progression_safety', 'test_stage15_*.py')]:
        suite = subprocess.run([sys.executable, '-m', 'unittest', 'discover', '-s', 'Tests',
                                '-p', pattern, '-v'], cwd=ROOT, capture_output=True, text=True, timeout=40)
        checks[label] = {'passed': suite.returncode == 0,
                         'output': (suite.stdout + suite.stderr).splitlines()[-12:]}
    required = json.loads((DATA / 'release-required-shots.json').read_text(encoding='utf8'))['requiredShots']
    shots = {x['id']:x for x in json.loads((DATA / 'shots.json').read_text(encoding='utf8'))['shots']}
    unfinished = [sid for sid in required if shots[sid]['status'] != 'approved_final']
    previews = sorted((ROOT / 'Assets/StreamingAssets/Media/Previs').glob('*.mp4'))
    result = {
        'created_at_utc': dt.datetime.now(dt.timezone.utc).isoformat(),
        'project': '03:17 · 回声', 'stage': '15',
        'unity_editor_available': can_run_unity,
        'unity_editor_path': editor if can_run_unity else None,
        'native_csharp_compiler_available': any(shutil.which(x) for x in ('csc', 'mcs', 'dotnet')),
        'windows_native_build_executed': False,
        'unity_editmode_executed': False,
        'unity_playmode_executed': False,
        'compiled_windows_player_qa_executed': False,
        'checks': checks,
        'release': {
            'mandatory_shots': len(required),
            'unapproved_mandatory_shots': unfinished,
            'content_gate_passed': not unfinished,
            'native_release_approved': False,  # requires actual Editor tests, Windows build and QA

            'alpha_previews': [{'file': f.name, 'bytes': f.stat().st_size,
                                 'sha256': hashlib.sha256(f.read_bytes()).hexdigest()} for f in previews]
        }
    }
    out = ROOT / 'Builds'; out.mkdir(exist_ok=True)
    (out / 'stage15-preflight.json').write_text(json.dumps(result, ensure_ascii=False, indent=2)+'\n', encoding='utf8')
    print('Stage15 Unity preflight:')
    for name, check in checks.items():print(f"  {'PASS' if check['passed'] else 'FAIL'} {name}")
    print('  ' + ('AVAILABLE' if can_run_unity else 'NOT AVAILABLE') + ': Unity Editor executable')
    print('  NOT RUN: Unity EditMode / PlayMode / native Windows build / compiled Player QA')
    print(f"  RELEASE BLOCKED: {len(unfinished)}/{len(required)} required films unapproved")
    print(f"  generated: {out / 'stage15-preflight.json'}")
    return 0 if all(c['passed'] for c in checks.values()) else 1


if __name__ == '__main__':
    sys.exit(main())
