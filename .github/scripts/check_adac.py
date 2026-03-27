import json
import sys
from datetime import datetime, timezone

path = sys.argv[1]
with open(path, encoding='utf-8-sig') as f:
    d = json.load(f)

if 'metadata' not in d:
    print(-1)
else:
    generated = datetime.fromisoformat(d['metadata']['generated_at'])
    if generated.tzinfo is None:
        generated = generated.replace(tzinfo=timezone.utc)
    age = (datetime.now(timezone.utc) - generated).days
    print(age)