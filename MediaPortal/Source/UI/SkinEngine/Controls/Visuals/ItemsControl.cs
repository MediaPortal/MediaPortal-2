#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
    http://www.team-mediaportal.com

    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MediaPortal.Common.General;
using MediaPortal.UI.SkinEngine.Controls.Visuals.Styles;
using MediaPortal.UI.SkinEngine.Controls.Panels;
using MediaPortal.UI.SkinEngine.Controls.Visuals.Templates;
using MediaPortal.UI.SkinEngine.MpfElements;
using MediaPortal.UI.SkinEngine.MpfElements.Resources;
using MediaPortal.UI.SkinEngine.ScreenManagement;
using MediaPortal.UI.SkinEngine.Xaml;
using MediaPortal.UI.SkinEngine.Xaml.Interfaces;
using MediaPortal.Utilities;
using MediaPortal.Utilities.DeepCopy;
using Size = SharpDX.Size2;
using SizeF = SharpDX.Size2F;
using PointF = SharpDX.Vector2;
using SharpDX;

namespace MediaPortal.UI.SkinEngine.Controls.Visuals
{
  /// <summary>
  /// Represents a control that can be used to present a collection of items.
  /// http://msdn2.microsoft.com/en-us/library/system.windows.controls.itemscontrol.aspx
  /// </summary>
  public abstract class ItemsControl : Control
  {
    #region Protected fields

    protected AbstractProperty _itemsSourceProperty;
    protected AbstractProperty _itemTemplateProperty;
    protected AbstractProperty _itemContainerStyleProperty;
    protected AbstractProperty _groupHeaderContainerStyleProperty;
    protected AbstractProperty _groupHeaderTemplateProperty;
    protected AbstractProperty _itemsPanelProperty;
    protected AbstractProperty _dataStringProviderProperty;
    protected AbstractProperty _currentItemProperty;
    protected AbstractProperty _itemsCountProperty;
    protected AbstractProperty _isEmptyProperty;
    protected AbstractProperty _groupingValueProviderProperty;
    protected AbstractProperty _restoreFocusProperty;

    protected ItemCollection _items;
    protected bool _preventItemsPreparation = false; // Prevent preparation before we are fully initialized - optimization
    protected bool _setItems = false; // Tells render thread to update the Items collection
    protected bool _setChildren = false; // Tells render thread to update the items host panel
    protected ItemCollection _preparedItems = null; // Items to be updated in items host panel
    protected ItemCollection _preparedChildren = null; // Child elements to be updated in items host panel
    protected bool _panelTemplateApplied = false; // Set to true as soon as the ItemsPanel style is applied on the items presenter
    protected Panel _itemsHostPanel = null; // Our instanciated items host panel

    protected FrameworkElement _restoreFocusElement = null;
    protected FrameworkElement _lastFocusedElement = null; // Needed for focus tracking/update of current item
    protected ISelectableItemContainer _lastSelectedItem = null; // Needed for updating of the selected item

    protected BindingWrapper _groupingBindingWrapper;

    #endregion

    #region Ctor

    //MP2-522 this static constructor ensures that all static fields (notably RoutedEvent registrations) are initialized before an instance of this class is created
    static ItemsControl()
    {
    }

    protected ItemsControl()
    {
      Init();
      Attach();
    }

    void Init()
    {
      _items = new ItemCollection();
      _itemsSourceProperty = new SProperty(typeof(IEnumerable), null);
      _itemTemplateProperty = new SProperty(typeof(DataTemplate), null);
      _itemContainerStyleProperty = new SProperty(typeof(Style), null);
      _groupHeaderContainerStyleProperty = new SProperty(typeof(Style), null);
      _groupHeaderTemplateProperty = new SProperty(typeof(DataTemplate), null);
      _groupingValueProviderProperty = new SProperty(typeof(IGroupingValueProvider), null);
      _itemsPanelProperty = new SProperty(typeof(ItemsPanelTemplate), null);
      _dataStringProviderProperty = new SProperty(typeof(DataStringProvider), null);
      _currentItemProperty = new SProperty(typeof(object), null);
      _itemsCountProperty = new SProperty(typeof(int), 0);
      _isEmptyProperty = new SProperty(typeof(bool), false);
      _restoreFocusProperty = new SProperty(typeof(bool), false);
    }

