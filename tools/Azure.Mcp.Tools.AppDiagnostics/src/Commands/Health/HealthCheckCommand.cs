// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Mcp.Core.Commands.Subscription;
using Azure.Mcp.Core.Services.Azure.Subscription;
using Azure.Mcp.Tools.AppDiagnostics.Models;
using Azure.Mcp.Tools.AppDiagnostics.Options.Health;
using Azure.Mcp.Tools.AppDiagnostics.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Mcp.Core.Commands;
using Microsoft.Mcp.Core.Models.Command;

namespace Azure.Mcp.Tools.AppDiagnostics.Commands.Health;

[CommandMetadata(
    Id = "9c6a1d08-fc60-1998-0349-0671490d9009",
    Name = "check",
    Title = "Application Health Check",
    Description = "Checks the operational state, availability state, HTTPS setting, and VNet integration status for App Service / Function App / Container App.",
    OperationPlane = ToolOperationPlane.Control,
    Destructive = false,
    Idempotent = true,
    OpenWorld = false,
    ReadOnly = true,
    Secret = false,
    LocalRequired = false)]
public sealed class HealthCheckCommand(
    ILogger<HealthCheckCommand> logger,
    IAppDiagnosticsService appDiagnosticsService,
    ISubscriptionResolver subscriptionResolver)
    : SubscriptionCommand<HealthCheckOptions, HealthCheckCommand.HealthCheckCommandResult>(subscriptionResolver)
{
    private readonly ILogger<HealthCheckCommand> _logger = logger;
    private readonly IAppDiagnosticsService _appDiagnosticsService = appDiagnosticsService;

    public override async Task<CommandResponse> ExecuteAsync(
        CommandContext context,
        HealthCheckOptions options,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _appDiagnosticsService.CheckAppHealthAsync(
                options.App,
                options.ResourceGroup,
                options.Subscription!,
                options.Tenant,
                cancellationToken);

            context.Response.Results = ResponseResult.Create(
                new HealthCheckCommandResult(result),
                AppDiagnosticsJsonContext.Default.HealthCheckCommandResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing App Health Check command. App: {App}, Subscription: {Subscription}", options.App, options.Subscription);
            HandleException(context, ex);
        }

        return context.Response;
    }

    public record HealthCheckCommandResult(AppHealthCheckResult Result);
}
