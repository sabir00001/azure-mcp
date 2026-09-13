// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Mcp.Core.Commands.Subscription;
using Azure.Mcp.Core.Services.Azure.Subscription;
using Azure.Mcp.Tools.VmDiagnostics.Models;
using Azure.Mcp.Tools.VmDiagnostics.Options.EventLogs;
using Azure.Mcp.Tools.VmDiagnostics.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Mcp.Core.Commands;
using Microsoft.Mcp.Core.Models.Command;

namespace Azure.Mcp.Tools.VmDiagnostics.Commands.EventLogs;

[CommandMetadata(
    Id = "8b5f0c97-eb59-0887-f238-9560381c8008",
    Name = "query",
    Title = "VM OS Event Log Query",
    Description = "Queries Windows Event Logs (System/Application) or Linux syslog for critical errors and warnings.",
    OperationPlane = ToolOperationPlane.Control,
    Destructive = false,
    Idempotent = true,
    OpenWorld = false,
    ReadOnly = true,
    Secret = false,
    LocalRequired = false)]
public sealed class EventLogsQueryCommand(
    ILogger<EventLogsQueryCommand> logger,
    IVmDiagnosticsService vmDiagnosticsService,
    ISubscriptionResolver subscriptionResolver)
    : SubscriptionCommand<EventLogsQueryOptions, EventLogsQueryCommand.EventLogsQueryCommandResult>(subscriptionResolver)
{
    private readonly ILogger<EventLogsQueryCommand> _logger = logger;
    private readonly IVmDiagnosticsService _vmDiagnosticsService = vmDiagnosticsService;

    public override async Task<CommandResponse> ExecuteAsync(
        CommandContext context,
        EventLogsQueryOptions options,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _vmDiagnosticsService.QueryEventLogsAsync(
                options.Vm,
                options.ResourceGroup,
                options.Subscription!,
                options.LogType,
                options.MaxEntries,
                options.Tenant,
                cancellationToken);

            context.Response.Results = ResponseResult.Create(
                new EventLogsQueryCommandResult(result),
                VmDiagnosticsJsonContext.Default.EventLogsQueryCommandResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing VM Event Logs Query command. VM: {Vm}, Subscription: {Subscription}", options.Vm, options.Subscription);
            HandleException(context, ex);
        }

        return context.Response;
    }

    public record EventLogsQueryCommandResult(VmEventLogsQueryResult Result);
}
