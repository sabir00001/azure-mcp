// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Mcp.Core.Commands.Subscription;
using Azure.Mcp.Core.Services.Azure.Subscription;
using Azure.Mcp.Tools.NetworkDiagnostics.Models;
using Azure.Mcp.Tools.NetworkDiagnostics.Options.Topology;
using Azure.Mcp.Tools.NetworkDiagnostics.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Mcp.Core.Commands;
using Microsoft.Mcp.Core.Models.Command;

namespace Azure.Mcp.Tools.NetworkDiagnostics.Commands.Topology;

[CommandMetadata(
    Id = "1a8f3b20-74e2-4110-8b01-299f01ab1001",
    Name = "summary",
    Title = "Network Topology Summary",
    Description = "Retrieves a operational topology summary for Virtual Networks, subnets, route tables, NSGs, and private endpoints in a subscription or resource group.",
    OperationPlane = ToolOperationPlane.Control,
    Destructive = false,
    Idempotent = true,
    OpenWorld = false,
    ReadOnly = true,
    Secret = false,
    LocalRequired = false)]
public sealed class TopologySummaryCommand(
    ILogger<TopologySummaryCommand> logger,
    INetworkDiagnosticsService networkDiagnosticsService,
    ISubscriptionResolver subscriptionResolver)
    : SubscriptionCommand<TopologySummaryOptions, TopologySummaryCommand.TopologySummaryCommandResult>(subscriptionResolver)
{
    private readonly ILogger<TopologySummaryCommand> _logger = logger;
    private readonly INetworkDiagnosticsService _networkDiagnosticsService = networkDiagnosticsService;

    public override async Task<CommandResponse> ExecuteAsync(
        CommandContext context,
        TopologySummaryOptions options,
        CancellationToken cancellationToken)
    {
        try
        {
            var summary = await _networkDiagnosticsService.GetTopologySummaryAsync(
                options.Subscription!,
                options.ResourceGroup,
                options.Tenant,
                cancellationToken);

            context.Response.Results = ResponseResult.Create(
                new TopologySummaryCommandResult(summary),
                NetworkDiagnosticsJsonContext.Default.TopologySummaryCommandResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing Network Topology Summary command. Subscription: {Subscription}", options.Subscription);
            HandleException(context, ex);
        }

        return context.Response;
    }

    public record TopologySummaryCommandResult(NetworkTopologySummaryResult Summary);
}
