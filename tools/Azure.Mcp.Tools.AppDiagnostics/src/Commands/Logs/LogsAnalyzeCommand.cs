// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Mcp.Core.Commands.Subscription;
using Azure.Mcp.Core.Services.Azure.Subscription;
using Azure.Mcp.Tools.AppDiagnostics.Models;
using Azure.Mcp.Tools.AppDiagnostics.Options.Logs;
using Azure.Mcp.Tools.AppDiagnostics.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Mcp.Core.Commands;
using Microsoft.Mcp.Core.Models.Command;

namespace Azure.Mcp.Tools.AppDiagnostics.Commands.Logs;

[CommandMetadata(
    Id = "1e8c3f20-1e82-31ba-2561-28936b2f0011",
    Name = "analyze",
    Title = "Application Error and Exception Log Analysis",
    Description = "Analyzes application telemetry and exception traces from App Insights / Log Analytics to correlate root causes for L2 support handoffs.",
    OperationPlane = ToolOperationPlane.Control,
    Destructive = false,
    Idempotent = true,
    OpenWorld = false,
    ReadOnly = true,
    Secret = false,
    LocalRequired = false)]
public sealed class LogsAnalyzeCommand(
    ILogger<LogsAnalyzeCommand> logger,
    IAppDiagnosticsService appDiagnosticsService,
    ISubscriptionResolver subscriptionResolver)
    : SubscriptionCommand<LogsAnalyzeOptions, LogsAnalyzeCommand.LogsAnalyzeCommandResult>(subscriptionResolver)
{
    private readonly ILogger<LogsAnalyzeCommand> _logger = logger;
    private readonly IAppDiagnosticsService _appDiagnosticsService = appDiagnosticsService;

    public override async Task<CommandResponse> ExecuteAsync(
        CommandContext context,
        LogsAnalyzeOptions options,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _appDiagnosticsService.AnalyzeAppLogsAsync(
                options.App,
                options.ResourceGroup,
                options.Subscription!,
                options.TimeWindow,
                options.Tenant,
                cancellationToken);

            context.Response.Results = ResponseResult.Create(
                new LogsAnalyzeCommandResult(result),
                AppDiagnosticsJsonContext.Default.LogsAnalyzeCommandResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing App Logs Analyze command. App: {App}, Subscription: {Subscription}", options.App, options.Subscription);
            HandleException(context, ex);
        }

        return context.Response;
    }

    public record LogsAnalyzeCommandResult(AppLogsAnalysisResult Result);
}
