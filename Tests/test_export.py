"""Independent source/data parity and migration integrity test; requires only Python 3."""
import json, pathlib, subprocess, collections, sys
p=pathlib.Path(__file__).resolve().parents[1]
files=p/'Assets/StreamingAssets/Data'
graph=json.loads((files/'story.json').read_text())
cases=json.loads((files/'cases.json').read_text())
shots=json.loads((files/'shots.json').read_text())
tracks=json.loads((files/'captions.json').read_text())
assert len(graph['nodes'])==55
assert len(graph['evidence'])==25
assert len(graph['media'])==24
assert len(graph['chapters'])==5
assert len(cases['rules'])==5
assert len(shots['shots'])==43  # 40 actionable + V07/V13/V17 parent grouping IDs
assert len(tracks['tracks'])==8
nodes={n['id']:n for n in graph['nodes']}
evidence={e['id'] for e in graph['evidence']}
assert len(nodes)==55 and nodes['intro']['chapterIndex']==1
for n in nodes.values():
    assert 1<=n['chapterIndex']<=5
    assert len(n['lines'])>=2
    assert any(m['id']==n['clip'] for m in graph['media'])
    choices=n['choices'] or ([{'to':n['next']}] if n.get('next') else [])
    assert n.get('ending') or choices
    for c in choices:
        assert c['to'] in nodes
        for flag in c.get('requires',[])+c.get('needsAny',[])+c.get('forbids',[])+([c['flag']] if c.get('flag') else []):
            assert flag in evidence
    for f in n.get('entryFlags',[]):assert f in evidence

def sig(n,flags):return n+'|'+','.join(sorted(flags))
queue=collections.deque([('intro',frozenset())]);seen=set();reachable=set();endings=set();n_edges=0
while queue:
    current, flags=queue.popleft()
    key=sig(current,flags)
    if key in seen:continue
    seen.add(key); reachable.add(current)
    node=nodes[current]
    if node.get('ending'):endings.add(current);continue
    for c in node.get('choices',[]) or [{'to':node['next']}]:
        if not set(c.get('requires',[])).issubset(flags):continue
        if c.get('needsAny') and not (set(c['needsAny'])&flags):continue
        if set(c.get('forbids',[]))&flags:continue
        ns=set(flags)
        if c.get('flag'):ns.add(c['flag'])
        ns.update(nodes[c['to']].get('entryFlags',[]))
        if c['to']=='c5_sign':
            assert {'incident_proof','cache46','source_verified','protocol_read','lin_willing'}.issubset(ns)
            assert ('shadow' in ns or 'render_log' in ns)
            assert 'coerced' not in ns or 'amends' in ns
        n_edges+=1
        queue.append((c['to'],frozenset(ns)))
assert len(seen)==69497,(len(seen),n_edges)
assert reachable==set(nodes)
assert endings=={'ending_merge','ending_loop','ending_stop','ending_audit'}
print('PASS: 55 nodes / 69,497 reachable states / 4 endings / 5 rules / 43 shot IDs (40 work orders)')
# The generated data, not an independently reauthored story, is the authoritative input.
original=pathlib.Path(p/'Tools/story.js').read_text()
assert 'window.FILM' in original
for id in nodes:
    assert '"'+id+'"' in original
sample=json.loads((files/'stage8-sample-save.json').read_text())
assert sample['state']['node']=='c1_peephole' and 'shadow' in sample['state']['flags']
for path in (p/'Assets/StreamingAssets/Media/Previs').glob('*.mp4'):
    assert path.stat().st_size>20000, path
assert len(list((p/'Assets/StreamingAssets/Media/Previs').glob('*.mp4')))==8
print('PASS: data, sample legacy save, and 8 offline video previews')