    void Attach()
    {
      _itemsSourceProperty.Attach(OnItemsSourceChanged);
      _itemTemplateProperty.Attach(OnItemTemplateChanged);
      _groupHeaderTemplateProperty.Attach(OnGroupHeaderTemplateChanged);
      _groupingValueProviderProperty.Attach(OnGroupingValueProviderChanged);
      _itemsPanelProperty.Attach(OnItemsPanelChanged);
      _dataStringProviderProperty.Attach(OnDataStringProviderChanged);
      _itemContainerStyleProperty.Attach(OnItemContainerStyleChanged);
      _groupHeaderContainerStyleProperty.Attach(OnGroupHeaderContainerStyleChanged);

      _templateControlProperty.Attach(OnTemplateControlChanged);
      AttachToItems(_items);
      AttachToItemsSource(ItemsSource);
    }

    void Detach()
    {
      _itemsSourceProperty.Detach(OnItemsSourceChanged);
      _itemTemplateProperty.Detach(OnItemTemplateChanged);
      _groupHeaderTemplateProperty.Detach(OnGroupHeaderTemplateChanged);
      _groupingValueProviderProperty.Detach(OnGroupingValueProviderChanged);
      _itemsPanelProperty.Detach(OnItemsPanelChanged);
      _dataStringProviderProperty.Detach(OnDataStringProviderChanged);
      _itemContainerStyleProperty.Detach(OnItemContainerStyleChanged);
      _groupHeaderContainerStyleProperty.Detach(OnGroupHeaderContainerStyleChanged);

      _templateControlProperty.Detach(OnTemplateControlChanged);
      DetachFromItems(_items);
      DetachFromItemsSource(ItemsSource);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      _preventItemsPreparation = true;
      Detach();
      base.DeepCopy(source, copyManager);
      ItemsControl c = (ItemsControl)source;
      ItemsSource = copyManager.GetCopy(c.ItemsSource);
      _items.Clear();
      foreach (object item in c.Items)
        _items.Add(copyManager.GetCopy(item));
      ItemContainerStyle = copyManager.GetCopy(c.ItemContainerStyle);
      ItemTemplate = copyManager.GetCopy(c.ItemTemplate);
      GroupHeaderContainerStyle = copyManager.GetCopy(c.GroupHeaderContainerStyle);
      GroupHeaderTemplate = copyManager.GetCopy(c.GroupHeaderTemplate);
      ItemsPanel = copyManager.GetCopy(c.ItemsPanel);
      DataStringProvider = copyManager.GetCopy(c.DataStringProvider);
      RestoreFocus = c.RestoreFocus;
      _lastSelectedItem = copyManager.GetCopy(c._lastSelectedItem);
      Attach();
      _preventItemsPreparation = false;
      copyManager.CopyCompleted += manager => PrepareItems(false); // During the deep copy process, our referenced objects are not initialized yet so defer preparation of items to the end of the copy process
    }

    public override void Dispose()
    {
      Detach();
      DetachFromItemsSource(ItemsSource);
      ItemCollection items = _items;
      if (items != null)
      {
        DetachFromItems(items);
        items.Dispose();
      }
      ItemCollection preparedItems = _preparedItems;
      if (preparedItems != null)
        preparedItems.Dispose();
      ItemCollection preparedChildren = _preparedChildren;
      if (preparedChildren != null)
        preparedChildren.Dispose();
      base.Dispose();
      MPF.TryCleanupAndDispose(ItemTemplate);
      MPF.TryCleanupAndDispose(GroupHeaderTemplate);
      MPF.TryCleanupAndDispose(GroupingBindingWrapper);
      MPF.TryCleanupAndDispose(ItemContainerStyle);
      MPF.TryCleanupAndDispose(GroupHeaderContainerStyle);
      MPF.TryCleanupAndDispose(ItemsPanel);
    }

    #endregion

    #region Event handlers

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

    protected void DetachFromItems(ItemCollection items)
    {
      if (items == null)
        return;
      items.CollectionChanged -= OnItemsCollectionChanged;
    }

    protected void AttachToItems(ItemCollection items)
    {
      if (items == null)
        return;
      items.CollectionChanged += OnItemsCollectionChanged;
    }

    void OnItemsSourceChanged(AbstractProperty property, object oldValue)
    {
      DetachFromItemsSource(oldValue as IEnumerable);
      AttachToItemsSource(ItemsSource);
      OnItemsSourceChanged();
    }

