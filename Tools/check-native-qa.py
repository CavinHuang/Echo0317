#!/usr/bin/env python3
"""Validate a *real* compiled Unity player's runtime QA JSON report.

This tool validates evidence; it does not run Unity or produce a pass report.
"""
import argparse
import json
import pathlib
import re
import sys
from datetime import datetime


def validate(report):
    required = {
        "schema": "echo0317-native-qa-v2",
        "result": "pass",
        "storyRevision": "v3.0",
        "nodes": 55,
        "rules": 5,
        "graphStatesVerified": 69497,
        "graphNodesReachable": 55,
        "deadEndStates": 0,
        "endingsVerified": 4,
        "saveRoundtripPassed": True,
        "tamperingRejected": True,
    }
    for key, expected in required.items():
        if report.get(key) != expected:
            raise ValueError(f"{key}: expected {expected!r}, found {report.get(key)!r}")
    if not re.match(r"^6000\.3\.\d+", str(report.get("unityVersion", ""))):
        raise ValueError("Unity version is not 6.3 LTS")
    if not isinstance(report.get("choicesVerified"), int) or report["choicesVerified"] < 100:
        raise ValueError("Too few native path choices verified")
    if report.get("error"):
        raise ValueError("Native runtime reported an error")
    timestamp = str(report.get("utc", ""))
    try:
        datetime.fromisoformat(timestamp.replace("Z", "+00:00"))
    except ValueError as ex:
        raise ValueError("Missing or invalid native execution timestamp") from ex
    return True


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("report", type=pathlib.Path, help="JSON produced by compiled Unity player, not by offline tests")
    args = parser.parse_args()
    try:
        validate(json.loads(args.report.read_text(encoding="utf-8-sig")))
    except (ValueError, OSError, json.JSONDecodeError) as ex:
        print(f"FAIL native Unity player QA: {ex}", file=sys.stderr)
        return 1
    print(f"PASS native Unity player QA report: {args.report}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
