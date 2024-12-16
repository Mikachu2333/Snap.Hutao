// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Core.Database.Abstraction;
using Snap.Hutao.Model;
using Snap.Hutao.Model.Entity.Database;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Runtime.InteropServices;

namespace Snap.Hutao.Core.Database;

internal sealed class ObservableReorderableDbCollection<TEntity> : ObservableCollection<TEntity>
    where TEntity : class, IReorderable
{
    private readonly IServiceProvider serviceProvider;

    public ObservableReorderableDbCollection(List<TEntity> items, IServiceProvider serviceProvider)
        : base(AdjustIndex(items.SortBy(x => x.Index))) // Normalized index
    {
        this.serviceProvider = serviceProvider;
    }

    protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        base.OnCollectionChanged(e);

        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Remove:
            case NotifyCollectionChangedAction.Add:
                OnReorder();
                break;
        }
    }

    private static List<TEntity> AdjustIndex(List<TEntity> list)
    {
        Span<TEntity> span = CollectionsMarshal.AsSpan(list);
        for (int i = 0; i < span.Length; i++)
        {
            span[i].Index = i;
        }

        return list;
    }

    private void OnReorder()
    {
        AdjustIndex((List<TEntity>)Items);

        using (IServiceScope scope = serviceProvider.CreateScope())
        {
            scope.ServiceProvider.GetRequiredService<AppDbContext>().Set<TEntity>().UpdateRangeAndSave(Items);
        }
    }
}

[SuppressMessage("", "SA1402")]
internal sealed class ObservableReorderableDbCollection<TEntityAccess, TEntity> : ObservableCollection<TEntityAccess>
    where TEntityAccess : class, IEntityAccess<TEntity>
    where TEntity : class, IReorderable
{
    private readonly IServiceProvider serviceProvider;

    public ObservableReorderableDbCollection(List<TEntityAccess> items, IServiceProvider serviceProvider)
        : base(AdjustIndex(items.SortBy(x => x.Entity.Index)))
    {
        this.serviceProvider = serviceProvider;
    }

    protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        base.OnCollectionChanged(e);

        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Remove:
            case NotifyCollectionChangedAction.Add:
                OnReorder();
                break;
        }
    }

    private static List<TEntityAccess> AdjustIndex(List<TEntityAccess> list)
    {
        Span<TEntityAccess> span = CollectionsMarshal.AsSpan(list);
        for (int i = 0; i < span.Length; i++)
        {
            span[i].Entity.Index = i;
        }

        return list;
    }

    private static TEntity AccessEntity(TEntityAccess access)
    {
        return access.Entity;
    }

    private void OnReorder()
    {
        AdjustIndex((List<TEntityAccess>)Items);

        using (IServiceScope scope = serviceProvider.CreateScope())
        {
            scope.ServiceProvider.GetRequiredService<AppDbContext>().Set<TEntity>().UpdateRangeAndSave(Items.Select(AccessEntity));
        }
    }
}