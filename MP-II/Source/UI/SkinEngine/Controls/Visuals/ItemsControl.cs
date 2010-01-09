#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal II

    MediaPortal II is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal II is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using MediaPortal.Core.General;
using MediaPortal.UI.SkinEngine.Commands;
using MediaPortal.UI.SkinEngine.Controls.Visuals.Styles;
using MediaPortal.UI.SkinEngine.Controls.Panels;
using MediaPortal.UI.SkinEngine.Controls.Visuals.Templates;
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
    protected AbstractProperty _currentItemProperty;

    protected bool _prepare = false;
    protected bool _templateApplied = false;
    protected Panel _itemsHostPanel = null;

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
      _itemTemplateProperty = new SProperty(typeof(DataTemplate), null);
      _itemContainerStyleProperty = new SProperty(typeof(Style), null);
      _itemsPanelProperty = new SProperty(typeof(ItemsPanelTemplate), null);
      _currentItemProperty = new SProperty(typeof(object), null);
      _selectionChangedProperty = new SProperty(typeof(ICommandStencil), null);
    }

    void Attach()
    {
      _itemsSourceProperty.Attach(OnItemsSourceChanged);
      _itemTemplateProperty.Attach(OnItemTemplateChanged);
      _itemsPanelProperty.Attach(OnItemsPanelChanged);
      _itemContainerStyleProperty.Attach(OnItemContainerStyleChanged);
    }

    void Detach()
    {
      _itemsSourceProperty.Detach(OnItemsSourceChanged);
      _itemTemplateProperty.Detach(OnItemTemplateChanged);
      _itemsPanelProperty.Detach(OnItemsPanelChanged);
      _itemContainerStyleProperty.Detach(OnItemContainerStyleChanged);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      Detach();
      base.DeepCopy(source, copyManager);
      ItemsControl c = (ItemsControl) source;
      object oldItemsSource = ItemsSource;
      ItemsSource = copyManager.GetCopy(c.ItemsSource);
      ItemContainerStyle = copyManager.GetCopy(c.ItemContainerStyle);
      SelectionChanged = copyManager.GetCopy(c.SelectionChanged);
      _prepare = false;
      ItemTemplate = copyManager.GetCopy(c.ItemTemplate);
      ItemsPanel = copyManager.GetCopy(c.ItemsPanel);
      Attach();
      OnItemsSourceChanged(_itemsSourceProperty, oldItemsSource);
      InvalidateItems();
    }

    #endregion

    #region Event handlers

    protected void DetachFromItemsSource(IEnumerable itemsSource)
    {
      IObservable oldItemsSource = itemsSource as IObservable;
      if (oldItemsSource != null)
        oldItemsSource.ObjectChanged -= OnCollectionChanged;
    }

    protected void AttachToItemsSource(IEnumerable itemsSource)
    {
      IObservable coll = itemsSource as IObservable;
      if (coll != null)
        coll.ObjectChanged += OnCollectionChanged;
    }

    void OnItemsSourceChanged(AbstractProperty property, object oldValue)
    {
      DetachFromItemsSource(oldValue as IEnumerable);
      AttachToItemsSource(ItemsSource);
      InvalidateItems();
    }

    void OnCollectionChanged(IObservable itemsSource)
    {
      InvalidateItems();
    }

    void OnItemTemplateChanged(AbstractProperty property, object oldValue)
    {
      InvalidateItems();
    }

    void OnItemsPanelChanged(AbstractProperty property, object oldValue)
    {
      _templateApplied = false;
      InvalidateItems();
    }

    void OnItemContainerStyleChanged(AbstractProperty property, object oldValue)
    {
      InvalidateItems();
    }

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
    /// Gets or sets a collection used to generate the content of the ItemsControl.
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
    /// Gets or sets the DataTemplate used to display each item. For subclasses displaying
    /// hierarchical data, this should be a <see cref="HierarchicalDataTemplate"/>.
    /// </summary>
    public DataTemplate ItemTemplate
    {
      get { return (DataTemplate) _itemTemplateProperty.GetValue(); }
      set { _itemTemplateProperty.SetValue(value); }
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

    #endregion

    #region Measure&Arrange

    public override void Measure(ref SizeF totalSize)
    {
      // Call this before we measure. It will invalidate the layout (ApplyTemplate)
      DoUpdateItems();
      base.Measure(ref totalSize);
    }

    #endregion

    #region Item management

    /// <summary>
    /// Checks if the currently focused element is contained in this items control.
    /// </summary>
    bool CheckFocusInScope()
    {
      Visual focusPath = Screen == null ? null : Screen.FocusedElement;
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
      if (_itemsHostPanel == null || !CheckFocusInScope())
        CurrentItem = null;
      else
      {
        Visual element = Screen.FocusedElement;
        while (element != null && element.VisualParent != _itemsHostPanel)
          element = element.VisualParent;
        CurrentItem = element == null ? null : element.Context;
      }
      if (SelectionChanged != null)
        SelectionChanged.Execute(new object[] { CurrentItem });
    }

    protected void InvalidateItems()
    {
      _prepare = true;
      Invalidate();
      if (Screen != null) Screen.Invalidate(this);
    }

    protected ItemsPresenter FindItemsPresenter()
    {
      return TemplateControl == null ? null : TemplateControl.FindElement(
          new TypeFinder(typeof(ItemsPresenter))) as ItemsPresenter;
    }

    protected virtual bool Prepare()
    {
      if (ItemsSource == null) return false;
      if (ItemsPanel == null) return false;
      if (TemplateControl == null) return false;
      if (ItemContainerStyle == null) return false;
      if (ItemTemplate == null) return false;
      IList<object> l = new List<object>();
      // TODO: find a way to lock out other threads potentially accessing ItemsSource while we access it
      // (for example an interface which is implemented by ItemsList providing a member SyncRoot, which we can use
      // here to lock)
      CollectionUtils.AddAll(l, ItemsSource);
      IEnumerator enumer = l.GetEnumerator();
      ItemsPresenter presenter = FindItemsPresenter();
      if (presenter == null) return false;

      if (!_templateApplied)
      {
        presenter.ApplyTemplate(ItemsPanel);
        _itemsHostPanel = null;
        _templateApplied = true;
      }

      if (_itemsHostPanel == null)
        _itemsHostPanel = presenter.ItemsHostPanel;
      if (_itemsHostPanel == null) return false;

      _itemsHostPanel.Children.Clear();
      UIElementCollection children = new UIElementCollection(null);
      while (enumer.MoveNext())
      {
        UIElement container = PrepareItemContainer(enumer.Current);
        children.Add(container);
      }
      _itemsHostPanel.SetChildren(children);
      _itemsHostPanel.Invalidate();

      return true;
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
    protected abstract UIElement PrepareItemContainer(object dataItem);

    public void DoUpdateItems()
    {
      if (_prepare)
      {
        _prepare = false; // Set this first because another thread might InvalidateItems again while we are rebuilding
        if (!Prepare())
          // Didn't succeed yet. Try again next time.
          _prepare = true;
      }
    }

    public void SetFocusOnItem(object dataItem)
    {
      if (_itemsHostPanel == null || Screen == null)
        return;
      FrameworkElement item = null;
      foreach (UIElement child in _itemsHostPanel.Children)
        if (child.DataContext == dataItem)
          item = child as FrameworkElement;
      if (item == null)
        return;
      FrameworkElement focusable = ScreenManagement.Screen.FindFirstFocusableElement(item);
      if (focusable != null)
        focusable.HasFocus = true;
    }

    #endregion

    public override void Allocate()
    {
      base.Allocate();
      _prepare = true;
    }

    public override void Update()
    {
      base.Update();
      DoUpdateItems();
    }

    public override void  FireEvent(string eventName)
    {
      if (eventName == LOSTFOCUS_EVENT || eventName == GOTFOCUS_EVENT)
        UpdateCurrentItem();
 	    base.FireEvent(eventName);
    }

    public override void Dispose()
    {
      Detach();
      base.Dispose();
      DetachFromItemsSource(ItemsSource);
    }
  }
}
