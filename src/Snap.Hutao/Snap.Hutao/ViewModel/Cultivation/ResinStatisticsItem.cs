// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.ComponentModel;
using JetBrains.Annotations;
using Snap.Hutao.Core.ExceptionService;
using Snap.Hutao.Model.Metadata.Item;

namespace Snap.Hutao.ViewModel.Cultivation;

internal sealed partial class ResinStatisticsItem : ObservableObject
{
    private readonly bool canUseCondensedResin;
    private readonly ResinStatisticsItemKind kind;

    public ResinStatisticsItem(string title, ResinStatisticsItemKind kind, int resinPerBlossom, bool canUseCondensedResin)
    {
        this.kind = kind;
        this.canUseCondensedResin = canUseCondensedResin;
        Title = title;
        ResinPerBlossom = resinPerBlossom;
        SelectedDropDistribution = MaterialDropDistribution.Nine;
    }

    public string Title { get; }

    public int ResinPerBlossom { get; }

    [AllowNull]
    public MaterialDropDistribution SelectedDropDistribution
    {
        get;
        set
        {
            if (value is not null && SetProperty(ref field, value))
            {
                OnPropertyChanged(nameof(TotalResin));
                OnPropertyChanged(nameof(CondensedResin));
                OnPropertyChanged(nameof(Days));
            }
        }
    }

    public double RawItemCount { get; set; }

    [UsedImplicitly]
    public bool HasData
    {
        get => RawItemCount > 0D;
    }

    public int TotalResin
    {
        get => RawTimes * ResinPerBlossom;
    }

    public int? CondensedResin
    {
        get => canUseCondensedResin ? (int)Math.Ceiling(TotalResin / 40D) : null;
    }

    public string Days
    {
        get => SH.FormatViewModelCultivationResinStatisticsItemRemainDays((int)Math.Ceiling(TotalResin / (1440D / 8)));
    }

    internal int RawTimes
    {
        get
        {
            double expectation = kind switch
            {
                ResinStatisticsItemKind.BlossomOfWealth => SelectedDropDistribution.BlossomOfWealth,
                ResinStatisticsItemKind.BlossomOfRevelation => SelectedDropDistribution.BlossomOfRevelation,
                ResinStatisticsItemKind.TalentAscension => SelectedDropDistribution.TalentBooks,
                ResinStatisticsItemKind.WeaponAscension => SelectedDropDistribution.WeaponAscension,
                ResinStatisticsItemKind.NormalBoss => SelectedDropDistribution.NormalBoss,
                ResinStatisticsItemKind.WeeklyBoss => SelectedDropDistribution.WeeklyBoss,
                _ => throw HutaoException.NotSupported(),
            };

            return (int)Math.Ceiling(RawItemCount / expectation);
        }
    }
}