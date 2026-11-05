"""Validate the Unity release/alpha split without Unity Editor.

The gate intentionally rejects RELEASE while required cinematic media is missing.
This is a preflight only; Editor-side EchoBuild.VerifyContent is authoritative.
"""
import argparse
import json
import pathlib
import sys

ROOT = pathlib.Path(__file__).resolve().parents[1]
DATA = ROOT / 'Assets' / 'StreamingAssets' / 'Data'
MEDIA_FINAL = ROOT / 'Assets' / 'StreamingAssets' / 'Media' / 'Final'


def inspect(profile: str):
    story = json.loads((DATA / 'story.json').read_text(encoding='utf8'))
    shots = json.loads((DATA / 'shots.json').read_text(encoding='utf8'))['shots']
    requirements = json.loads((DATA / 'release-required-shots.json').read_text(encoding='utf8'))
    all_ids = {item['id'] for item in shots}
    registered = {item['id']: item for item in shots}
    required = set(requirements['requiredShots'])
    core = {node['clip'] for node in story['nodes']} - {'V07', 'V13', 'V17'}
    variants = {'V07P', 'V07S', 'V13A', 'V13B', 'V17A', 'V17B', 'V24B'}
    assert requirements['schema'] == 1, 'unsupported release manifest'
    assert core | variants == required, 'release manifest differs from story + runtime variants'
    assert len(required) == 27 and required <= all_ids
    assert len(shots) == 43
    if profile == 'internal':
        previews = list((ROOT / 'Assets' / 'StreamingAssets' / 'Media' / 'Previs').glob('*.mp4'))
        assert len(previews) == 8, 'internal alpha lacks eight previs assets'
        return required, []

    missing = []
    for shot_id in sorted(required):
        info = registered[shot_id]
        path = info.get('path', '')
        legal_name = path and pathlib.PurePath(path).name == path and '/' not in path and '\\' not in path and '..' not in path
        if info.get('status') != 'approved_final' or not legal_name or not (MEDIA_FINAL / path).is_file():
            missing.append(shot_id)
    return required, missing


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--profile', choices=['internal', 'release'], default='internal')
    args = parser.parse_args()
    required, missing = inspect(args.profile)
    if missing:
        print('BLOCKED: release profile requires approved cinematic assets for ' + str(len(missing)) + ' shots')
        print('missing/unapproved: ' + ', '.join(missing))
        return 1
    print('PASS: ' + args.profile.upper() + ' profile, ' + str(len(required)) + ' mandatory story-visible shots')
    return 0


if __name__ == '__main__':
    sys.exit(main())
