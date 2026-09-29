// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Mcp.Core.Options;
using Microsoft.Mcp.Core.Options;

namespace Azure.Mcp.Tools.VmDiagnostics.Options.Service;

public class ServiceStatusOptions : ISubscriptionOption
{
    [Option(Description = "The name of the Virtual Machine.")]
    public required string Vm { get; set; }

    [Option(Description = "The allowlisted Windows service or Linux systemd unit.")]
    public required string Service { get; set; }

    [Option(Description = OptionDescriptions.ResourceGroup)]
    public required string ResourceGroup { get; set; }

    [Option(Description = OptionDescriptions.Subscription)]
    public string? Subscription { get; set; }

    [Option(Description = OptionDescriptions.Tenant)]
    public string? Tenant { get; set; }
}
