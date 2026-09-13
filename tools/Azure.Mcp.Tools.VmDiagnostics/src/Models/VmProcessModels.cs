// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Azure.Mcp.Tools.VmDiagnostics.Models;

public record ProcessInfo(
    int ProcessId,
    string ProcessName,
    double CpuPercent,
    long WorkingSetBytes);

public record VmProcessListResult(
    string VmName,
    string ResourceGroup,
    string ExecutionStatus,
    List<ProcessInfo> Processes,
    string OutputSummary);
