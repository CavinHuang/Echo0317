"""Optional C# lexical bracket audit; not a Unity or Roslyn compilation test."""
from pathlib import Path
import unittest

try:
    from pygments import lex
    from pygments.lexers import CSharpLexer
    from pygments.token import Comment, String
except ImportError:
    lex = None


class CSharpSurfaceTests(unittest.TestCase):
    @unittest.skipUnless(lex is not None, 'Pygments unavailable; lexical audit skipped')
    def test_balanced_code_brackets(self):
        root = Path(__file__).resolve().parents[1]
        files = (list((root/'Assets/Scripts').rglob('*.cs')) +
                 list((root/'Assets/Editor').rglob('*.cs')) +
                 list((root/'Assets/Tests').rglob('*.cs')))
        self.assertGreater(len(files), 5)
        pairs = {')':'(', ']':'[', '}':'{'}
        for file in files:
            stack = []
            for token, value in lex(file.read_text(encoding='utf-8'), CSharpLexer()):
                if token in Comment or token in String:
                    continue
                for char in value:
                    if char in '{[(':
                        stack.append(char)
                    elif char in '}])':
                        self.assertTrue(stack, f'{file}: unmatched {char}')
                        self.assertEqual(stack.pop(), pairs[char], f'{file}: mismatched {char}')
            self.assertFalse(stack, f'{file}: unclosed brackets {stack}')


if __name__ == '__main__':
    unittest.main()