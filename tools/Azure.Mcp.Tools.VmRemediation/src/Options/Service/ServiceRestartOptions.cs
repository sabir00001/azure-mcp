// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Mcp.Core.Options;
using Microsoft.Mcp.Core.Models;
using Microsoft.Mcp.Core.Options;

namespace Azure.Mcp.Tools.VmRemediation.Options.Service;

public class ServiceRestartOptions : ISubscriptionOption
{
    [Option(Description = "The virtual machine name.")]
    public required string Vm { get; set; }

    [Option(Description = "The allowlisted service name to start or restart.")]
    public required string Service { get; set; }

    [Option(Description = "The ITSM incident identifier. Required for every remediation action.")]
    public required string IncidentId { get; set; }

    [Option(Description = OptionDescriptions.ResourceGroup)]
    public required string ResourceGroup { get; set; }

    [Option(Description = OptionDescriptions.Subscription)]
    public string? Subscription { get; set; }

    [Option(Description = OptionDescriptions.Tenant)]
    public string? Tenant { get; set; }
}
