// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Mcp.Core.Commands.Subscription;
using Azure.Mcp.Core.Services.Azure.Subscription;
using Azure.Mcp.Tools.NetworkDiagnostics.Models;
using Azure.Mcp.Tools.NetworkDiagnostics.Options.Peering;
using Azure.Mcp.Tools.NetworkDiagnostics.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Mcp.Core.Commands;
using Microsoft.Mcp.Core.Models.Command;

namespace Azure.Mcp.Tools.NetworkDiagnostics.Commands.Peering;

[CommandMetadata(
    Id = "4d1b6e53-a715-6443-be04-512c04de4004",
    Name = "check",
    Title = "Virtual Network Peering Check",
    Description = "Checks the peering state, sync status, and transit permissions for all peerings on a virtual network.",
    OperationPlane = ToolOperationPlane.Control,
    Destructive = false,
    Idempotent = true,
    OpenWorld = false,
    ReadOnly = true,
    Secret = false,
    LocalRequired = false)]
public sealed class PeeringCheckCommand(
    ILogger<PeeringCheckCommand> logger,
    INetworkDiagnosticsService networkDiagnosticsService,
    ISubscriptionResolver subscriptionResolver)
    : SubscriptionCommand<PeeringCheckOptions, PeeringCheckCommand.PeeringCheckCommandResult>(subscriptionResolver)
{
    private readonly ILogger<PeeringCheckCommand> _logger = logger;
    private readonly INetworkDiagnosticsService _networkDiagnosticsService = networkDiagnosticsService;

    public override async Task<CommandResponse> ExecuteAsync(
        CommandContext context,
        PeeringCheckOptions options,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _networkDiagnosticsService.CheckPeeringStatusAsync(
                options.Vnet,
                options.ResourceGroup,
                options.Subscription!,
                options.Tenant,
                cancellationToken);

            context.Response.Results = ResponseResult.Create(
                new PeeringCheckCommandResult(result),
                NetworkDiagnosticsJsonContext.Default.PeeringCheckCommandResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing VNet Peering Check command. VNet: {Vnet}, Subscription: {Subscription}", options.Vnet, options.Subscription);
            HandleException(context, ex);
        }

        return context.Response;
    }

    public record PeeringCheckCommandResult(PeeringCheckResult Result);
}
