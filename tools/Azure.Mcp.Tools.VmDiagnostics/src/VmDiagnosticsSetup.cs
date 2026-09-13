// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Mcp.Tools.VmDiagnostics.Commands.EventLogs;
using Azure.Mcp.Tools.VmDiagnostics.Commands.Health;
using Azure.Mcp.Tools.VmDiagnostics.Commands.Metrics;
using Azure.Mcp.Tools.VmDiagnostics.Commands.Process;
using Azure.Mcp.Tools.VmDiagnostics.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Mcp.Core.Areas;
using Microsoft.Mcp.Core.Commands;

namespace Azure.Mcp.Tools.VmDiagnostics;

public class VmDiagnosticsSetup : IAreaSetup
{
    public string Name => "vmdiagnostics";

    public string Title => "Azure VM Diagnostics";

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IVmDiagnosticsService, VmDiagnosticsService>();
        services.AddSingleton<HealthGetCommand>();
        services.AddSingleton<MetricsReviewCommand>();
        services.AddSingleton<ProcessListCommand>();
        services.AddSingleton<EventLogsQueryCommand>();
    }

    public CommandGroup RegisterCommands(IServiceProvider serviceProvider)
    {
        var vmDiagnostics = new CommandGroup(Name,
            "VM Diagnostics operations - Commands to diagnose VM power state, agent status, performance metrics, process usage, and OS event logs.", Title);

        var health = new CommandGroup("health", "VM health commands.");
        var metrics = new CommandGroup("metrics", "VM metrics commands.");
        var process = new CommandGroup("process", "VM process diagnostics commands.");
        var eventLogs = new CommandGroup("eventlogs", "VM OS event log commands.");

        vmDiagnostics.AddSubGroup(health);
        vmDiagnostics.AddSubGroup(metrics);
        vmDiagnostics.AddSubGroup(process);
        vmDiagnostics.AddSubGroup(eventLogs);

        health.AddCommand<HealthGetCommand>(serviceProvider);
        metrics.AddCommand<MetricsReviewCommand>(serviceProvider);
        process.AddCommand<ProcessListCommand>(serviceProvider);
        eventLogs.AddCommand<EventLogsQueryCommand>(serviceProvider);

        return vmDiagnostics;
    }
}
