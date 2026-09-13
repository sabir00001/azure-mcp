// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Mcp.Core.Commands.Subscription;
using Azure.Mcp.Core.Services.Azure.Subscription;
using Azure.Mcp.Tools.VmDiagnostics.Models;
using Azure.Mcp.Tools.VmDiagnostics.Options.Process;
using Azure.Mcp.Tools.VmDiagnostics.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Mcp.Core.Commands;
using Microsoft.Mcp.Core.Models.Command;

namespace Azure.Mcp.Tools.VmDiagnostics.Commands.Process;

[CommandMetadata(
    Id = "7a4e9b86-da48-9776-e127-845f270b7007",
    Name = "list",
    Title = "VM Top Active Processes",
    Description = "Executes safe, policy-restricted VM Run Command diagnostics to list top active processes by CPU and memory consumption.",
    OperationPlane = ToolOperationPlane.Control,
    Destructive = false,
    Idempotent = true,
    OpenWorld = false,
    ReadOnly = true,
    Secret = false,
    LocalRequired = false)]
public sealed class ProcessListCommand(
    ILogger<ProcessListCommand> logger,
    IVmDiagnosticsService vmDiagnosticsService,
    ISubscriptionResolver subscriptionResolver)
    : SubscriptionCommand<ProcessListOptions, ProcessListCommand.ProcessListCommandResult>(subscriptionResolver)
{
    private readonly ILogger<ProcessListCommand> _logger = logger;
    private readonly IVmDiagnosticsService _vmDiagnosticsService = vmDiagnosticsService;

    public override async Task<CommandResponse> ExecuteAsync(
        CommandContext context,
        ProcessListOptions options,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _vmDiagnosticsService.ListTopProcessesAsync(
                options.Vm,
                options.ResourceGroup,
                options.Subscription!,
                options.Top,
                options.Tenant,
                cancellationToken);

            context.Response.Results = ResponseResult.Create(
                new ProcessListCommandResult(result),
                VmDiagnosticsJsonContext.Default.ProcessListCommandResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing VM Process List command. VM: {Vm}, Subscription: {Subscription}", options.Vm, options.Subscription);
            HandleException(context, ex);
        }

        return context.Response;
    }

    public record ProcessListCommandResult(VmProcessListResult Result);
}