    void OnItemsSourceCollectionChanged(IObservable itemsSource)
    {
      OnItemsSourceChanged();
    }

    void OnItemTemplateChanged(AbstractProperty property, object oldValue)
    {
      PrepareItems(true);
    }

    void OnGroupHeaderTemplateChanged(AbstractProperty property, object oldValue)
    {
      PrepareItems(true);
    }

    void OnGroupingValueProviderChanged(AbstractProperty property, object oldValue)
    {
      PrepareItems(true);
    }

    void OnItemsPanelChanged(AbstractProperty property, object oldValue)
    {
      _panelTemplateApplied = false;
      PrepareItems(true);
    }

    void OnTemplateControlChanged(AbstractProperty property, object oldValue)
    {
      _panelTemplateApplied = false;
      PrepareItems(true);
    }

    void OnDataStringProviderChanged(AbstractProperty property, object oldValue)
    {
      PrepareItems(true);
    }

    void OnItemContainerStyleChanged(AbstractProperty property, object oldValue)
    {
      PrepareItems(true);
    }

    void OnGroupHeaderContainerStyleChanged(AbstractProperty property, object oldValue)
    {
      PrepareItems(true);
    }

    /// <summary>
    /// Called when the <see cref="Items"/> collection changed.
    /// </summary>
    /// <param name="collection">The <see cref="Items"/> collection.</param>
    void OnItemsCollectionChanged(ItemCollection collection)
    {
      OnItemsChanged();
    }

    /// <summary>
    /// Will be called when the <see cref="ItemsSource"/> object or the <see cref="ItemsSource"/> collection were changed.
    /// </summary>
    protected virtual void OnItemsSourceChanged()
    {
      PrepareItems(true);
    }

    /// <summary>
    /// Will be called if the <see cref="Items"/> collection was changed.
    /// </summary>
    protected virtual void OnItemsChanged()
    {
      SetCount();
      // Unlike WFP, we don't support the change of the Items collection directly, so no need to react to any
      // change of that collection. This method is only to be overridden.
    }

    protected void SetCount()
    {
      ItemsCount = ItemsSource != null ? ItemsSource.Cast<object>().Count() : _items != null ? _items.Count : 0;
    }

    #endregion

    #region Events

    public static readonly RoutedEvent SelectionChangedEvent = EventManager.RegisterRoutedEvent(
      "SelectionChanged", RoutingStrategy.Bubble, typeof(SelectionChangedEventHandler), typeof(ItemsControl));

    public event SelectionChangedEventHandler SelectionChanged
    {
      add { AddHandler(SelectionChangedEvent, value); }
      remove { RemoveHandler(SelectionChangedEvent, value); }
    }

    protected void InvokeSelectionChanged(object oldItem, object newItem)
    {
      InvokeSelectionChanged(
        oldItem != null ? new[] { oldItem } : null,
        newItem != null ? new[] { newItem } : null);
    }

    protected void InvokeSelectionChanged(IList oldItems, IList newItems)
    {
      oldItems = oldItems ?? new object[0];
      newItems = newItems ?? new object[0];
      var args = new SelectionChangedEventArgs(SelectionChangedEvent, oldItems, newItems);
      OnSelectionChanged(args);
    }


    /// <summary>
    /// Is called when ever the selection changes
    /// </summary>
    /// <param name="args">Selection changed event arguments.</param>
    /// <remarks>
    /// When overridden the selection event can be handled internally.
    /// If the base method is not called, the event will not be fired!
    /// </remarks>
    protected virtual void OnSelectionChanged(SelectionChangedEventArgs args)
    {
      RaiseEvent(args);
    }

    #endregion

    #region Public properties

    public AbstractProperty ItemsPanelProperty
    {
      get { return _itemsPanelProperty; }
    }

    /// <summary>
    /// Gets or sets the template that defines the panel that controls the layout of items.
    /// </summary>
    public ItemsPanelTemplate ItemsPanel
    {
      get { return (ItemsPanelTemplate)_itemsPanelProperty.GetValue(); }
      set { _itemsPanelProperty.SetValue(value); }
    }

    public AbstractProperty ItemsSourceProperty
    {
      get { return _itemsSourceProperty; }
    }

    /// <summary>
    /// Gets or sets an enumeration used to generate the content of the ItemsControl.
    /// </summary>
    public IEnumerable ItemsSource
    {
      get { return (IEnumerable)_itemsSourceProperty.GetValue(); }
      set { _itemsSourceProperty.SetValue(value); }
    }

