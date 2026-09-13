// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Mcp.Core.Options;
using Microsoft.Mcp.Core.Options;

namespace Azure.Mcp.Tools.VmDiagnostics.Options.Process;

public class ProcessListOptions : ISubscriptionOption
{
    [Option(Description = "The name of the Virtual Machine.")]
    public required string Vm { get; set; }

    [Option(Description = "Number of top processes to return by CPU/Memory usage.")]
    public int Top { get; set; } = 10;

    [Option(Description = OptionDescriptions.ResourceGroup)]
    public required string ResourceGroup { get; set; }

    [Option(Description = OptionDescriptions.Subscription)]
    public string? Subscription { get; set; }

    [Option(Description = OptionDescriptions.Tenant)]
    public string? Tenant { get; set; }
}
