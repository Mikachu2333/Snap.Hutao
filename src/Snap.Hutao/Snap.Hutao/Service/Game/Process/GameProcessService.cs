﻿// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Core;
using Snap.Hutao.Factory.Progress;
using Snap.Hutao.Service.Discord;
using Snap.Hutao.Service.Game.Account;
using Snap.Hutao.Service.Game.Scheme;
using Snap.Hutao.Service.Game.Unlocker;
using System.IO;
using static Snap.Hutao.Service.Game.GameConstants;

namespace Snap.Hutao.Service.Game.Process;

/// <summary>
/// 进程互操作
/// </summary>
[ConstructorGenerated]
[Injection(InjectAs.Singleton, typeof(IGameProcessService))]
internal sealed partial class GameProcessService : IGameProcessService
{
    private readonly IServiceProvider serviceProvider;
    private readonly IProgressFactory progressFactory;
    private readonly IDiscordService discordService;
    private readonly RuntimeOptions runtimeOptions;
    private readonly LaunchOptions launchOptions;

    private volatile bool isGameRunning;

    public bool IsGameRunning()
    {
        if (isGameRunning)
        {
            return true;
        }

        // Original two GetProcessesByName is O(2n)
        // GetProcesses once and manually loop is O(n)
        foreach (ref System.Diagnostics.Process process in System.Diagnostics.Process.GetProcesses().AsSpan())
        {
            if (process.ProcessName is YuanShenProcessName or GenshinImpactProcessName)
            {
                return true;
            }
        }

        return false;
    }

    public async ValueTask LaunchAsync(IProgress<LaunchStatus> progress)
    {
        if (IsGameRunning())
        {
            return;
        }

        if (!launchOptions.TryGetGamePathAndGameFileName(out string gamePath, out string? gameFileName))
        {
            ArgumentException.ThrowIfNullOrEmpty(gamePath);
            return; // null check passing, actually never reach.
        }

        bool isOversea = LaunchScheme.ExecutableIsOversea(gameFileName);

        if (launchOptions.IsWindowsHDREnabled)
        {
            RegistryInterop.SetWindowsHDR(isOversea);
        }

        progress.Report(new(LaunchPhase.ProcessInitializing, SH.ServiceGameLaunchPhaseProcessInitializing));
        using (System.Diagnostics.Process game = InitializeGameProcess(gamePath))
        {
            await using (await GameRunningTracker.CreateAsync(this, isOversea).ConfigureAwait(false))
            {
                game.Start();
                progress.Report(new(LaunchPhase.ProcessStarted, SH.ServiceGameLaunchPhaseProcessStarted));

                if (launchOptions.UseStarwardPlayTimeStatistics)
                {
                    await Starward.LaunchForPlayTimeStatisticsAsync(isOversea).ConfigureAwait(false);
                }

                if (runtimeOptions.IsElevated && launchOptions.IsAdvancedLaunchOptionsEnabled && launchOptions.UnlockFps)
                {
                    progress.Report(new(LaunchPhase.UnlockingFps, SH.ServiceGameLaunchPhaseUnlockingFps));
                    try
                    {
                        await UnlockFpsAsync(game, progress).ConfigureAwait(false);
                    }
                    catch (InvalidOperationException)
                    {
                        // The Unlocker can't unlock the process
                        game.Kill();
                        throw;
                    }
                    finally
                    {
                        progress.Report(new(LaunchPhase.ProcessExited, SH.ServiceGameLaunchPhaseProcessExited));
                    }
                }
                else
                {
                    progress.Report(new(LaunchPhase.WaitingForExit, SH.ServiceGameLaunchPhaseWaitingProcessExit));
                    await game.WaitForExitAsync().ConfigureAwait(false);
                    progress.Report(new(LaunchPhase.ProcessExited, SH.ServiceGameLaunchPhaseProcessExited));
                }
            }
        }
    }

    private System.Diagnostics.Process InitializeGameProcess(string gamePath)
    {
        string commandLine = string.Empty;

        if (launchOptions.IsEnabled)
        {
            // https://docs.unity.cn/cn/current/Manual/PlayerCommandLineArguments.html
            // https://docs.unity3d.com/2017.4/Documentation/Manual/CommandLineArguments.html
            commandLine = new CommandLineBuilder()
                .AppendIf(launchOptions.IsBorderless, "-popupwindow")
                .AppendIf(launchOptions.IsExclusive, "-window-mode", "exclusive")
                .Append("-screen-fullscreen", launchOptions.IsFullScreen ? 1 : 0)
                .AppendIf(launchOptions.IsScreenWidthEnabled, "-screen-width", launchOptions.ScreenWidth)
                .AppendIf(launchOptions.IsScreenHeightEnabled, "-screen-height", launchOptions.ScreenHeight)
                .AppendIf(launchOptions.IsMonitorEnabled, "-monitor", launchOptions.Monitor.Value)
                .AppendIf(launchOptions.IsUseCloudThirdPartyMobile, "-platform_type CLOUD_THIRD_PARTY_MOBILE")
                .ToString();
        }

        return new()
        {
            StartInfo = new()
            {
                Arguments = commandLine,
                FileName = gamePath,
                UseShellExecute = true,
                Verb = "runas",
                WorkingDirectory = Path.GetDirectoryName(gamePath),
            },
        };
    }

    private ValueTask UnlockFpsAsync(System.Diagnostics.Process game, IProgress<LaunchStatus> progress, CancellationToken token = default)
    {
#pragma warning disable CA1859
        IGameFpsUnlocker unlocker = serviceProvider.CreateInstance<GameFpsUnlocker>(game);
#pragma warning restore CA1859
        UnlockTimingOptions options = new(100, 20000, 3000);
        IProgress<UnlockerStatus> lockerProgress = progressFactory.CreateForMainThread<UnlockerStatus>(unlockStatus => progress.Report(LaunchStatus.FromUnlockStatus(unlockStatus)));
        return unlocker.UnlockAsync(options, lockerProgress, token);
    }

    private class GameRunningTracker : IAsyncDisposable
    {
        private readonly GameProcessService service;
        private readonly bool previousSetDiscordActivityWhenPlaying;

        private GameRunningTracker(GameProcessService service, bool isOversea)
        {
            service.isGameRunning = true;
            previousSetDiscordActivityWhenPlaying = service.launchOptions.SetDiscordActivityWhenPlaying;
            this.service = service;
        }

        public static async ValueTask<GameRunningTracker> CreateAsync(GameProcessService service, bool isOversea)
        {
            GameRunningTracker tracker = new(service, isOversea);
            if (tracker.previousSetDiscordActivityWhenPlaying)
            {
                await service.discordService.SetPlayingActivityAsync(isOversea).ConfigureAwait(false);
            }

            return tracker;
        }

        public async ValueTask DisposeAsync()
        {
            if (previousSetDiscordActivityWhenPlaying)
            {
                await service.discordService.SetNormalActivityAsync().ConfigureAwait(false);
            }

            service.isGameRunning = false;
        }
    }
}