// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Mcp.Core.Commands.Subscription;
using Azure.Mcp.Core.Services.Azure.Subscription;
using Azure.Mcp.Tools.AppDiagnostics.Models;
using Azure.Mcp.Tools.AppDiagnostics.Options.Deployment;
using Azure.Mcp.Tools.AppDiagnostics.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Mcp.Core.Commands;
using Microsoft.Mcp.Core.Models.Command;

namespace Azure.Mcp.Tools.AppDiagnostics.Commands.Deployment;

[CommandMetadata(
    Id = "0d7b2e19-0d71-20a9-1450-17825a1e0010",
    Name = "triage",
    Title = "Application Deployment Failure Triage",
    Description = "Triages recent application deployments, slot/revision status, and deployment build logs to isolate release failures.",
    OperationPlane = ToolOperationPlane.Control,
    Destructive = false,
    Idempotent = true,
    OpenWorld = false,
    ReadOnly = true,
    Secret = false,
    LocalRequired = false)]
public sealed class DeploymentTriageCommand(
    ILogger<DeploymentTriageCommand> logger,
    IAppDiagnosticsService appDiagnosticsService,
    ISubscriptionResolver subscriptionResolver)
    : SubscriptionCommand<DeploymentTriageOptions, DeploymentTriageCommand.DeploymentTriageCommandResult>(subscriptionResolver)
{
    private readonly ILogger<DeploymentTriageCommand> _logger = logger;
    private readonly IAppDiagnosticsService _appDiagnosticsService = appDiagnosticsService;

    public override async Task<CommandResponse> ExecuteAsync(
        CommandContext context,
        DeploymentTriageOptions options,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _appDiagnosticsService.TriageDeploymentAsync(
                options.App,
                options.ResourceGroup,
                options.Subscription!,
                options.Tenant,
                cancellationToken);

            context.Response.Results = ResponseResult.Create(
                new DeploymentTriageCommandResult(result),
                AppDiagnosticsJsonContext.Default.DeploymentTriageCommandResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing App Deployment Triage command. App: {App}, Subscription: {Subscription}", options.App, options.Subscription);
            HandleException(context, ex);
        }

        return context.Response;
    }

    public record DeploymentTriageCommandResult(AppDeploymentTriageResult Result);
}
