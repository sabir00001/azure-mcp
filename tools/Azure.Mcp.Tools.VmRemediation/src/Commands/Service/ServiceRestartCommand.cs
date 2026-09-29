// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Mcp.Core.Commands.Subscription;
using Azure.Mcp.Core.Services.Azure.Subscription;
using Azure.Mcp.Tools.VmRemediation.Models;
using Azure.Mcp.Tools.VmRemediation.Options.Service;
using Azure.Mcp.Tools.VmRemediation.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Mcp.Core.Commands;
using Microsoft.Mcp.Core.Models.Command;

namespace Azure.Mcp.Tools.VmRemediation.Commands.Service;

[CommandMetadata(
    Id = "91f5c2b8-4f59-4c6d-9c53-0f1fbe7b1d01",
    Name = "restart",
    Title = "Restart Allowlisted VM Service",
    Description = "Safely starts or restarts an allowlisted Windows or Linux VM service after VM health, approval, and rate-limit checks.",
    OperationPlane = ToolOperationPlane.Control,
    Destructive = true,
    Idempotent = false,
    OpenWorld = false,
    ReadOnly = false,
    Secret = false,
    LocalRequired = false)]
public sealed class ServiceRestartCommand(
    ILogger<ServiceRestartCommand> logger,
    IVmRemediationService remediationService,
    ISubscriptionResolver subscriptionResolver)
    : SubscriptionCommand<ServiceRestartOptions, ServiceRestartCommand.ServiceRestartCommandResult>(subscriptionResolver)
{
    private readonly ILogger<ServiceRestartCommand> _logger = logger;
    private readonly IVmRemediationService _remediationService = remediationService;

    public override void ValidateOptions(ServiceRestartOptions options, ValidationResult validationResult)
    {
        base.ValidateOptions(options, validationResult);
        if (string.IsNullOrWhiteSpace(options.IncidentId))
        {
            validationResult.Errors.Add("--incident-id is required for every remediation action.");
        }
    }

    public override async Task<CommandResponse> ExecuteAsync(CommandContext context, ServiceRestartOptions options, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _remediationService.RestartServiceAsync(
                options.Vm,
                options.ResourceGroup,
                options.Service,
                options.IncidentId,
                options.Subscription!,
                options.Tenant,
                cancellationToken);
            context.Response.Results = ResponseResult.Create(new(result), VmRemediationJsonContext.Default.ServiceRestartCommandResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error remediating VM service. VM: {Vm}, Service: {Service}, IncidentId: {IncidentId}", options.Vm, options.Service, options.IncidentId);
            HandleException(context, ex);
        }

        return context.Response;
    }

    public sealed record ServiceRestartCommandResult(RemediationResult Result);
}
