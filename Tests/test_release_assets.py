"""Editor-free native release preflight. Does not claim to compile C# or run Unity."""
import json
import pathlib
import shutil
import subprocess

root = pathlib.Path(__file__).resolve().parents[1]
assets = root / 'Assets' / 'StreamingAssets'
data = assets / 'Data'
production = root / 'Production' / 'Stage4'
story = json.loads((data / 'story.json').read_text(encoding='utf8'))
shots = json.loads((data / 'shots.json').read_text(encoding='utf8'))['shots']
paths = json.loads((data / 'verified-ending-paths.json').read_text(encoding='utf8'))['paths']
parents = {'V07', 'V13', 'V17'}
ids = {s['id'] for s in shots}
assert len(ids) == len(shots) == 43, 'duplicate shot IDs'
assert len(paths) == 4 and {p['ending'] for p in paths} == {
    'ending_merge', 'ending_loop', 'ending_stop', 'ending_audit'
}
assert all(s['status'] in ('not_generated', 'draft_final', 'approved_final') for s in shots)
for s in shots:
    if s['id'] in parents:
        assert s['status'] != 'approved_final', 'parent grouping shot may not be a final asset'
    if s['status'] == 'approved_final':
        name = s['path']
        assert name and pathlib.PurePath(name).name == name and '..' not in name
        assert (assets / 'Media' / 'Final' / name).is_file(), 'approved asset is missing'

prompts = {x.stem for x in (production / 'prompts').glob('*.md')}
assert ids - parents == prompts, f'prompt mismatch: {ids - parents - prompts}, {prompts - ids}'
for sid in prompts:
    body = (production / 'prompts' / (sid + '.md')).read_text(encoding='utf8')
    assert len(body) > 400, f'prompt too short for {sid}'

nodes = {n['id']: n for n in story['nodes']}
for p in paths:
    node_ids = p['nodes']
    assert node_ids[0] == 'intro' and node_ids[-1] == p['ending']
    flags = set()
    for source, target in zip(node_ids, node_ids[1:]):
        candidates = nodes[source].get('choices', []) or ([{'to': nodes[source]['next']}]
            if nodes[source].get('next') else [])
        eligible = [c for c in candidates if c['to'] == target
                    and set(c.get('requires', [])).issubset(flags)
                    and (not c.get('needsAny') or set(c['needsAny']) & flags)
                    and not set(c.get('forbids', [])) & flags]
        assert len(eligible) == 1, f'{p["ending"]}: blocked at {source}->{target}'
        choice = eligible[0]
        if choice.get('flag'): flags.add(choice['flag'])
        flags.update(nodes[target].get('entryFlags', []))
    assert nodes[node_ids[-1]].get('ending') is True

previs = assets / 'Media' / 'Previs'
expected = {'V01', 'V02', 'V03', 'V04', 'V05', 'V23', 'V24', 'V24B'}
assert {f.stem for f in previs.glob('*.mp4')} == expected
for f in previs.glob('*.mp4'):
    assert f.stat().st_size > 20000, f'empty video {f.name}'
if shutil.which('ffprobe'):
    for f in previs.glob('*.mp4'):
        info = json.loads(subprocess.check_output(['ffprobe', '-v', 'error', '-show_entries',
            'format=duration:stream=codec_name,codec_type,width,height', '-of', 'json', str(f)]))
        codecs = {s['codec_type']: s['codec_name'] for s in info['streams']}
        assert codecs.get('video') == 'h264' and codecs.get('audio') == 'aac', f.name
        assert float(info['format']['duration']) > 2, f.name
    print('PASS: 8 MP4 files are H.264/AAC with plausible durations (ffprobe)')
else:
    print('WARN: ffprobe missing; tested only file presence, not codec metadata')
assert (root / 'Assets/Tests/EditMode/EchoJourneyTests.cs').is_file()
assert (root / 'Assets/Tests/PlayMode/EchoPlaybackSmokeTests.cs').is_file()
assert (root / 'Tools/build-windows.ps1').is_file()
print('PASS: 4 playable exported routes, 43 shot IDs (40 prompts + 3 parents), 8 previews, Unity test and build inputs')
