import json
import sys
from datetime import datetime

path = sys.argv[1]
with open(path) as f:
    d = json.load(f)

if 'metadata' not in d:
    print(-1)
else:
    generated = datetime.fromisoformat(d['metadata']['generated_at'])
    print((datetime.now() - generated).days)