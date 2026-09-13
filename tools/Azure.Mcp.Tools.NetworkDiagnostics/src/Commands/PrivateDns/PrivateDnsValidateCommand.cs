// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Mcp.Core.Commands.Subscription;
using Azure.Mcp.Core.Services.Azure.Subscription;
using Azure.Mcp.Tools.NetworkDiagnostics.Models;
using Azure.Mcp.Tools.NetworkDiagnostics.Options.PrivateDns;
using Azure.Mcp.Tools.NetworkDiagnostics.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Mcp.Core.Commands;
using Microsoft.Mcp.Core.Models.Command;

namespace Azure.Mcp.Tools.NetworkDiagnostics.Commands.PrivateDns;

[CommandMetadata(
    Id = "3c0a5d42-9604-5332-ad03-401b03cd3003",
    Name = "validate",
    Title = "Private DNS Zone Validation",
    Description = "Validates Private DNS zones, linked virtual networks, and registered DNS records for private endpoints.",
    OperationPlane = ToolOperationPlane.Control,
    Destructive = false,
    Idempotent = true,
    OpenWorld = false,
    ReadOnly = true,
    Secret = false,
    LocalRequired = false)]
public sealed class PrivateDnsValidateCommand(
    ILogger<PrivateDnsValidateCommand> logger,
    INetworkDiagnosticsService networkDiagnosticsService,
    ISubscriptionResolver subscriptionResolver)
    : SubscriptionCommand<PrivateDnsValidateOptions, PrivateDnsValidateCommand.PrivateDnsValidateCommandResult>(subscriptionResolver)
{
    private readonly ILogger<PrivateDnsValidateCommand> _logger = logger;
    private readonly INetworkDiagnosticsService _networkDiagnosticsService = networkDiagnosticsService;

    public override async Task<CommandResponse> ExecuteAsync(
        CommandContext context,
        PrivateDnsValidateOptions options,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _networkDiagnosticsService.ValidatePrivateDnsAsync(
                options.Subscription!,
                options.ResourceGroup,
                options.Tenant,
                cancellationToken);

            context.Response.Results = ResponseResult.Create(
                new PrivateDnsValidateCommandResult(result),
                NetworkDiagnosticsJsonContext.Default.PrivateDnsValidateCommandResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing Private DNS Validation command. Subscription: {Subscription}", options.Subscription);
            HandleException(context, ex);
        }

        return context.Response;
    }

    public record PrivateDnsValidateCommandResult(PrivateDnsValidationResult Result);
}