    public AbstractProperty ItemContainerStyleProperty
    {
      get { return _itemContainerStyleProperty; }
    }

    /// <summary>
    /// Gets the items of the ItemsControl directly.
    /// </summary>
    public ItemCollection Items
    {
      get { return _items; }
    }

    /// <summary>
    /// Gets or sets the Style that is applied to the container element generated for each item.
    /// </summary>
    public Style ItemContainerStyle
    {
      get { return (Style)_itemContainerStyleProperty.GetValue(); }
      set { _itemContainerStyleProperty.SetValue(value); }
    }

    public AbstractProperty ItemTemplateProperty
    {
      get { return _itemTemplateProperty; }
    }

    /// <summary>
    /// Gets or sets the data template used to display each item.
    /// </summary>
    public DataTemplate ItemTemplate
    {
      get { return (DataTemplate)_itemTemplateProperty.GetValue(); }
      set { _itemTemplateProperty.SetValue(value); }
    }

    /// <summary>
    /// Gets or sets the Style that is applied to the container element generated for each group header.
    /// </summary>
    public Style GroupHeaderContainerStyle
    {
      get { return (Style)_groupHeaderContainerStyleProperty.GetValue(); }
      set { _groupHeaderContainerStyleProperty.SetValue(value); }
    }

    public AbstractProperty GroupHeaderContainerStyleProperty
    {
      get { return _groupHeaderContainerStyleProperty; }
    }

    public AbstractProperty GroupHeaderTemplateProperty
    {
      get { return _groupHeaderTemplateProperty; }
    }


    public AbstractProperty GroupingValueProviderProperty
    {
      get { return _groupingValueProviderProperty; }
    }

    /// <summary>
    /// Gets or sets the data template used to display each group header.
    /// </summary>
    public DataTemplate GroupHeaderTemplate
    {
      get { return (DataTemplate)_groupHeaderTemplateProperty.GetValue(); }
      set { _groupHeaderTemplateProperty.SetValue(value); }
    }

    /// <summary>
    /// Gets or sets the grouping value provider
    /// </summary>
    /// <remarks>
    /// This can alternatively be used instead of <see cref="GroupingBindingWrapper"/>
    /// </remarks>
    public IGroupingValueProvider GroupingValueProvider
    {
      get { return (IGroupingValueProvider)_groupingValueProviderProperty.GetValue(); }
      set { _groupingValueProviderProperty.SetValue(value); }
    }

    /// <summary>
    /// Gets or sets the bidning to get the grouping value for an item
    /// </summary>
    /// <remarks>
    /// This can alternatively be used instead of <see cref="GroupingValueProvider"/>
    /// </remarks>
    public BindingWrapper GroupingBindingWrapper
    {
      get { return _groupingBindingWrapper; }
      set { _groupingBindingWrapper = value; }
    }

    public AbstractProperty DataStringProviderProperty
    {
      get { return _dataStringProviderProperty; }
    }

    /// <summary>
    /// Gets or sets the data string provider which is used to build strings for each item to be able to
    /// focus items when the user types keys.
    /// </summary>
    public DataStringProvider DataStringProvider
    {
      get { return (DataStringProvider)_dataStringProviderProperty.GetValue(); }
      set { _dataStringProviderProperty.SetValue(value); }
    }

    public AbstractProperty CurrentItemProperty
    {
      get { return _currentItemProperty; }
    }

    public object CurrentItem
    {
      get { return _currentItemProperty.GetValue(); }
      internal set { _currentItemProperty.SetValue(value); }
    }

    public AbstractProperty IsEmptyProperty
    {
      get { return _isEmptyProperty; }
    }

    public bool IsEmpty
    {
      get { return (bool)_isEmptyProperty.GetValue(); }
      set { _isEmptyProperty.SetValue(value); }
    }

    public AbstractProperty ItemsCountProperty
    {
      get { return _itemsCountProperty; }
    }

    public int ItemsCount
    {
      get { return (int)_itemsCountProperty.GetValue(); }
      set { _itemsCountProperty.SetValue(value); }
    }

    public AbstractProperty RestoreFocusProperty
    {
      get { return _restoreFocusProperty; }
    }

    public bool RestoreFocus
    {
      get { return (bool)_restoreFocusProperty.GetValue(); }
      set { _restoreFocusProperty.SetValue(value); }
    }

