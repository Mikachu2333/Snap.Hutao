﻿// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.Input;
using Snap.Hutao.Core.Setting;
using Snap.Hutao.Web.Hutao.HutaoAsAService;
using Snap.Hutao.Web.Response;
using System.Collections.ObjectModel;
using System.Globalization;
using Windows.Storage;
using HutaoAnnouncement = Snap.Hutao.Web.Hutao.HutaoAsAService.Announcement;

namespace Snap.Hutao.Service.Hutao;

[ConstructorGenerated]
[Injection(InjectAs.Scoped, typeof(IHutaoAsAService))]
internal sealed partial class HutaoAsAService : IHutaoAsAService
{
    private const int AnnouncementDuration = 30;
    private readonly IServiceScopeFactory serviceScopeFactory;

    private ObservableCollection<HutaoAnnouncement>? announcements;

    public async ValueTask<ObservableCollection<HutaoAnnouncement>> GetHutaoAnnouncementCollectionAsync(CancellationToken token = default)
    {
        if (announcements is null)
        {
            RelayCommand<HutaoAnnouncement> dismissCommand = new(DismissAnnouncement);

            ApplicationDataCompositeValue excludedIds = LocalSetting.Get(SettingKeys.ExcludedAnnouncementIds, []);
            List<long> data = excludedIds.Select(kvp => long.Parse(kvp.Key, CultureInfo.InvariantCulture)).ToList();

            List<HutaoAnnouncement>? list;
            using (IServiceScope scope = serviceScopeFactory.CreateScope())
            {
                HutaoAsAServiceClient hutaoAsAServiceClient = scope.ServiceProvider.GetRequiredService<HutaoAsAServiceClient>();
                Response<List<HutaoAnnouncement>> response = await hutaoAsAServiceClient.GetAnnouncementListAsync(data, token).ConfigureAwait(false);

                if (!ResponseValidator.TryValidate(response, scope.ServiceProvider, out list))
                {
                    return [];
                }
            }

            foreach (HutaoAnnouncement item in list)
            {
                item.DismissCommand = dismissCommand;
            }

            announcements = list.ToObservableCollection();
        }

        return announcements;
    }

    private void DismissAnnouncement(HutaoAnnouncement? announcement)
    {
        if (announcement is not null && announcements is not null)
        {
            ApplicationDataCompositeValue excludedIds = LocalSetting.Get(SettingKeys.ExcludedAnnouncementIds, []);

            foreach ((string key, object value) in excludedIds)
            {
                if (value is DateTimeOffset time && time < DateTimeOffset.UtcNow - TimeSpan.FromDays(AnnouncementDuration))
                {
                    excludedIds.Remove(key);
                }
            }

            excludedIds.TryAdd($"{announcement.Id}", DateTimeOffset.UtcNow + TimeSpan.FromDays(AnnouncementDuration));
            LocalSetting.Set(SettingKeys.ExcludedAnnouncementIds, excludedIds);

            announcements.Remove(announcement);
        }
    }
}