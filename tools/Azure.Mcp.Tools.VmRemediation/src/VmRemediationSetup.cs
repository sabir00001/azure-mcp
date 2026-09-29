// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Mcp.Tools.VmRemediation.Commands.Service;
using Azure.Mcp.Tools.VmRemediation.Commands.Vm;
using Azure.Mcp.Tools.VmRemediation.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Mcp.Core.Areas;
using Microsoft.Mcp.Core.Commands;

namespace Azure.Mcp.Tools.VmRemediation;

public sealed class VmRemediationSetup : IAreaSetup
{
    public string Name => "vmremediation";
    public string Title => "Azure VM Remediation";

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IRemediationApprovalProvider, FailClosedRemediationApprovalProvider>();
        services.AddSingleton<IVmRemediationService, VmRemediationService>();
        services.AddSingleton<ServiceRestartCommand>();
        services.AddSingleton<VmRestartCommand>();
    }

    public CommandGroup RegisterCommands(IServiceProvider serviceProvider)
    {
        var remediation = new CommandGroup(Name, "Safe, approval-aware remediation operations for Azure virtual machines.", Title);
        var service = new CommandGroup("service", "Allowlisted VM service remediation.");
        var vm = new CommandGroup("vm", "Approval-gated VM remediation.");
        remediation.AddSubGroup(service);
        remediation.AddSubGroup(vm);
        service.AddCommand<ServiceRestartCommand>(serviceProvider);
        vm.AddCommand<VmRestartCommand>(serviceProvider);
        return remediation;
    }
}
