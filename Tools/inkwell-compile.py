"""Compile current Unity runtime/editor sources to Logs only; never writes Library assemblies."""
from pathlib import Path
import subprocess, re

root = Path(__file__).resolve().parent.parent
out = root / 'Logs' / 'inkwell-review'
out.mkdir(parents=True, exist_ok=True)
data = Path('C:/Program Files/Unity/Hub/Editor/6000.6.0f1/Editor/Data')
runtime = root / 'Library/Bee/artifacts/1900b0aE.dag/Assembly-CSharp.rsp'
for kind in ('runtime', 'editor'):
    path = runtime if kind == 'runtime' else runtime.with_name('Assembly-CSharp-Editor.rsp')
    text = path.read_text(encoding='utf-8-sig')
    normalized = text.replace('\\', '/')
    dirs = ['Assets/RoyalAves/Scripts'] if kind == 'runtime' else ['Assets/RoyalAves/Editor']
    for directory in dirs:
        for source in (root / directory).rglob('*.cs'):
            rel = source.relative_to(root).as_posix()
            if '"'+rel+'"' not in normalized:
                text += '\n"'+rel+'"'
    text = re.sub(r'^-out:.*$', '-out:"'+(out / ('Assembly-CSharp' + ('-Editor' if kind == 'editor' else '') + '.dll')).as_posix()+'"', text, flags=re.M)
    text = re.sub(r'^-refout:.*$', '', text, flags=re.M)
    if kind == 'editor':
        text = re.sub(r'-r:"[^"]*/Assembly-CSharp(?:\.ref)?\.dll"', '-r:"'+(out/'Assembly-CSharp.dll').as_posix()+'"', text)
    rsp = out/(kind+'.rsp'); rsp.write_text(text,encoding='utf-8')
    result = subprocess.run([str(data/'NetCoreRuntime/dotnet.exe'),str(data/'DotNetSdk/sdk/8.0.318/Roslyn/bincore/csc.dll'),'@'+str(rsp)],cwd=root,capture_output=True,text=True)
    log = result.stdout+result.stderr
    (out/(kind+'-compile.log')).write_text(log,encoding='utf-8')
    errors = [line for line in log.splitlines() if 'error ' in line]
    print(kind, 'exit', result.returncode, 'errors', len(errors))
    for line in errors[:15]: print(line)
    if result.returncode: break
