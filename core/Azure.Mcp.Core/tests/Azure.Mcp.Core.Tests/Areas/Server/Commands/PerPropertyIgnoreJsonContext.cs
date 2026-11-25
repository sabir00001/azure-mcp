// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Azure.Mcp.Core.Tests.Areas.Server.Commands;

[JsonSerializable(typeof(IgnoreConditionSample))]
[JsonSerializable(typeof(NestedIgnoreConditionSample))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
internal sealed partial class PerPropertyIgnoreJsonContext : JsonSerializerContext;
