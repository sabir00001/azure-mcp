// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Azure.Mcp.Tools.VmDiagnostics.Models;

public record VmServiceStatusResult(
    string VmName,
    string ResourceGroup,
    string ServiceName,
    string Status,
    string StartType,
    List<string> DependentServices,
    string OperatingSystem,
    string RawOutput);
