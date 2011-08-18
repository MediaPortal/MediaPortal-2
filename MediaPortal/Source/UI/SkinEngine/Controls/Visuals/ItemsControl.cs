#region Copyright (C) 2007-2011 Team MediaPortal

/*
    Copyright (C) 2007-2011 Team MediaPortal
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
using System.Drawing;
using System.Linq;
using MediaPortal.Core.General;
using MediaPortal.UI.SkinEngine.Commands;
using MediaPortal.UI.SkinEngine.Controls.Visuals.Styles;
using MediaPortal.UI.SkinEngine.Controls.Panels;
using MediaPortal.UI.SkinEngine.Controls.Visuals.Templates;
using MediaPortal.UI.SkinEngine.MpfElements;
using MediaPortal.UI.SkinEngine.ScreenManagement;
using MediaPortal.UI.SkinEngine.Xaml;
using MediaPortal.UI.SkinEngine.Xaml.Interfaces;
using MediaPortal.Utilities;
using MediaPortal.Utilities.DeepCopy;

namespace MediaPortal.UI.SkinEngine.Controls.Visuals
{
  /// <summary>
  /// Represents a control that can be used to present a collection of items.
  /// http://msdn2.microsoft.com/en-us/library/system.windows.controls.itemscontrol.aspx
  /// </summary>
  public abstract class ItemsControl : Control
  {
    #region Protected fields

    protected AbstractProperty _selectionChangedProperty;
    protected AbstractProperty _itemsSourceProperty;
    protected AbstractProperty _itemTemplateProperty;
    protected AbstractProperty _itemContainerStyleProperty;
    protected AbstractProperty _itemsPanelProperty;
    protected AbstractProperty _dataStringProviderProperty;
    protected AbstractProperty _currentItemProperty;
    protected AbstractProperty _itemsProperty;
    protected AbstractProperty _isEmptyProperty;

    protected bool _preventItemsPreparation = false; // Prevent preparation before we are fully initialized - optimization
    protected bool _preparingItems = false; // Flag to prevent recursive call of PrepareItems method
    protected bool _prepareItems = false; // Flag to synchronize different threads; Tells render thread to update the items host panel
    protected ICollection<object> _preparedItems = null; // Items to be updated in items host panel
    protected bool _panelTemplateApplied = false; // Set to true as soon as the ItemsPanel style is applied on the items presenter
    protected Panel _itemsHostPanel = null; // Our instanciated items host panel
    protected FrameworkElement _lastFocusedElement = null; // Needed for focus tracking/update of current item
    protected ISelectableItemContainer _lastSelectedItem = null; // Needed for updating of the selected item

    #endregion

    #region Ctor

    protected ItemsControl()
    {
      Init();
      Attach();
    }

    void Init()
    {
      _itemsSourceProperty = new SProperty(typeof(IEnumerable), null);
      ItemCollection ic = new ItemCollection();
      _itemsProperty = new SProperty(typeof(ItemCollection), ic);
      _itemTemplateProperty = new SProperty(typeof(DataTemplate), null);
      _itemContainerStyleProperty = new SProperty(typeof(Style), null);
      _itemsPanelProperty = new SProperty(typeof(ItemsPanelTemplate), null);
      _dataStringProviderProperty = new SProperty(typeof(DataStringProvider), null);
      _currentItemProperty = new SProperty(typeof(object), null);
      _selectionChangedProperty = new SProperty(typeof(ICommandStencil), null);
      _isEmptyProperty = new SProperty(typeof(bool), false);
      AttachToItems(Items);
    }

    void Attach()
    {
      _itemsSourceProperty.Attach(OnItemsSourceChanged);
      _itemsProperty.Attach(OnItemsChanged);
      _itemTemplateProperty.Attach(OnItemTemplateChanged);
      _itemsPanelProperty.Attach(OnItemsPanelChanged);
      _dataStringProviderProperty.Attach(OnDataStringProviderChanged);
      _itemContainerStyleProperty.Attach(OnItemContainerStyleChanged);
    }

    void Detach()
    {
      _itemsSourceProperty.Detach(OnItemsSourceChanged);
      _itemsProperty.Detach(OnItemsChanged);
      _itemTemplateProperty.Detach(OnItemTemplateChanged);
      _itemsPanelProperty.Detach(OnItemsPanelChanged);
      _dataStringProviderProperty.Attach(OnDataStringProviderChanged);
      _itemContainerStyleProperty.Detach(OnItemContainerStyleChanged);
      DetachFromItems(Items);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      _preventItemsPreparation = true;
      Detach();
      base.DeepCopy(source, copyManager);
      ItemsControl c = (ItemsControl) source;
      object oldItemsSource = ItemsSource;
      object oldItems = Items;
      ItemsSource = copyManager.GetCopy(c.ItemsSource);
      ItemContainerStyle = copyManager.GetCopy(c.ItemContainerStyle);
      SelectionChanged = copyManager.GetCopy(c.SelectionChanged);
      ItemTemplate = copyManager.GetCopy(c.ItemTemplate);
      ItemsPanel = copyManager.GetCopy(c.ItemsPanel);
      DataStringProvider = copyManager.GetCopy(c.DataStringProvider);
      _lastSelectedItem = copyManager.GetCopy(c._lastSelectedItem);
      Attach();
      OnItemsSourceChanged(_itemsSourceProperty, oldItemsSource);
      OnItemsChanged(_itemsProperty, oldItems);
      _preventItemsPreparation = false;
      copyManager.CopyCompleted += manager => PrepareItems(); // During the deep copy process, our referenced objects are not initialized yet so defer preparation of items to the end of the copy process
    }

    public override void Dispose()
    {
      Detach();
      DetachFromItemsSource(ItemsSource);
      ItemCollection items = Items;
      if (items != null)
      {
        DetachFromItems(items);
        // Normally, the disposal of items will be done by our items host panel. But in the rare case that we didn't add
        // the Items to our host panel's Children yet, we need to clean up them manually.
        items.Dispose();
      }
      IEnumerable<object> preparedItems = _preparedItems;
      if (preparedItems != null)
      {
        // Normally, preparedItems is null here. But in the rare case that we couldn't use them yet,
        // so we need to clean up them manually.
        foreach (object preparedItem in preparedItems)
        {
          object o = preparedItem;
          TryDispose(ref o);
        }
      }
      Registration.TryCleanupAndDispose(ItemTemplate);
      Registration.TryCleanupAndDispose(ItemContainerStyle);
      Registration.TryCleanupAndDispose(ItemsPanel);
      Registration.TryCleanupAndDispose(SelectionChanged);
      base.Dispose();
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
      PrepareItems();
    }

    void OnItemsPanelChanged(AbstractProperty property, object oldValue)
    {
      _panelTemplateApplied = false;
      PrepareItems();
    }

    void OnDataStringProviderChanged(AbstractProperty property, object oldValue)
    {
      PrepareItems();
    }

    void OnItemContainerStyleChanged(AbstractProperty property, object oldValue)
    {
      PrepareItems();
    }

    /// <summary>
    /// Called when the <see cref="Items"/> property has changed.
    /// </summary>
    /// <remarks>
    /// This method is called in two cases.
    /// 1) if the ItemsSource changed and new items were built automatically.
    /// 2) if the <see cref="Items"/> property is changed manually.
    /// </remarks>
    /// <param name="prop">The <see cref="ItemsProperty"/> property.</param>
    /// <param name="oldVal">The old value of the property.</param>
    void OnItemsChanged(AbstractProperty prop, object oldVal)
    {
      ItemCollection oldItems = oldVal as ItemCollection;
      if (oldItems != null)
        DetachFromItems(oldItems);
      // Disposal of items not necessary because they are disposed by the items host panel
      ItemCollection items = Items;
      AttachToItems(items);
      OnItemsChanged();
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
      PrepareItems();
    }

    /// <summary>
    /// Will be called if the <see cref="Items"/> object or the <see cref="Items"/> collection were changed.
    /// </summary>
    protected virtual void OnItemsChanged() { }

    #endregion

    #region Events

    public AbstractProperty SelectionChangedProperty
    {
      get { return _selectionChangedProperty; }
    }

    public ICommandStencil SelectionChanged
    {
      get { return (ICommandStencil)_selectionChangedProperty.GetValue(); }
      set { _selectionChangedProperty.SetValue(value); }
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
      get { return (ItemsPanelTemplate) _itemsPanelProperty.GetValue(); }
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
      get { return (IEnumerable) _itemsSourceProperty.GetValue(); }
      set { _itemsSourceProperty.SetValue(value); }
    }

    public AbstractProperty ItemContainerStyleProperty
    {
      get { return _itemContainerStyleProperty; }
    }

    public AbstractProperty ItemsProperty
    {
      get { return _itemsProperty; }
    }

    /// <summary>
    /// Gets or sets the items of the ItemsControl directly.
    /// </summary>
    public ItemCollection Items
    {
      get { return (ItemCollection) _itemsProperty.GetValue(); }
      set { _itemsProperty.SetValue(value); }
    }

    /// <summary>
    /// Gets or sets the Style that is applied to the container element generated for each item.
    /// </summary>
    public Style ItemContainerStyle
    {
      get { return (Style) _itemContainerStyleProperty.GetValue(); }
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
      get { return (DataTemplate) _itemTemplateProperty.GetValue(); }
      set { _itemTemplateProperty.SetValue(value); }
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
      get { return (DataStringProvider) _dataStringProviderProperty.GetValue(); }
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
      get { return (bool) _isEmptyProperty.GetValue(); }
      set { _isEmptyProperty.SetValue(value); }
    }

    public bool IsItemsPrepared
    {
      get { return _itemsHostPanel != null; }
    }

    #endregion

    #region Item management

    public override void FireEvent(string eventName, RoutingStrategyEnum routingStrategy)
    {
      base.FireEvent(eventName, routingStrategy);
      if (eventName == LOSTFOCUS_EVENT || eventName == GOTFOCUS_EVENT)
        UpdateCurrentItem();
    }

    /// <summary>
    /// Checks if the currently focused element is contained in this items control.
    /// </summary>
    /// <param name="focusedElement">Currelty focused element.</param>
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
      }
      if (newCurrentItem != lastCurrentItem)
      {
        CurrentItem = newCurrentItem;
        FireSelectionChanged(newCurrentItem);
      }
    }

    protected void FireSelectionChanged(object newCurrentItem)
    {
      ICommandStencil commandStencil = SelectionChanged;
      if (commandStencil != null)
        commandStencil.Execute(new object[] { newCurrentItem });
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

    protected virtual void PrepareItems()
    {
      if (_preventItemsPreparation)
        return;
      _preparingItems = true; // Needed to suspend the change handler for the Items property
      try
      {
        // Check properties which are necessary in each case
        if (ItemsPanel == null) return;
        if (TemplateControl == null) return;
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

        SimplePropertyDataDescriptor itemsDataDescriptor;
        SimplePropertyDataDescriptor.CreateSimplePropertyDataDescriptor(this, "Items", out itemsDataDescriptor);

        IEnumerable itemsSource = ItemsSource;
        if (itemsSource == null)
        { // In this case, we must set up the items control using the Items property
          ItemCollection origItems = Items;
          if (origItems == null || origItems.IsReadOnly)
            // Reset read/write items after an ItemsSource had been used and then was reset to null.
            // Detach/Attach happens automatically by change handlers.
            SetValueInRenderThread(itemsDataDescriptor, origItems = new ItemCollection());
          IList<object> preparedItems = new List<object>(origItems.Count);
          foreach (object item in origItems)
          {
            // Hint: Since we use the original items from the Items collection, we must not change the collection.
            // The next time we call SetPreparedItems(), the old items are disposed and cannot be reused.
            FrameworkElement element = item as FrameworkElement ?? PrepareItemContainer(item);
            if (element.Style == null)
              element.Style = ItemContainerStyle;
            preparedItems.Add(element);
          }
          presenter.SetDataStrings(BuildDataStrings(origItems));
          SetPreparedItems(preparedItems);
        }
        else
        {
          // Check properties which are necessary to build items automatically
          if (ItemContainerStyle == null) return;
          // Albert: We can work without ItemTemplate - in that case, the ListViewItem/TreeViewItem (ContentControl) will automatically search the data template
          //if (ItemTemplate == null) return;
          IList<object> l = new List<object>();
          ISynchronizable sync = itemsSource as ISynchronizable;
          if (sync != null)
            lock (sync.SyncRoot)
              CollectionUtils.AddAll(l, itemsSource);
          else
            CollectionUtils.AddAll(l, itemsSource);

          presenter.SetDataStrings(BuildDataStrings(l));

          VirtualizingStackPanel vsp = _itemsHostPanel as VirtualizingStackPanel;
          if (vsp != null)
          {
            ListViewItemGenerator lvig = new ListViewItemGenerator();
            lvig.Initialize(this, l, ItemContainerStyle, ItemTemplate);
            IsEmpty = l.Count == 0;
            vsp.ItemProvider = lvig;
            SetValueInRenderThread(itemsDataDescriptor, null);
          }
          else
          {
            ItemCollection items = new ItemCollection();
            items.AddAll(l.Select(PrepareItemContainer));
            items.IsReadOnly = true;
            SetValueInRenderThread(itemsDataDescriptor, items);
            SetPreparedItems(items);
          }
        }
      }
      finally
      {
        _preparingItems = false;
      }
    }

    /// <summary>
    /// Called after the collection of items to be displayed has been set up.
    /// </summary>
    /// <param name="preparedItems">The prepared items.</param>
    protected void SetPreparedItems(ICollection<object> preparedItems)
    {
      ICollection<object> oldPreparedItems;
      lock (_renderLock)
      {
        oldPreparedItems = _preparedItems;
        _preparedItems = preparedItems;
        _prepareItems = true;
      }
      if (oldPreparedItems != null)
        // It seems that this method was called multiple times before _preparedItems could be
        // used by UpdatePreparedItems, so dispose old items
        foreach (object item in oldPreparedItems)
        {
          object o = item;
          TryDispose(ref o);
        }
      IsEmpty = preparedItems == null ? true : preparedItems.Count == 0;
      InvalidateLayout(true, true);
    }

    protected void UpdatePreparedItems()
    {
      ICollection<object> items;
      lock (_renderLock)
      {
        if (!_prepareItems)
          return;
        _prepareItems = false;
        items = _preparedItems;
        _preparedItems = null;
      }
      FrameworkElementCollection children = _itemsHostPanel.Children;
      lock (children.SyncRoot)
      {
        children.Clear();
        if (items == null)
          return;
        IList<FrameworkElement> tempItems = new List<FrameworkElement>(items.Count);
        foreach (object item in items)
        {
          ISelectableItemContainer sic = item as ISelectableItemContainer;
          if (sic != null && sic.Selected)
            _lastSelectedItem = sic;
          FrameworkElement fe = item as FrameworkElement;
          if (fe == null)
            continue;
          tempItems.Add(fe);
        }
        children.AddAll(tempItems);
      }
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
      PrepareItems();
    }
  }
}
