// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Mcp.Tools.AppDiagnostics.Commands.Deployment;
using Azure.Mcp.Tools.AppDiagnostics.Commands.Health;
using Azure.Mcp.Tools.AppDiagnostics.Commands.Logs;
using Azure.Mcp.Tools.AppDiagnostics.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Mcp.Core.Areas;
using Microsoft.Mcp.Core.Commands;

namespace Azure.Mcp.Tools.AppDiagnostics;

public class AppDiagnosticsSetup : IAreaSetup
{
    public string Name => "appdiagnostics";

    public string Title => "Azure App Diagnostics";

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IAppDiagnosticsService, AppDiagnosticsService>();
        services.AddSingleton<HealthCheckCommand>();
        services.AddSingleton<DeploymentTriageCommand>();
        services.AddSingleton<LogsAnalyzeCommand>();
    }

    public CommandGroup RegisterCommands(IServiceProvider serviceProvider)
    {
        var appDiagnostics = new CommandGroup(Name,
            "App Diagnostics operations - Commands to diagnose application health, deployment failures, and telemetry error logs.", Title);

        var health = new CommandGroup("health", "App health check commands.");
        var deployment = new CommandGroup("deployment", "App deployment triage commands.");
        var logs = new CommandGroup("logs", "App log analysis commands.");

        appDiagnostics.AddSubGroup(health);
        appDiagnostics.AddSubGroup(deployment);
        appDiagnostics.AddSubGroup(logs);

        health.AddCommand<HealthCheckCommand>(serviceProvider);
        deployment.AddCommand<DeploymentTriageCommand>(serviceProvider);
        logs.AddCommand<LogsAnalyzeCommand>(serviceProvider);

        return appDiagnostics;
    }
}
