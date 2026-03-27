import json
import sys
from datetime import datetime

path = sys.argv[1]
with open(path, encoding='utf-8-sig') as f:
    d = json.load(f)

print(f"metadata in d: {'metadata' in d}", file=sys.stderr)

if 'metadata' not in d:
    print(-1)
else:
    try:
        generated = datetime.fromisoformat(d['metadata']['generated_at'])
        age = (datetime.now() - generated).days
        print(f"generated: {generated}", file=sys.stderr)
        print(f"age: {age}", file=sys.stderr)
        print(age)
    except Exception as e:
        print(f"Error: {e}", file=sys.stderr)
        print(-1)