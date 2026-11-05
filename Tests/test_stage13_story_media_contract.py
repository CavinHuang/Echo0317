"""Story/media cross-checks for branch-specific video IDs.

This verifies the shipped data and the selection logic contract. It is not a
substitute for frame-by-frame verification of a compiled Unity player.
"""
import json
import pathlib
import re
import unittest

ROOT=pathlib.Path(__file__).resolve().parents[1]
DATA=ROOT/'Assets/StreamingAssets/Data'

class StoryMediaContract(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.graph=json.loads((DATA/'story.json').read_text())
        cls.shots=json.loads((DATA/'shots.json').read_text())
        cls.required=json.loads((DATA/'release-required-shots.json').read_text())
        cls.clip_ids={shot['id'] for shot in cls.shots['shots']}

    def test_branch_specific_footage_has_individual_production_tasks(self):
        for id in ['V07P','V07S','V13A','V13B','V17A','V17B','V24','V24B']:
            self.assertIn(id,self.clip_ids)
            self.assertTrue((ROOT/'Production/Stage4/prompts'/f'{id}.md').is_file())

    def test_required_video_gate_reflects_branch_assets(self):
        required=set(self.required['requiredShots'])
        for id in ['V07P','V07S','V13A','V13B','V17A','V17B','V24B']:
            self.assertIn(id,required)
        for virtual in ['V07','V13','V17']:
            self.assertNotIn(virtual,required)

    def test_each_story_node_has_solvable_video_mapping(self):
        nodes=self.graph['nodes']
        parent={'V07':{'V07P','V07S'},'V13':{'V13A','V13B'},'V17':{'V15','V17A','V17B'}}
        for node in nodes:
            base=node['clip']
            if base not in parent and base not in self.clip_ids:
                self.fail(f"{node['id']}: unknown video {base}")
        source=(ROOT/'Assets/Scripts/Runtime/EchoGame.cs').read_text()
        for token in ('c1_password', 'pressed_lock','V24B','incident_proof','V07S','c4_door','V13A'):
            self.assertIn(token,source)

if __name__=='__main__': unittest.main()
