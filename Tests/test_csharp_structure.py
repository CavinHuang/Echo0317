"""C# static syntax-surface checks. NOT a substitute for Unity/real Roslyn compilation."""
import pathlib,re,sys
root=pathlib.Path(__file__).resolve().parents[1]
files=list((root/'Assets/Scripts').rglob('*.cs'))+list((root/'Assets/Editor').rglob('*.cs'))+list((root/'Assets/Tests').rglob('*.cs'))
try:
 from pygments import lex
 from pygments.lexers import CSharpLexer
 from pygments.token import Comment, String
except ImportError:
 print('WARN: Pygments unavailable; C# structural lexer skipped');sys.exit(0)
for f in files:
 stack=[]
 for token,value in lex(f.read_text(),CSharpLexer()):
  if token in Comment or token in String:continue
  for char in value:
   if char in '{[(':
    stack.append(char)
   if char in '}])':
    assert stack and {')':'(',']':'[','}':'{'}[char]==stack.pop(),(f,char,len(stack))
 assert not stack,(f,stack)
print('PASS:',len(files),'C# source files have balanced bracket structure (not compiled)')
