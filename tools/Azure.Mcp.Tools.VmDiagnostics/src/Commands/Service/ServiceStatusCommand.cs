// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Mcp.Core.Commands.Subscription;
using Azure.Mcp.Core.Services.Azure.Subscription;
using Azure.Mcp.Tools.VmDiagnostics.Models;
using Azure.Mcp.Tools.VmDiagnostics.Options.Service;
using Azure.Mcp.Tools.VmDiagnostics.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Mcp.Core.Commands;
using Microsoft.Mcp.Core.Models.Command;

namespace Azure.Mcp.Tools.VmDiagnostics.Commands.Service;

[CommandMetadata(
    Id = "0cb50e59-b7b9-4c58-89d4-6c1bdfd7d2b1",
    Name = "status",
    Title = "VM Service Status",
    Description = "Reads the status, start type, and dependent services for an allowlisted Windows service or Linux systemd unit.",
    OperationPlane = ToolOperationPlane.Control,
    Destructive = false,
    Idempotent = true,
    OpenWorld = false,
    ReadOnly = true,
    Secret = false,
    LocalRequired = false)]
public sealed class ServiceStatusCommand(
    ILogger<ServiceStatusCommand> logger,
    IVmDiagnosticsService vmDiagnosticsService,
    ISubscriptionResolver subscriptionResolver)
    : SubscriptionCommand<ServiceStatusOptions, ServiceStatusCommand.ServiceStatusCommandResult>(subscriptionResolver)
{
    private readonly ILogger<ServiceStatusCommand> _logger = logger;
    private readonly IVmDiagnosticsService _vmDiagnosticsService = vmDiagnosticsService;

    public override async Task<CommandResponse> ExecuteAsync(CommandContext context, ServiceStatusOptions options, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _vmDiagnosticsService.GetServiceStatusAsync(
                options.Vm,
                options.ResourceGroup,
                options.Service,
                options.Subscription!,
                options.Tenant,
                cancellationToken);
            context.Response.Results = ResponseResult.Create(new(result), VmDiagnosticsJsonContext.Default.ServiceStatusCommandResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying VM service status. VM: {Vm}, Service: {Service}, Subscription: {Subscription}", options.Vm, options.Service, options.Subscription);
            HandleException(context, ex);
        }

        return context.Response;
    }

    public sealed record ServiceStatusCommandResult(VmServiceStatusResult Result);
}
