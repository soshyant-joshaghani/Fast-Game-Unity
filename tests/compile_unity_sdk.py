"""Compile Unity Fast Game + FastGameStore Runtime C# against UnityEngine stubs."""
from __future__ import annotations

import shutil
import subprocess
from pathlib import Path

TESTS = Path(__file__).resolve().parent
ROOT = TESTS.parent
UNITY = ROOT / "Packages/com.fastgame.sdk"
STUBS = TESTS / "unity-stubs/UnityEngine.cs"
OUT = TESTS / "_compile_out" / "unity"
CSC_CANDIDATES = [
    Path(r"C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\Roslyn\csc.exe"),
    Path(r"C:\WINDOWS\Microsoft.NET\Framework64\v4.0.30319\csc.exe"),
]


def find_csc() -> Path:
    which = shutil.which("csc")
    if which:
        return Path(which)
    for cand in CSC_CANDIDATES:
        if cand.is_file():
            return cand
    raise SystemExit("csc.exe not found - install Visual Studio / .NET Framework targeting pack")


def main() -> int:
    if not STUBS.is_file():
        raise SystemExit(f"missing Unity stubs: {STUBS}")
    sources = sorted((UNITY / "Runtime").rglob("*.cs"))
    if not sources:
        raise SystemExit("no Unity Runtime .cs files")
    if OUT.exists():
        shutil.rmtree(OUT)
    OUT.mkdir(parents=True)
    dll = OUT / "FastGame.Sdk.Test.dll"
    csc = find_csc()
    cmd = [
        str(csc),
        "-nologo",
        "-t:library",
        "-langversion:latest",
        f"-out:{dll}",
        "-define:UNITY_2020_2_OR_NEWER;UNITY_ANDROID",
        str(STUBS),
        *[str(p) for p in sources],
    ]
    print("+", " ".join(cmd[:6]), f"... {len(sources)} Runtime cs + stubs")
    subprocess.check_call(cmd)
    if not dll.is_file():
        raise SystemExit("Unity SDK dll not produced")
    print(f"OK csc Unity Runtime ({len(sources)} files) -> {dll.name}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
