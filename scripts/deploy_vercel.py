"""Publish tracked API source to the dedicated personal Vercel project."""
import json
from pathlib import Path
import shutil
import subprocess
import tempfile

root = Path(__file__).resolve().parent.parent
project_path = root / ".vercel/project.json"
project = json.loads(project_path.read_text())
if project.get("projectName") != "case-reviews-demo-api" or project.get("orgId") != "team_cwjFWlUVzkCIYgVepQ6Glc71":
    raise SystemExit("Link the dedicated personal case-reviews-demo-api project first.")
files = subprocess.check_output(["git", "ls-files", "-z"], cwd=root).decode().split("\0")
with tempfile.TemporaryDirectory(prefix="case-reviews-api-deploy-") as directory:
    target = Path(directory)
    for name in filter(None, files):
        source = root / name
        if not source.is_file():
            raise SystemExit("Commit file deletions before deploying.")
        destination = target / name
        destination.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(source, destination)
    (target / ".vercel").mkdir()
    shutil.copy2(project_path, target / ".vercel/project.json")
    subprocess.run([
        "npm", "exec", "--yes", "--package=vercel@59.15.0", "--",
        "vercel", "deploy", "--prod", "--yes", "--scope", "brennanbutler01s-projects",
    ], cwd=target, check=True)
