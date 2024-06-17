﻿// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Model.Intrinsic;
using Snap.Hutao.Service.Game.Configuration;

namespace Snap.Hutao.Service.Game.Scheme;

/// <summary>
/// 启动方案
/// </summary>
[HighQuality]
internal class LaunchScheme : IEquatable<ChannelOptions>
{
    /// <summary>
    /// 显示名称
    /// </summary>
    public string DisplayName
    {
        get
        {
            string name = (Channel, IsOversea) switch
            {
                (ChannelType.Bili, false) => SH.ModelBindingLaunchGameLaunchSchemeBilibili,
                (_, false) => SH.ModelBindingLaunchGameLaunchSchemeChinese,
                (_, true) => SH.ModelBindingLaunchGameLaunchSchemeOversea,
            };

            return $"{name} | {Channel} | {SubChannel}";
        }
    }

    /// <summary>
    /// 通道
    /// </summary>
    public ChannelType Channel { get; private protected set; }

    /// <summary>
    /// 子通道
    /// </summary>
    public SubChannelType SubChannel { get; private protected set; }

    /// <summary>
    /// 启动器 Id
    /// </summary>
    public int LauncherId { get; private protected set; }

    public string HoyoPlayLauncherId { get; private protected set; } = default!;

    public string HoyoPlayGameId { get; private protected set; } = default!;

    /// <summary>
    /// API Key
    /// </summary>
    public string Key { get; private protected set; } = default!;

    /// <summary>
    /// 是否为海外
    /// </summary>
    public bool IsOversea { get; private protected set; }

    public bool IsNotCompatOnly { get; private protected set; } = true;

    public static bool ExecutableIsOversea(string gameFileName)
    {
        return gameFileName.ToUpperInvariant() switch
        {
            GameConstants.GenshinImpactFileNameUpper => true,
            GameConstants.YuanShenFileNameUpper => false,
            _ => throw Requires.Fail("Invalid game executable file name：{0}", gameFileName),
        };
    }

    [SuppressMessage("", "SH002")]
    public bool Equals(ChannelOptions other)
    {
        return Channel == other.Channel && SubChannel == other.SubChannel && IsOversea == other.IsOversea;
    }

    public bool ExecutableMatches(string gameFileName)
    {
        return (IsOversea, gameFileName) switch
        {
            (true, GameConstants.GenshinImpactFileName) => true,
            (false, GameConstants.YuanShenFileName) => true,
            _ => false,
        };
    }
}