// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;
using Azure.Mcp.Tools.VmRemediation.Commands.Service;
using Azure.Mcp.Tools.VmRemediation.Commands.Vm;
using Azure.Mcp.Tools.VmRemediation.Models;

namespace Azure.Mcp.Tools.VmRemediation;

[JsonSerializable(typeof(ServiceRestartCommand.ServiceRestartCommandResult))]
[JsonSerializable(typeof(VmRestartCommand.VmRestartCommandResult))]
[JsonSerializable(typeof(RemediationResult))]
[JsonSerializable(typeof(RemediationState))]
[JsonSerializable(typeof(RemediationApproval))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
internal partial class VmRemediationJsonContext : JsonSerializerContext;
