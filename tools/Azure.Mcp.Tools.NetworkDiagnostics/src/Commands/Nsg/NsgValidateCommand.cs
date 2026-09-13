// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Mcp.Core.Commands.Subscription;
using Azure.Mcp.Core.Services.Azure.Subscription;
using Azure.Mcp.Tools.NetworkDiagnostics.Models;
using Azure.Mcp.Tools.NetworkDiagnostics.Options.Nsg;
using Azure.Mcp.Tools.NetworkDiagnostics.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Mcp.Core.Commands;
using Microsoft.Mcp.Core.Models.Command;

namespace Azure.Mcp.Tools.NetworkDiagnostics.Commands.Nsg;

[CommandMetadata(
    Id = "2b9f4c31-85f3-4221-9c02-300a02bc2002",
    Name = "validate",
    Title = "Network Security Group Rule Validation",
    Description = "Evaluates effective NSG security rules for a specified direction, protocol, source address, and destination port to determine allow/deny decisions.",
    OperationPlane = ToolOperationPlane.Control,
    Destructive = false,
    Idempotent = true,
    OpenWorld = false,
    ReadOnly = true,
    Secret = false,
    LocalRequired = false)]
public sealed class NsgValidateCommand(
    ILogger<NsgValidateCommand> logger,
    INetworkDiagnosticsService networkDiagnosticsService,
    ISubscriptionResolver subscriptionResolver)
    : SubscriptionCommand<NsgValidateOptions, NsgValidateCommand.NsgValidateCommandResult>(subscriptionResolver)
{
    private readonly ILogger<NsgValidateCommand> _logger = logger;
    private readonly INetworkDiagnosticsService _networkDiagnosticsService = networkDiagnosticsService;

    public override async Task<CommandResponse> ExecuteAsync(
        CommandContext context,
        NsgValidateOptions options,
        CancellationToken cancellationToken)
    {
        try
        {
            var validation = await _networkDiagnosticsService.ValidateNsgRulesAsync(
                options.Nsg,
                options.ResourceGroup,
                options.Subscription!,
                options.Direction,
                options.Protocol,
                options.SourceAddress,
                options.DestinationPort,
                options.Tenant,
                cancellationToken);

            context.Response.Results = ResponseResult.Create(
                new NsgValidateCommandResult(validation),
                NetworkDiagnosticsJsonContext.Default.NsgValidateCommandResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing NSG Rule Validation command. NSG: {Nsg}, Subscription: {Subscription}", options.Nsg, options.Subscription);
            HandleException(context, ex);
        }

        return context.Response;
    }

    public record NsgValidateCommandResult(NsgValidationResult Result);
}
