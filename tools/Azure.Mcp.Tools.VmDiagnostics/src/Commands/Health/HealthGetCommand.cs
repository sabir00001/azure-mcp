// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Mcp.Core.Commands.Subscription;
using Azure.Mcp.Core.Services.Azure.Subscription;
using Azure.Mcp.Tools.VmDiagnostics.Models;
using Azure.Mcp.Tools.VmDiagnostics.Options.Health;
using Azure.Mcp.Tools.VmDiagnostics.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Mcp.Core.Commands;
using Microsoft.Mcp.Core.Models.Command;

namespace Azure.Mcp.Tools.VmDiagnostics.Commands.Health;

[CommandMetadata(
    Id = "5e2c7f64-b826-7554-cf05-623d05ef5005",
    Name = "get",
    Title = "VM Instance View and Agent Health",
    Description = "Retrieves power state, provisioning status, VM agent health, and extension statuses for a virtual machine.",
    OperationPlane = ToolOperationPlane.Control,
    Destructive = false,
    Idempotent = true,
    OpenWorld = false,
    ReadOnly = true,
    Secret = false,
    LocalRequired = false)]
public sealed class HealthGetCommand(
    ILogger<HealthGetCommand> logger,
    IVmDiagnosticsService vmDiagnosticsService,
    ISubscriptionResolver subscriptionResolver)
    : SubscriptionCommand<HealthGetOptions, HealthGetCommand.HealthGetCommandResult>(subscriptionResolver)
{
    private readonly ILogger<HealthGetCommand> _logger = logger;
    private readonly IVmDiagnosticsService _vmDiagnosticsService = vmDiagnosticsService;

    public override async Task<CommandResponse> ExecuteAsync(
        CommandContext context,
        HealthGetOptions options,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _vmDiagnosticsService.GetVmHealthAsync(
                options.Vm,
                options.ResourceGroup,
                options.Subscription!,
                options.Tenant,
                cancellationToken);

            context.Response.Results = ResponseResult.Create(
                new HealthGetCommandResult(result),
                VmDiagnosticsJsonContext.Default.HealthGetCommandResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing VM Health Get command. VM: {Vm}, Subscription: {Subscription}", options.Vm, options.Subscription);
            HandleException(context, ex);
        }

        return context.Response;
    }

    public record HealthGetCommandResult(VmHealthSummaryResult Result);
}
