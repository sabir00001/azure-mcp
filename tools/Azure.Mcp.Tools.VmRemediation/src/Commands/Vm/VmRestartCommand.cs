// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Mcp.Core.Commands.Subscription;
using Azure.Mcp.Core.Services.Azure.Subscription;
using Azure.Mcp.Tools.VmRemediation.Models;
using Azure.Mcp.Tools.VmRemediation.Options.Vm;
using Azure.Mcp.Tools.VmRemediation.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Mcp.Core.Commands;
using Microsoft.Mcp.Core.Models.Command;

namespace Azure.Mcp.Tools.VmRemediation.Commands.Vm;

[CommandMetadata(
    Id = "d2d8cc4d-86bb-4dc9-9fd4-1d0ce2c7c802",
    Name = "restart",
    Title = "Restart Virtual Machine with Approval",
    Description = "Requests or executes a VM restart only after instance-view, guest-agent, ITSM approval, and Activity Log rate-limit checks.",
    OperationPlane = ToolOperationPlane.Control,
    Destructive = true,
    Idempotent = false,
    OpenWorld = false,
    ReadOnly = false,
    Secret = false,
    LocalRequired = false)]
public sealed class VmRestartCommand(
    ILogger<VmRestartCommand> logger,
    IVmRemediationService remediationService,
    ISubscriptionResolver subscriptionResolver)
    : SubscriptionCommand<VmRestartOptions, VmRestartCommand.VmRestartCommandResult>(subscriptionResolver)
{
    private readonly ILogger<VmRestartCommand> _logger = logger;
    private readonly IVmRemediationService _remediationService = remediationService;

    public override void ValidateOptions(VmRestartOptions options, ValidationResult validationResult)
    {
        base.ValidateOptions(options, validationResult);
        if (string.IsNullOrWhiteSpace(options.IncidentId))
        {
            validationResult.Errors.Add("--incident-id is required for every remediation action.");
        }
    }

    public override async Task<CommandResponse> ExecuteAsync(CommandContext context, VmRestartOptions options, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _remediationService.RestartVmAsync(
                options.Vm,
                options.ResourceGroup,
                options.IncidentId,
                options.Subscription!,
                options.Tenant,
                cancellationToken);
            context.Response.Results = ResponseResult.Create(new(result), VmRemediationJsonContext.Default.VmRestartCommandResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error remediating VM restart. VM: {Vm}, IncidentId: {IncidentId}", options.Vm, options.IncidentId);
            HandleException(context, ex);
        }

        return context.Response;
    }

    public sealed record VmRestartCommandResult(RemediationResult Result);
}
