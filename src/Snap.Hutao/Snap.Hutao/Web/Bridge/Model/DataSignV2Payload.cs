﻿// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Web.Bridge.Model;

internal sealed class DataSignV2Payload
{
    [JsonPropertyName("query")]
    public Dictionary<string, JsonElement> Query { get; set; } = default!;

    [JsonPropertyName("body")]
    public string Body { get; set; } = default!;

    [SuppressMessage("", "CA1308")]
    public string GetQueryParam()
    {
        // TODO : improve here.
        IEnumerable<string> parts = Query
            .OrderBy(x => x.Key)
            .Select(x => x.Value.ValueKind is JsonValueKind.True or JsonValueKind.False
                ? $"{x.Key}={x.Value.ToString().ToLowerInvariant()}"
                : $"{x.Key}={x.Value}");

        return string.Join('&', parts);
    }
}