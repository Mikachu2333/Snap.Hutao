// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Core.IO.Ini;
using Snap.Hutao.Service.Game.Configuration;
using System.Collections.Immutable;
using System.IO;
using System.Runtime.InteropServices;

namespace Snap.Hutao.Service.Game.Launching.Handler;

internal sealed class LaunchExecutionSetChannelOptionsHandler : ILaunchExecutionDelegateHandler
{
    public async ValueTask OnExecutionAsync(LaunchExecutionContext context, LaunchExecutionDelegate next)
    {
        if (!context.TryGetGameFileSystem(out IGameFileSystem? gameFileSystem))
        {
            // context.Result is set in TryGetGameFileSystem
            return;
        }

        string configPath = gameFileSystem.GetGameConfigurationFilePath();
        context.Logger.LogInformation("Game config file path: {ConfigPath}", configPath);

        IniElement[]? elements;
        try
        {
            elements = ImmutableCollectionsMarshal.AsArray(IniSerializer.DeserializeFromFile(configPath));
            ArgumentNullException.ThrowIfNull(elements);
        }
        catch (FileNotFoundException)
        {
            context.Result.Kind = LaunchExecutionResultKind.GameConfigFileNotFound;
            context.Result.ErrorMessage = SH.FormatServiceGameSetMultiChannelConfigFileNotFound(configPath);
            return;
        }
        catch (DirectoryNotFoundException)
        {
            context.Result.Kind = LaunchExecutionResultKind.GameConfigDirectoryNotFound;
            context.Result.ErrorMessage = SH.FormatServiceGameSetMultiChannelConfigFileNotFound(configPath);
            return;
        }
        catch (UnauthorizedAccessException)
        {
            context.Result.Kind = LaunchExecutionResultKind.GameConfigInsufficientPermissions;
            context.Result.ErrorMessage = SH.ServiceGameSetMultiChannelUnauthorizedAccess;
            return;
        }

        SetChannelOptions(context, elements);

        if (context.ChannelOptionsChanged)
        {
            IniSerializer.SerializeToFile(configPath, elements);
        }

        await next().ConfigureAwait(false);
    }

    private static void SetChannelOptions(LaunchExecutionContext context, IniElement[] elements)
    {
        foreach (ref IniElement element in elements.AsSpan())
        {
            if (element is not IniParameter parameter)
            {
                continue;
            }

            switch (parameter.Key)
            {
                case ChannelOptions.ChannelName:
                    {
                        element = parameter.WithValue(context.TargetScheme.Channel.ToString("D"), out bool changed);
                        context.ChannelOptionsChanged = changed || context.ChannelOptionsChanged;
                        continue;
                    }

                case ChannelOptions.SubChannelName:
                    {
                        element = parameter.WithValue(context.TargetScheme.SubChannel.ToString("D"), out bool changed);
                        context.ChannelOptionsChanged = changed || context.ChannelOptionsChanged;
                        continue;
                    }
            }
        }
    }
}