    public bool IsItemsPrepared
    {
      get { return _itemsHostPanel != null; }
    }

    protected override void OnUpdateElementState()
    {
      base.OnUpdateElementState();
      if (_elementState == ElementState.Running || _elementState == ElementState.Preparing)
        PrepareItems(false);
    }

    #endregion

    #region Item management

    protected override void DoFireEvent(string eventName)
    {
      base.DoFireEvent(eventName);
      if (eventName == LOSTFOCUS_EVENT || eventName == GOTFOCUS_EVENT)
        UpdateCurrentItem();
    }

    public override void AddPotentialFocusableElements(RectangleF? startingRect, ICollection<FrameworkElement> elements)
    {
      if (RestoreFocus && _restoreFocusElement != null)
      {
        ICollection<FrameworkElement> potentialElements = new List<FrameworkElement>();
        base.AddPotentialFocusableElements(startingRect, potentialElements);
        if (potentialElements.Contains(_restoreFocusElement))
          elements.Add(_restoreFocusElement);
        else
          CollectionUtils.AddAll(elements, potentialElements);
      }
      else
        base.AddPotentialFocusableElements(startingRect, elements);
    }

    /// <summary>
    /// Checks if the currently focused element is contained in this items control.
    /// </summary>
    /// <param name="focusedElement">Currently focused element.</param>
    bool CheckFocusInScope(FrameworkElement focusedElement)
    {
      Visual focusPath = focusedElement;
      while (focusPath != null)
      {
        if (focusPath == this)
          // Focused control is located in our focus scope
          return true;
        if (focusPath is ItemsControl)
          // Focused control is located in another itemscontrol's focus scope
          return false;
        focusPath = focusPath.VisualParent;
      }
      return false;
    }

    /// <summary>
    /// Will update the <see cref="CurrentItem"/> property. This method will be called when the
    /// current item might have changed.
    /// </summary>
    protected void UpdateCurrentItem()
    {
      Screen screen = Screen;
      FrameworkElement focusedElement = screen == null ? null : screen.FocusedElement;
      if (_lastFocusedElement == focusedElement)
        return;
      _lastFocusedElement = focusedElement;
      object lastCurrentItem = CurrentItem;
      object newCurrentItem = null;
      if (_itemsHostPanel != null && CheckFocusInScope(focusedElement))
      {
        Visual element = focusedElement;
        while (element != null && element.VisualParent != _itemsHostPanel)
          element = element.VisualParent;
        newCurrentItem = element == null ? null : element.Context;
        ISelectableItemContainer container = element as ISelectableItemContainer;
        if (container != null)
          container.Selected = true; // Triggers an update of our _lastSelectedItem
        _restoreFocusElement = focusedElement;
      }
      if (newCurrentItem != lastCurrentItem)
      {
        CurrentItem = newCurrentItem;
        InvokeSelectionChanged(lastCurrentItem, newCurrentItem);
      }
    }

    public void UpdateSelectedItem(ISelectableItemContainer container)
    {
      bool isSelected = container != null && container.Selected;
      if (ReferenceEquals(_lastSelectedItem, container))
      { // Our selected container
        if (!isSelected)
          _lastSelectedItem = null;
        return;
      }
      // Not our selected container
      if (!isSelected)
        return;
      if (_lastSelectedItem != null)
        _lastSelectedItem.Selected = false;
      _lastSelectedItem = container;
    }

    protected ItemsPresenter FindItemsPresenter()
    {
      FrameworkElement templateControl = TemplateControl;
      return templateControl == null ? null : templateControl.FindElement(
          new TypeMatcher(typeof(ItemsPresenter))) as ItemsPresenter;
    }

    protected IList<string> BuildDataStrings(ICollection<object> objects)
    {
      DataStringProvider dataStringProvider = DataStringProvider;
      if (dataStringProvider == null)
        return null;
      IList<string> result = new List<string>(objects.Count);
      foreach (object o in objects)
        result.Add(dataStringProvider.GenerateDataString(o));
      return result;
    }

    protected void PrepareItems(bool force)
    {
      if (!PreparingOrRunning)
        return;
      if (_preventItemsPreparation)
        return;
      _preventItemsPreparation = true;
      try
      {
        PrepareItemsOverride(force);
      }
      finally
      {
        _preventItemsPreparation = false;
      }
    }

