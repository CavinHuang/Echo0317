"""Focused executable tests for the Stage 11 release prerequisites.

These run in the container without installing Unity and deliberately do NOT
claim to have compiled or run the native C# application.
"""
import importlib.util
import pathlib
import unittest

HERE = pathlib.Path(__file__).resolve().parent
spec = importlib.util.spec_from_file_location('release_gate', HERE / 'test_release_gate.py')
release_gate = importlib.util.module_from_spec(spec)
spec.loader.exec_module(release_gate)


class ReleaseGateTests(unittest.TestCase):
    def test_internal_build_can_use_existing_previs(self):
        required, missing = release_gate.inspect('internal')
        self.assertEqual(len(required), 27)
        self.assertEqual(missing, [])

    def test_release_rejects_all_unapproved_mandatory_shots(self):
        required, missing = release_gate.inspect('release')
        self.assertEqual(len(required), 27)
        self.assertEqual(set(missing), required)

    def test_grouping_ids_are_not_generation_jobs(self):
        required, _ = release_gate.inspect('internal')
        self.assertFalse(set(['V07', 'V13', 'V17']) & required)
        self.assertTrue(set(['V07P', 'V07S', 'V13A', 'V13B', 'V17A', 'V17B', 'V24B']) <= required)


if __name__ == '__main__':
    unittest.main()
