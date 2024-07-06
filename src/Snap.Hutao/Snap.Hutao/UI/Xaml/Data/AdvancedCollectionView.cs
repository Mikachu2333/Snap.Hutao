// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.WinUI.Collections;
using CommunityToolkit.WinUI.Helpers;
using Microsoft.UI.Xaml.Data;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Runtime.CompilerServices;
using Windows.Foundation;
using Windows.Foundation.Collections;
using NotifyCollectionChangedAction = System.Collections.Specialized.NotifyCollectionChangedAction;

namespace Snap.Hutao.UI.Xaml.Data;

internal class AdvancedCollectionView<T> : IAdvancedCollectionView<T>, INotifyPropertyChanged, ISupportIncrementalLoading, IComparer<T>
    where T : class, IAdvancedCollectionViewItem
{
    private readonly List<T> view;
    private readonly ObservableCollection<SortDescription> sortDescriptions;
    private readonly bool liveShapingEnabled;
    private readonly HashSet<string?> observedFilterProperties = [];

    private IList<T> source;
    private Predicate<T>? filter;
    private int deferCounter;
    private WeakEventListener<AdvancedCollectionView<T>, object?, NotifyCollectionChangedEventArgs>? sourceWeakEventListener;

    public AdvancedCollectionView()
        : this([])
    {
    }

    public AdvancedCollectionView(IList<T> source, bool isLiveShaping = true)
    {
        liveShapingEnabled = isLiveShaping;
        view = [];
        sortDescriptions = [];
        sortDescriptions.CollectionChanged += SortDescriptionsCollectionChanged;
        Source = source;
    }

    public event EventHandler<object>? CurrentChanged;

    public event CurrentChangingEventHandler? CurrentChanging;

    public event PropertyChangedEventHandler? PropertyChanged;

    public event VectorChangedEventHandler<object>? VectorChanged;

    public IList<T> Source
    {
        get => source;

        [MemberNotNull(nameof(source))]
        set
        {
            if (ReferenceEquals(source, value))
            {
                return;
            }

            if (source is not null)
            {
                DetachPropertyChangedHandler(source);
            }

            source = value;
            AttachPropertyChangedHandler(source);

            sourceWeakEventListener?.Detach();

            if (source is INotifyCollectionChanged sourceINCC)
            {
                sourceWeakEventListener = new WeakEventListener<AdvancedCollectionView<T>, object?, NotifyCollectionChangedEventArgs>(this)
                {
                    OnEventAction = static (target, source, args) => target.SourceNotifyCollectionChangedCollectionChanged(args),
                    OnDetachAction = (listener) => sourceINCC.CollectionChanged -= listener.OnEvent,
                };
                sourceINCC.CollectionChanged += sourceWeakEventListener.OnEvent;
            }

            HandleSourceChanged();
            OnPropertyChanged();
        }
    }

    public int Count
    {
        get => view.Count;
    }

    public bool IsReadOnly
    {
        get => source is null;
    }

    public IObservableVector<object> CollectionGroups
    {
        get => default!;
    }

    public T? CurrentItem
    {
        get => CurrentPosition > -1 && CurrentPosition < view.Count ? view[CurrentPosition] : default;
        set => MoveCurrentTo(value);
    }

    public int CurrentPosition
    {
        get;
        private set;
    }

    public bool HasMoreItems
    {
        get => source is ISupportIncrementalLoading { HasMoreItems: true };
    }

    public bool IsCurrentAfterLast
    {
        get => CurrentPosition >= view.Count;
    }

    public bool IsCurrentBeforeFirst
    {
        get => CurrentPosition < 0;
    }

    public bool CanFilter
    {
        get => true;
    }

    public Predicate<T>? Filter
    {
        get => filter;
        set
        {
            if (filter == value)
            {
                return;
            }

            filter = value;
            HandleFilterChanged();
        }
    }

    public bool CanSort
    {
        get => true;
    }

    public ObservableCollection<SortDescription> SortDescriptions
    {
        get => sortDescriptions;
    }

    public IList<T> SourceCollection
    {
        get => source;
    }

    public List<T> View
    {
        get => view;
    }

    public T this[int index]
    {
        get => view[index];
        set => view[index] = value;
    }

    public void Refresh()
    {
        HandleSourceChanged();
    }

    public void RefreshFilter()
    {
        HandleFilterChanged();
    }

    public void RefreshSorting()
    {
        HandleSortChanged();
    }

    public IEnumerator<T> GetEnumerator()
    {
        return view.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return view.GetEnumerator();
    }

    public void Add(T item)
    {
        source.Add(item);
    }

    public void Clear()
    {
        source.Clear();
    }

    public bool Contains(T item)
    {
        return view.Contains(item);
    }

    public void CopyTo(T[] array, int arrayIndex)
    {
        view.CopyTo(array, arrayIndex);
    }

    public bool Remove(T item)
    {
        return source.Remove(item);
    }

    public int IndexOf(T item)
    {
        return view.IndexOf(item);
    }

    public void Insert(int index, T item)
    {
        source.Insert(index, item);
    }

    public void RemoveAt(int index)
    {
        Remove(view[index]);
    }

    [SuppressMessage("", "SH007")]
    public bool MoveCurrentTo(T? item)
    {
        return (item is not null && item.Equals(CurrentItem)) || MoveCurrentToIndex(IndexOf(item!));
    }

    public bool MoveCurrentToPosition(int index)
    {
        return MoveCurrentToIndex(index);
    }

    public bool MoveCurrentToFirst()
    {
        return MoveCurrentToIndex(0);
    }

    public bool MoveCurrentToLast()
    {
        return MoveCurrentToIndex(view.Count - 1);
    }

    public bool MoveCurrentToNext()
    {
        return MoveCurrentToIndex(CurrentPosition + 1);
    }

    public bool MoveCurrentToPrevious()
    {
        return MoveCurrentToIndex(CurrentPosition - 1);
    }

    public IAsyncOperation<LoadMoreItemsResult>? LoadMoreItemsAsync(uint count)
    {
        return (source as ISupportIncrementalLoading)?.LoadMoreItemsAsync(count);
    }

    public void ObserveFilterProperty(string propertyName)
    {
        observedFilterProperties.Add(propertyName);
    }

    public void ClearObservedFilterProperties()
    {
        observedFilterProperties.Clear();
    }

    public IDisposable DeferRefresh()
    {
        return new NotificationDeferrer(this);
    }

    int IComparer<T>.Compare(T? x, T? y)
    {
        foreach (SortDescription sd in sortDescriptions)
        {
            object? cx, cy;

            if (string.IsNullOrEmpty(sd.PropertyName))
            {
                cx = x;
                cy = y;
            }
            else
            {
                cx = x?.GetPropertyValue(sd.PropertyName);
                cy = y?.GetPropertyValue(sd.PropertyName);
            }

            int cmp = sd.Comparer.Compare(cx, cy);

            if (cmp is not 0)
            {
                return sd.Direction is SortDirection.Ascending ? +cmp : -cmp;
            }
        }

        return 0;
    }

    protected virtual void OnCurrentChangedOverride()
    {
    }

    private void OnPropertyChanged([CallerMemberName] string propertyName = default!)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void ItemOnPropertyChanged(object? item, PropertyChangedEventArgs e)
    {
        if (!liveShapingEnabled)
        {
            return;
        }

        ArgumentNullException.ThrowIfNull(item);
        T typedItem = (T)item;

        bool? filterResult = filter?.Invoke(typedItem);

        if (filterResult.HasValue && observedFilterProperties.Contains(e.PropertyName))
        {
            int viewIndex = view.IndexOf(typedItem);
            if (viewIndex != -1 && !filterResult.Value)
            {
                RemoveFromView(viewIndex, typedItem);
            }
            else if (viewIndex == -1 && filterResult.Value)
            {
                int index = source.IndexOf(typedItem);
                HandleSourceItemAdded(index, typedItem);
            }
        }

        if ((filterResult ?? true) && SortDescriptions.Any(sd => sd.PropertyName == e.PropertyName))
        {
            int oldIndex = view.IndexOf(typedItem);

            // Check if item is in view:
            if (oldIndex < 0)
            {
                return;
            }

            view.RemoveAt(oldIndex);
            int targetIndex = view.BinarySearch(typedItem, this);
            if (targetIndex < 0)
            {
                targetIndex = ~targetIndex;
            }

            // Only trigger expensive UI updates if the index really changed:
            if (targetIndex != oldIndex)
            {
                OnVectorChanged(new VectorChangedEventArgs(CollectionChange.ItemRemoved, oldIndex, typedItem));

                view.Insert(targetIndex, typedItem);

                OnVectorChanged(new VectorChangedEventArgs(CollectionChange.ItemInserted, targetIndex, typedItem));
            }
            else
            {
                view.Insert(targetIndex, typedItem);
            }
        }
        else if (string.IsNullOrEmpty(e.PropertyName))
        {
            HandleSourceChanged();
        }
    }

    private void AttachPropertyChangedHandler(IEnumerable items)
    {
        if (!liveShapingEnabled || items is null)
        {
            return;
        }

        foreach (object item in items)
        {
            if (item is INotifyPropertyChanged notifyPropertyChanged)
            {
                notifyPropertyChanged.PropertyChanged += ItemOnPropertyChanged;
            }
        }
    }

    private void DetachPropertyChangedHandler(IEnumerable items)
    {
        if (!liveShapingEnabled || items is null)
        {
            return;
        }

        foreach (object item in items)
        {
            if (item is INotifyPropertyChanged notifyPropertyChanged)
            {
                notifyPropertyChanged.PropertyChanged -= ItemOnPropertyChanged;
            }
        }
    }

    private void HandleSortChanged()
    {
        view.Sort(this);
        OnVectorChanged(new VectorChangedEventArgs(CollectionChange.Reset));
    }

    private void HandleFilterChanged()
    {
        if (filter is not null)
        {
            for (int index = 0; index < view.Count; index++)
            {
                T item = view[index];
                if (filter(item))
                {
                    continue;
                }

                RemoveFromView(index, item);
                index--;
            }
        }

        HashSet<T> viewSet = new(view);
        int viewIndex = 0;
        for (int index = 0; index < source.Count; index++)
        {
            T item = source[index];
            if (viewSet.Contains(item))
            {
                viewIndex++;
                continue;
            }

            if (HandleSourceItemAdded(index, item, viewIndex))
            {
                viewIndex++;
            }
        }
    }

    private void HandleSourceChanged()
    {
        T? currentItem = CurrentItem;
        view.Clear();
        foreach (T item in Source)
        {
            if (filter is not null && !filter(item))
            {
                continue;
            }

            if (sortDescriptions.Count > 0)
            {
                int targetIndex = view.BinarySearch(item, this);
                if (targetIndex < 0)
                {
                    targetIndex = ~targetIndex;
                }

                view.Insert(targetIndex, item);
            }
            else
            {
                view.Add(item);
            }
        }

        OnVectorChanged(new VectorChangedEventArgs(CollectionChange.Reset));
        MoveCurrentTo(currentItem);
    }

    private void SourceNotifyCollectionChangedCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                ArgumentNullException.ThrowIfNull(e.NewItems);
                AttachPropertyChangedHandler(e.NewItems);
                if (deferCounter <= 0)
                {
                    if (e.NewItems?.Count == 1)
                    {
                        object? newItem = e.NewItems[0];
                        ArgumentNullException.ThrowIfNull(newItem);
                        HandleSourceItemAdded(e.NewStartingIndex, (T)newItem);
                    }
                    else
                    {
                        HandleSourceChanged();
                    }
                }

                break;
            case NotifyCollectionChangedAction.Remove:
                ArgumentNullException.ThrowIfNull(e.OldItems);
                DetachPropertyChangedHandler(e.OldItems);
                if (deferCounter <= 0)
                {
                    if (e.OldItems?.Count == 1)
                    {
                        object? oldItem = e.OldItems[0];
                        ArgumentNullException.ThrowIfNull(oldItem);
                        HandleSourceItemRemoved(e.OldStartingIndex, (T)oldItem);
                    }
                    else
                    {
                        HandleSourceChanged();
                    }
                }

                break;
            case NotifyCollectionChangedAction.Move:
            case NotifyCollectionChangedAction.Replace:
            case NotifyCollectionChangedAction.Reset:
                if (deferCounter <= 0)
                {
                    HandleSourceChanged();
                }

                break;
        }
    }

    private bool HandleSourceItemAdded(int newStartingIndex, T newItem, int? viewIndex = null)
    {
        if (filter is not null && !filter(newItem))
        {
            return false;
        }

        int newViewIndex = view.Count;

        if (sortDescriptions.Count > 0)
        {
            newViewIndex = view.BinarySearch(newItem, this);
            if (newViewIndex < 0)
            {
                newViewIndex = ~newViewIndex;
            }
        }
        else if (filter is not null)
        {
            if (source is null)
            {
                HandleSourceChanged();
                return false;
            }

            if (newStartingIndex == 0 || view.Count == 0)
            {
                newViewIndex = 0;
            }
            else if (newStartingIndex == source.Count - 1)
            {
                newViewIndex = view.Count;
            }
            else if (viewIndex.HasValue)
            {
                newViewIndex = viewIndex.Value;
            }
            else
            {
                for (int i = 0, j = 0; i < source.Count; i++)
                {
                    if (i == newStartingIndex)
                    {
                        newViewIndex = j;
                        break;
                    }

                    if (Equals(view[j], source[i]))
                    {
                        j++;
                    }
                }
            }
        }

        view.Insert(newViewIndex, newItem);
        if (newViewIndex <= CurrentPosition)
        {
            CurrentPosition++;
        }

        OnVectorChanged(new VectorChangedEventArgs(CollectionChange.ItemInserted, newViewIndex, newItem));
        return true;
    }

    private void HandleSourceItemRemoved(int oldStartingIndex, T oldItem)
    {
        if (filter is not null && !filter(oldItem))
        {
            return;
        }

        if (oldStartingIndex < 0 || oldStartingIndex >= view.Count || !Equals(view[oldStartingIndex], oldItem))
        {
            oldStartingIndex = view.IndexOf(oldItem);
        }

        if (oldStartingIndex < 0)
        {
            return;
        }

        RemoveFromView(oldStartingIndex, oldItem);
    }

    private void RemoveFromView(int itemIndex, T item)
    {
        view.RemoveAt(itemIndex);
        if (itemIndex <= CurrentPosition)
        {
            if (itemIndex == CurrentPosition--)
            {
                OnCurrentChanged();
            }
        }

        OnVectorChanged(new VectorChangedEventArgs(CollectionChange.ItemRemoved, itemIndex, item));
    }

    private void SortDescriptionsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (deferCounter > 0)
        {
            return;
        }

        HandleSortChanged();
    }

    private bool MoveCurrentToIndex(int i)
    {
        if (i == CurrentPosition)
        {
            return false;
        }

        if (i < -1 || i >= view.Count)
        {
            OnPropertyChanged(nameof(CurrentItem));
            return false;
        }

        CurrentChangingEventArgs e = new();
        OnCurrentChanging(e);
        if (e.Cancel)
        {
            return false;
        }

        CurrentPosition = i;
        OnCurrentChanged();
        return true;
    }

    private void OnCurrentChanging(CurrentChangingEventArgs e)
    {
        if (deferCounter > 0)
        {
            return;
        }

        CurrentChanging?.Invoke(this, e);
    }

    private void OnCurrentChanged()
    {
        if (deferCounter > 0)
        {
            return;
        }

        OnCurrentChangedOverride();
        CurrentChanged?.Invoke(this, default!);
        OnPropertyChanged(nameof(CurrentItem));
    }

    private void OnVectorChanged(IVectorChangedEventArgs e)
    {
        if (deferCounter > 0)
        {
            return;
        }

        VectorChanged?.Invoke(this, e);
        OnPropertyChanged(nameof(Count));
    }

    internal sealed class NotificationDeferrer : IDisposable
    {
        private readonly AdvancedCollectionView<T> advancedCollectionView;
        private readonly T? currentItem;

        public NotificationDeferrer(AdvancedCollectionView<T> acvs)
        {
            advancedCollectionView = acvs;
            currentItem = advancedCollectionView.CurrentItem;
            advancedCollectionView.deferCounter++;
        }

        public void Dispose()
        {
            advancedCollectionView.MoveCurrentTo(currentItem);
            advancedCollectionView.deferCounter--;
            advancedCollectionView.Refresh();
        }
    }
}