    protected virtual void PrepareItemsOverride(bool force)
    {
      if (_panelTemplateApplied && _itemsHostPanel != null && !force)
        return;
      // Check properties which are necessary in each case
      if (ItemsPanel == null)
        return;

      ItemsPresenter presenter = FindItemsPresenter();
      if (presenter == null)
        return;

      if (!_panelTemplateApplied)
      {
        _panelTemplateApplied = true;
        presenter.ApplyTemplate(ItemsPanel);
        _itemsHostPanel = null;
      }

      if (_itemsHostPanel == null)
        _itemsHostPanel = presenter.ItemsHostPanel;
      if (_itemsHostPanel == null)
        return;

      // Albert: We cannot exit the method if one of the styles is not set because the styles
      // might be found by the SkinEngine's automatic Style assignment (FrameworkElement.CopyDefaultStyle)
      //if (ItemContainerStyle == null || ItemTemplate == null)
      //  return;

      IEnumerable itemsSource = ItemsSource;
      if (itemsSource == null)
      { // In this case, we must set up the items control using the Items property
        ItemCollection items = _items;
        ItemCollection preparedChildren = new ItemCollection();
        bool setItems = false;
        if (items == null)
        {
          // Restore items from "ItemsSource mode" where they have been set to null
          items = new ItemCollection();
          setItems = true;
        }
        foreach (object item in items)
        {
          object itemCopy = MpfCopyManager.DeepCopyWithFixedObject(item, this); // Keep this object as LogicalParent
          FrameworkElement element = itemCopy as FrameworkElement ?? PrepareItemContainer(itemCopy);
          if (element.Style == null && element is ContentControl)
            element.Style = ItemContainerStyle;
          element.LogicalParent = this;
          preparedChildren.Add(element);
        }
        presenter.SetDataStrings(BuildDataStrings(items));

        SetPreparedItems(setItems, setItems ? items : null, true, preparedChildren);
      }
      else
      {
        IList<object> l = new List<object>();
        ISynchronizable sync = itemsSource as ISynchronizable;
        if (sync != null)
          lock (sync.SyncRoot)
              CollectionUtils.AddAll(l, itemsSource);
        else
          CollectionUtils.AddAll(l, itemsSource);

        presenter.SetDataStrings(BuildDataStrings(l));

        var vsp = _itemsHostPanel as IVirtualizingPanel;
        if (vsp != null)
        {
          // In this case, the VSP will generate its items by itself
          ListViewItemGenerator lvig = new ListViewItemGenerator();
          lvig.Initialize(this, l, ItemContainerStyle, ItemTemplate,
            GroupingValueProvider, GroupingBindingWrapper == null ? null :  GroupingBindingWrapper.Binding, GroupHeaderContainerStyle, GroupHeaderTemplate);
          SimplePropertyDataDescriptor dd;
          if (SimplePropertyDataDescriptor.CreateSimplePropertyDataDescriptor(this, "IsEmpty", out dd))
            SetValueInRenderThread(dd, l.Count == 0);
          vsp.SetItemProvider(lvig);

          SetPreparedItems(true, null, false, null);
        }
        else
        {
          ItemCollection preparedItems = new ItemCollection();
          preparedItems.AddAll(l.Select(PrepareItemContainer));

          SetPreparedItems(true, null, true, preparedItems);
        }
      }
    }

    /// <summary>
    /// Called after the collection of items to be displayed has been set up.
    /// </summary>
    /// <param name="setItems">If set to <c>true</c>, the <see cref="Items"/> will be set to the given <paramref name="preparedItems"/>,
    /// else, the <see cref="Items"/> will be left unchanged.</param>
    /// <param name="preparedItems">Elements to be put into the <see cref="Items"/> collection.</param>
    /// <param name="setChildren">If set to <c>true</c>, the children of our host panel will be set to the given
    /// <paramref name="preparedChildren"/>, else, they will be left unchanged.</param>
    /// <param name="preparedChildren">Elements to be put into the <see cref="Panel.Children"/> of our host panel.</param>
    protected void SetPreparedItems(bool setItems, ItemCollection preparedItems, bool setChildren, ItemCollection preparedChildren)
    {
      ItemCollection oldPreparedItems;
      ItemCollection oldPreparedChildren;
      lock (_renderLock)
      {
        oldPreparedItems = _preparedItems;
        oldPreparedChildren = _preparedChildren;
        _preparedItems = preparedItems;
        _preparedChildren = preparedChildren;
        if (_preparedItems != null)
          _preparedItems.SetItemIndexes();
        if (_preparedChildren != null)
          _preparedChildren.SetItemIndexes();
        _setItems = setItems;
        _setChildren = setChildren;
      }
      // If one of those oldXXX properties is set, this method was called multiple times before _preparedItems could be
      // used by UpdatePreparedItems, so dispose old items
      if (oldPreparedItems != null)
        oldPreparedItems.Dispose();
      if (oldPreparedChildren != null)
        oldPreparedChildren.Dispose();
      if (_elementState == ElementState.Preparing)
        // Shortcut in state Preparing - no render thread necessary here to do the UpdatePreparedItems work
        UpdatePreparedItems();
      InvalidateLayout(true, true);
      SetCount();
    }

