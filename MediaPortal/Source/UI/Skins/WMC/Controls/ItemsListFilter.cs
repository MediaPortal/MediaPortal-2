using MediaPortal.Common.General;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UI.SkinEngine.Controls.Visuals;
using MediaPortal.UiComponents.SkinBase.General;
using MediaPortal.Utilities;
using MediaPortal.Utilities.DeepCopy;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace MediaPortal.UiComponents.WMCSkin.Controls
{
  public abstract class AbstractItemsListFilter : Control
  {
    #region Protected Fields

    protected AbstractProperty _itemsSourceProperty;
    protected AbstractProperty _filteredItemsProperty;
    protected AbstractProperty _additionalItemsProperty;

    #endregion

    #region Ctor

    protected AbstractItemsListFilter()
    {
      Init();
      Attach();
    }

    void Init()
    {
      _itemsSourceProperty = new SProperty(typeof(ItemsList), null);
      _filteredItemsProperty = new SProperty(typeof(ItemsList), new ItemsList());
      _additionalItemsProperty = new SProperty(typeof(ItemsList), new ItemsList());
    }

    void Attach()
    {
      _itemsSourceProperty.Attach(OnItemsSourceChanged);
    }

    void Detach()
    {
      _itemsSourceProperty.Detach(OnItemsSourceChanged);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      Detach();
      base.DeepCopy(source, copyManager);
      AbstractItemsListFilter c = (AbstractItemsListFilter)source;
      ItemsSource = c.ItemsSource;
      Attach();
      copyManager.CopyCompleted += manager => OnItemsSourceChanged();
    }

    #endregion

    #region Abstract Methods

    protected abstract bool ShouldIncludeItem(ListItem item);

    #endregion

    #region Event Handlers

    protected void DetachFromItemsSource(IEnumerable itemsSource)
    {
      IObservable coll = itemsSource as IObservable;
      if (coll != null)
        coll.ObjectChanged -= OnItemsSourceCollectionChanged;
    }

    protected void AttachToItemsSource(IEnumerable itemsSource)
    {
      IObservable coll = itemsSource as IObservable;
      if (coll != null)
        coll.ObjectChanged += OnItemsSourceCollectionChanged;
    }

    private void OnItemsSourceChanged(AbstractProperty property, object oldValue)
    {
      DetachFromItemsSource(oldValue as IEnumerable);
      AttachToItemsSource(ItemsSource);
      OnItemsSourceChanged();
    }

    private void OnItemsSourceCollectionChanged(IObservable observable)
    {
      OnItemsSourceChanged();
    }

    protected void OnItemsSourceChanged()
    {
      var filteredItems = FilteredItems;
      var additionalItems = AdditionalItems;

      filteredItems.Clear();
      additionalItems.Clear();
      ItemsList itemsSource = ItemsSource;
      if (itemsSource != null)
      {
        foreach (var item in itemsSource)
        {
          if (ShouldIncludeItem(item))
            filteredItems.Add(item);
          else
            additionalItems.Add(item);
        }
      }
      filteredItems.FireChange();
      additionalItems.FireChange();
    }

    #endregion

    #region Public Properties

    public AbstractProperty ItemsSourceProperty
    {
      get { return _itemsSourceProperty; }
    }

    public ItemsList ItemsSource
    {
      get { return (ItemsList)_itemsSourceProperty.GetValue(); }
      set { _itemsSourceProperty.SetValue(value); }
    }

    public AbstractProperty FilteredItemsProperty
    {
      get { return _filteredItemsProperty; }
    }

    public ItemsList FilteredItems
    {
      get { return (ItemsList)_filteredItemsProperty.GetValue(); }
    }

    public AbstractProperty AdditionalItemsProperty
    {
      get { return _additionalItemsProperty; }
    }

    public ItemsList AdditionalItems
    {
      get { return (ItemsList)_additionalItemsProperty.GetValue(); }
    }

    #endregion
  }

  public abstract class AbstractItemsListActionIdFilter : AbstractItemsListFilter
  {
    #region Protected Fields

    protected AbstractProperty _filterProperty;
    protected HashSet<Guid> _actionIds;

    #endregion

    #region Ctor

    protected AbstractItemsListActionIdFilter()
    {
      Init();
      Attach();
    }

    void Init()
    {
      _filterProperty = new SProperty(typeof(string), null);
      _actionIds = new HashSet<Guid>();
    }

    void Attach()
    {
      _filterProperty.Attach(OnFilterChanged);
    }

    void Detach()
    {
      _filterProperty.Detach(OnFilterChanged);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      Detach();
      base.DeepCopy(source, copyManager);
      var c = (AbstractItemsListActionIdFilter)source;
      Filter = c.Filter;
      Attach();
      InitFilter();
    }

    #endregion

    #region Event Handlers

    void OnFilterChanged(AbstractProperty property, object oldValue)
    {
      InitFilter();
      OnItemsSourceChanged();
    }

    #endregion

    #region Protected Methods

    protected void InitFilter()
    {
      _actionIds.Clear();
      var filter = Filter;
      if (string.IsNullOrEmpty(filter))
        return;
      CollectionUtils.AddAll(_actionIds, filter.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(s => new Guid(s)));
    }

    protected bool FilterContainsActionId(ListItem item)
    {
      if (item == null || _actionIds == null || _actionIds.Count == 0)
        return false;
      object action;
      if (!item.AdditionalProperties.TryGetValue(Consts.KEY_ITEM_ACTION, out action))
        return false;
      WorkflowAction wfAction = action as WorkflowAction;
      if (wfAction == null)
        return false;
      return _actionIds.Contains(wfAction.ActionId);
    }

    #endregion

    #region Public Properties

    public AbstractProperty FilterProperty
    {
      get { return _filterProperty; }
    }

    public string Filter
    {
      get { return (string)_filterProperty.GetValue(); }
      set { _filterProperty.SetValue(value); }
    }

    #endregion
  }

  public class IncludeItemsListActionIdFilter : AbstractItemsListActionIdFilter
  {
    protected override bool ShouldIncludeItem(ListItem item)
    {
      return FilterContainsActionId(item);
    }
  }

  public class ExcludeItemsListActionIdFilter : AbstractItemsListActionIdFilter
  {
    protected override bool ShouldIncludeItem(ListItem item)
    {
      return !FilterContainsActionId(item);
    }
  }
}
