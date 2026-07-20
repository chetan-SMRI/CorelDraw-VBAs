#!/usr/bin/env python3
"""Generate the current 8-digit SMRI Photoshop activation code."""

import time


def activation_code(minute: int) -> str:
    value = minute % 100_000_000
    value = (value * 48_271 + 917_263) % 100_000_000
    value = (value * 69_621 + 123_457) % 100_000_000
    return f"{value:08d}"


if __name__ == "__main__":
    current_minute = int(time.time() // 60)
    print("SMRI activation code:", activation_code(current_minute))
    print("Valid for the current one-minute window.")