    protected void UpdatePreparedItems()
    {
      bool doSetItems;
      bool doSetChildren;
      ItemCollection preparedItems;
      ItemCollection preparedChildren;
      lock (_renderLock)
      {
        if (!_setItems && !_setChildren)
          return;
        doSetItems = _setItems;
        _setItems = false;
        doSetChildren = _setChildren;
        _setChildren = false;
        preparedItems = _preparedItems;
        _preparedItems = null;
        preparedChildren = _preparedChildren;
        _preparedChildren = null;
      }
      Panel itemsHostPanel = _itemsHostPanel;
      if (doSetChildren)
      {
        FrameworkElementCollection children = itemsHostPanel.Children;
        lock (children.SyncRoot)
        {
          children.Clear(false);
          if (preparedChildren != null)
          {
            IList<FrameworkElement> tempItems = new List<FrameworkElement>(preparedChildren.Count);
            foreach (object item in preparedChildren.ExtractElements())
            {
              FrameworkElement fe = item as FrameworkElement;
              if (fe == null)
              {
                MPF.TryCleanupAndDispose(item);
                continue;
              }
              ISelectableItemContainer sic = item as ISelectableItemContainer;
              if (sic != null && sic.Selected)
                _lastSelectedItem = sic;
              tempItems.Add(fe);
            }
            children.AddAll(tempItems);
            preparedChildren.Dispose();
          }
        }
      }
      if (doSetItems)
      {
        _items.Clear();
        if (preparedItems != null)
        {
          _items.AddAll(preparedItems.ExtractElements());
          preparedItems.Dispose();
        }
      }
      var vsp = itemsHostPanel as IVirtualizingPanel;
      if (vsp == null)
        IsEmpty = itemsHostPanel.Children.Count == 0;
      // else IsEmpty has been updated by PrepareItemsOverride
    }

    /// <summary>
    /// Creates an UI element which displays one of the items of this <see cref="ItemsControl"/>.
    /// The specified <paramref name="dataItem"/> is one of the items from the <see cref="ItemsSource"/>
    /// collection.
    /// </summary>
    /// <remarks>
    /// The implementor should use the <see cref="ItemContainerStyle"/> as style for the new container,
    /// and it should use the <see cref="ItemTemplate"/> as data template to display the
    /// <paramref name="dataItem"/>.
    /// </remarks>
    /// <param name="dataItem">Item to build a visible container for.</param>
    /// <returns>UI element which renders the specified <paramref name="dataItem"/>.</returns>
    protected abstract FrameworkElement PrepareItemContainer(object dataItem);

    public void SetFocusOnItem(object dataItem)
    {
      if (_itemsHostPanel == null || Screen == null)
        return;
      FrameworkElement item = null;
      lock (_itemsHostPanel.Children.SyncRoot)
          foreach (FrameworkElement child in _itemsHostPanel.Children)
          if (child.DataContext == dataItem)
            item = child;
      if (item == null)
        return;
      FrameworkElement focusable = Screen.FindFirstFocusableElement(item);
      if (focusable != null)
        focusable.TrySetFocus(true);
    }

    #endregion

    protected override SizeF CalculateInnerDesiredSize(SizeF totalSize)
    {
      UpdatePreparedItems();
      return base.CalculateInnerDesiredSize(totalSize);
    }

    public override void StartInitialization(IParserContext context)
    {
      _preventItemsPreparation = true;
      base.StartInitialization(context);
    }

    public override void FinishInitialization(IParserContext context)
    {
      base.FinishInitialization(context);
      _preventItemsPreparation = false;
      PrepareItems(false);
    }
  }
}
