#!/usr/bin/env python3
"""
Azure MCP Server - PyPI package.

This module provides the entry point for the Azure MCP Server CLI.
The binary is bundled directly in the wheel for the target platform.
"""

import os
import platform
import subprocess
import sys
from pathlib import Path

__version__ = "0.0.0"  # Will be replaced during packaging

# Debug mode check
DEBUG = os.environ.get("DEBUG", "").lower() in ("true", "1", "*") or "mcp" in os.environ.get("DEBUG", "")


def debug_log(*args, **kwargs):
    """Print debug messages to stderr if DEBUG is enabled."""
    if DEBUG:
        print(*args, file=sys.stderr, **kwargs)


def get_executable_path():
    """Get the path to the platform-specific executable."""
    # The binary is located in the bin subdirectory of this package
    package_dir = Path(__file__).resolve().parent
    bin_dir = (package_dir / "bin").resolve()

    # Determine the executable name based on platform
    system = platform.system().lower()
    if system == "windows":
        executable_name = "azmcp.exe"
    else:
        executable_name = "azmcp"

    executable_path = (bin_dir / executable_name).resolve()

    # Ensure executable path stays strictly within the package bin directory
    try:
        executable_path.relative_to(bin_dir)
    except ValueError:
        raise RuntimeError(f"Invalid executable path: {executable_path}")

    debug_log(f"Package directory: {package_dir}")
    debug_log(f"Binary directory: {bin_dir}")
    debug_log(f"Executable path: {executable_path}")

    return executable_path


def run_executable(args=None):
    """
    Run the platform-specific executable with the given arguments.
    
    Args:
        args: List of command-line arguments to pass to the executable.
              Defaults to sys.argv[1:] if not provided.
    
    Returns:
        The exit code from the executable.
    """
    if args is None:
        args = sys.argv[1:]

    try:
        executable_path = get_executable_path()
    except RuntimeError as e:
        print(f"Error: {e}", file=sys.stderr)
        return 1

    if not executable_path.is_file():
        print(f"Error: Executable not found at {executable_path}", file=sys.stderr)
        print("This may indicate a packaging issue or unsupported platform.", file=sys.stderr)
        return 1

    # Sanitize and validate arguments to prevent command injection
    validated_args = []
    for arg in args:
        if not isinstance(arg, str):
            arg = str(arg)
        # Reject arguments with null bytes or control characters that could corrupt command boundaries
        if any(c in arg for c in ("\0", "\r", "\n")):
            print("Error: Invalid argument containing control characters", file=sys.stderr)
            return 1
        validated_args.append(arg)

    debug_log(f"Running: {executable_path} {' '.join(validated_args)}")

    try:
        result = subprocess.run(
            [str(executable_path), *validated_args],
            shell=False,
            stdin=sys.stdin,
            stdout=sys.stdout,
            stderr=sys.stderr,
        )
        return result.returncode
    except PermissionError:
        print(f"Error: Permission denied executing {executable_path}", file=sys.stderr)
        print("Try: chmod +x " + str(executable_path), file=sys.stderr)
        return 126
    except OSError as e:
        print(f"Error executing {executable_path}: {e}", file=sys.stderr)
        return 1


def main():
    """Main entry point for the CLI."""
    sys.exit(run_executable())


if __name__ == "__main__":
    main()
