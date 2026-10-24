"""Verify archived original bytes in both the checkout and a Git revision."""
import argparse
import hashlib
import json
from pathlib import Path
import subprocess

parser = argparse.ArgumentParser()
parser.add_argument("--revision", default="HEAD")
parser.add_argument("--report")
args = parser.parse_args()
root = Path(__file__).resolve().parent.parent
manifest = json.loads((root / "legacy/manifest.json").read_text(encoding="utf-8-sig"))
cases = []
for item in manifest:
    name = item["archived"].replace("\\", "/")
    checkout = (root / name).read_bytes()
    blob = subprocess.run(["git", "show", f"{args.revision}:{name}"], cwd=root, capture_output=True, check=True).stdout
    expected = item["sha256"]
    cases.append({"file": name, "checkout": hashlib.sha256(checkout).hexdigest(), "gitBlob": hashlib.sha256(blob).hexdigest(), "expected": expected,
                  "passed": hashlib.sha256(checkout).hexdigest() == expected == hashlib.sha256(blob).hexdigest()})
report = {"revision": args.revision, "passed": sum(case["passed"] for case in cases), "failed": sum(not case["passed"] for case in cases), "cases": cases}
if args.report:
    Path(args.report).write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")
print(json.dumps({key: value for key, value in report.items() if key != "cases"}, ensure_ascii=False))
raise SystemExit(0 if report["failed"] == 0 else 1)
