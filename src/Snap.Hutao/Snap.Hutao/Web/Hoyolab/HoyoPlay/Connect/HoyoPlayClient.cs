﻿// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Core.DependencyInjection.Annotation.HttpClient;
using Snap.Hutao.Service.Game.Scheme;
using Snap.Hutao.Web.Hoyolab.HoyoPlay.Connect.ChannelSDK;
using Snap.Hutao.Web.Hoyolab.HoyoPlay.Connect.DeprecatedFile;
using Snap.Hutao.Web.Hoyolab.HoyoPlay.Connect.Package;
using Snap.Hutao.Web.Request.Builder;
using Snap.Hutao.Web.Request.Builder.Abstraction;
using Snap.Hutao.Web.Response;
using System.Net.Http;

namespace Snap.Hutao.Web.Hoyolab.HoyoPlay.Connect;

[ConstructorGenerated(ResolveHttpClient = true)]
[HttpClient(HttpClientConfiguration.Default)]
internal sealed partial class HoyoPlayClient
{
    private readonly IHttpRequestMessageBuilderFactory httpRequestMessageBuilderFactory;
    private readonly HttpClient httpClient;
    private readonly ILogger<HoyoPlayClient> logger;

    public async ValueTask<Response<GamePackages>> GetPackagesAsync(LaunchScheme scheme, CancellationToken token = default)
    {
        string url = scheme.IsOversea
            ? ApiOsEndpoints.SgHoyoPlayConnectGamePackages(scheme)
            : ApiEndpoints.HoyoPlayConnectGamePackages(scheme);

        HttpRequestMessageBuilder builder = httpRequestMessageBuilderFactory.Create()
            .SetRequestUri(url)
            .Get();

        Response<GamePackages>? resp = await builder
            .SendAsync<Response<GamePackages>>(httpClient, logger, token)
            .ConfigureAwait(false);

        return Response.Response.DefaultIfNull(resp);
    }

    public async ValueTask<Response<ChannelSDKs>> GetChannelSDKAsync(LaunchScheme scheme, CancellationToken token = default)
    {
        string url = scheme.IsOversea
            ? ApiOsEndpoints.SgHoyoPlayConnectGameChannelSDKs(scheme)
            : ApiEndpoints.HoyoPlayConnectGameChannelSDKs(scheme);

        HttpRequestMessageBuilder builder = httpRequestMessageBuilderFactory.Create()
            .SetRequestUri(url)
            .Get();

        Response<ChannelSDKs>? resp = await builder
            .SendAsync<Response<ChannelSDKs>>(httpClient, logger, token)
            .ConfigureAwait(false);

        return Response.Response.DefaultIfNull(resp);
    }

    public async ValueTask<Response<DeprecatedFileConfigs>> GetDeprecatedFilesAsync(LaunchScheme scheme, CancellationToken token = default)
    {
        string url = scheme.IsOversea
            ? ApiOsEndpoints.SgHoyoPlayConnectDeprecatedFileConfigs(scheme)
            : ApiEndpoints.HoyoPlayConnectDeprecatedFileConfigs(scheme);

        HttpRequestMessageBuilder builder = httpRequestMessageBuilderFactory.Create()
            .SetRequestUri(url)
            .Get();

        Response<DeprecatedFileConfigs>? resp = await builder
            .SendAsync<Response<DeprecatedFileConfigs>>(httpClient, logger, token)
            .ConfigureAwait(false);

        return Response.Response.DefaultIfNull(resp);
    }
}
