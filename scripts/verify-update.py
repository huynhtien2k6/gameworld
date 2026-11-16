"""Check updater failure paths using fake clients; never contact Docker or AWS."""
import json
import os
from pathlib import Path
import subprocess
import tempfile

updater = Path(__file__).resolve().parents[1] / 'deploy' / 'update.sh'
client = r'''#!/usr/bin/env python3
import json, os, sys
from pathlib import Path
kind = Path(sys.argv[0]).name
args = sys.argv[1:]
with open(os.environ['GW_TEST_LOG'], 'a') as log:
    log.write(json.dumps([kind, *args]) + '\n')
mode = os.environ['GW_TEST_MODE']
if kind == 'git':
    if args[0] == 'diff' and mode == 'dirty': sys.exit(1)
    if args[0] == 'branch': print('main')
    if args[0] == 'log': print('fixture Validated source')
elif kind == 'sudo':
    assert args[:2] == ['-n', 'docker']
    if 'inspect' in args: print('sha256:previous')
    elif 'ps' in args and '-q' in args: print('previous-container')
    elif 'build' in args and mode == 'build-fail': sys.exit(1)
    elif 'up' in args and mode == 'rollout-fail':
        marker = Path(os.environ['GW_TEST_LOG'] + '.rollout')
        if not marker.exists(): marker.touch(); sys.exit(1)
elif kind == 'curl' and mode == 'health-fail': sys.exit(22)
'''

for mode in ('healthy', 'dirty', 'build-fail', 'rollout-fail', 'health-fail'):
    with tempfile.TemporaryDirectory(prefix='gameworld-update-') as temp:
        root = Path(temp)
        (root/'deploy').mkdir()
        (root/'GameWorld.Api'/'wwwroot').mkdir(parents=True)
        (root/'GameWorld.Api'/'wwwroot'/'characters.html').write_text('fixture')
        original = 'private fixture: must never be printed or replaced'
        (root/'deploy'/'.env').write_text(original)
        binary = root/'bin'
        binary.mkdir()
        for tool in ('git', 'sudo', 'curl', 'sleep'):
            path = binary/tool
            path.write_text(client)
            path.chmod(0o700)
        log = root/'calls.jsonl'
        result = subprocess.run(['bash', str(updater)], text=True, capture_output=True, timeout=20,
            env={**os.environ, 'PATH': str(binary)+os.pathsep+os.environ['PATH'],
                 'GW_REPO_DIR': str(root), 'GW_TEST_LOG': str(log), 'GW_TEST_MODE': mode})
        calls = [json.loads(line) for line in log.read_text().splitlines()]
        starts = [call for call in calls if call[0] == 'sudo' and 'up' in call]
        rollbacks = [call for call in calls if call[:4] == ['sudo', '-n', 'docker', 'tag']]
        assert result.returncode == (0 if mode == 'healthy' else 1), (mode, result.stdout, result.stderr)
        assert (root/'deploy'/'.env').read_text() == original
        assert original not in result.stdout+result.stderr
        assert not any('down' in call for call in calls)
        if mode in ('dirty', 'build-fail'): assert not starts
        if mode == 'healthy':
            assert len(starts) == 1 and not rollbacks
            assert any('/characters.html' in ' '.join(call) for call in calls if call[0] == 'curl')
        if mode in ('rollout-fail', 'health-fail'):
            assert len(starts) == 2 and len(rollbacks) == 1
            assert rollbacks[0][-2:] == ['sha256:previous', 'gameworld:local']
        print('PASS:', mode, '(config preserved)')
