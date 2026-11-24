"""Compare application versions at the two ends of a push to main."""
import os
from pathlib import Path
import re
import subprocess
import xml.etree.ElementTree as ET

PROJECT = "StagePad.App/StagePad.App.csproj"


def version(revision):
    xml = subprocess.check_output(["git", "show", f"{revision}:{PROJECT}"])
    value = ET.fromstring(xml).findtext("./PropertyGroup/Version", "").strip()
    if not re.fullmatch(r"[0-9]+[.][0-9]+", value):
        raise ValueError(f"Invalid major.minor version at {revision}")
    return tuple(map(int, value.split(".")))


if __name__ == "__main__":
    before = os.environ["BEFORE"]
    if not re.fullmatch(r"[0-9a-f]{40}", before):
        raise ValueError("Missing or invalid push base")
    increased = before != "0" * 40 and version("HEAD") > version(before)
    with Path(os.environ["GITHUB_OUTPUT"]).open("a", encoding="utf-8") as output:
        print(f"increased={str(increased).lower()}", file=output)
