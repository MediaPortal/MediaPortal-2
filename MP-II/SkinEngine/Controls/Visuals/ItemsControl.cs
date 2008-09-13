#region Copyright (C) 2007-2008 Team MediaPortal

/*
    Copyright (C) 2007-2008 Team MediaPortal
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
using System.Drawing;
using MediaPortal.Core.General;
using MediaPortal.Presentation.DataObjects;
using MediaPortal.SkinEngine.Controls.Visuals.Styles;
using MediaPortal.SkinEngine.Controls.Panels;
using MediaPortal.SkinEngine.InputManagement;
using MediaPortal.Utilities.DeepCopy;

namespace MediaPortal.SkinEngine.Controls.Visuals
{
  /// <summary>
  /// Represents a control that can be used to present a collection of items.
  /// http://msdn2.microsoft.com/en-us/library/system.windows.controls.itemscontrol.aspx
  /// </summary>
  public abstract class ItemsControl : Control
  {
    #region Private fields

    Property _itemsSourceProperty;
    Property _itemTemplateProperty;
    Property _itemTemplateSelectorProperty;
    Property _itemContainerStyleProperty;
    Property _itemContainerStyleSelectorProperty;
    Property _itemsPanelProperty;
    Property _currentItem;
    bool _prepare;
    bool _templateApplied;
    protected Panel _itemsHostPanel;

    protected IObservable _attachedItemsCollection = null;

    #endregion

    #region Ctor

    public ItemsControl()
    {
      Init();
      Attach();
    }

    void Init()
    {
      _itemsSourceProperty = new Property(typeof(IEnumerable), null);
      _itemTemplateProperty = new Property(typeof(DataTemplate), null);
      _itemTemplateSelectorProperty = new Property(typeof(DataTemplateSelector), null);
      _itemContainerStyleProperty = new Property(typeof(Style), null);
      _itemContainerStyleSelectorProperty = new Property(typeof(StyleSelector), null);
      _itemsPanelProperty = new Property(typeof(ItemsPanelTemplate), null);
      _currentItem = new Property(typeof(object), null);
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
      ItemsSource = copyManager.GetCopy(c.ItemsSource);
      ItemTemplateSelector = copyManager.GetCopy(c.ItemTemplateSelector);
      ItemContainerStyle = copyManager.GetCopy(c.ItemContainerStyle);
      ItemContainerStyleSelector = copyManager.GetCopy(c.ItemContainerStyleSelector);
      _prepare = false;
      ItemTemplate = copyManager.GetCopy(c.ItemTemplate);
      ItemsPanel = copyManager.GetCopy(c.ItemsPanel);
      Attach();
      OnItemsSourceChanged(_itemsSourceProperty);
      InvalidateItems();
    }

    #endregion

    #region Event handlers

    void OnItemsSourceChanged(Property property)
    {
      if (_attachedItemsCollection != null)
      {
        _attachedItemsCollection.ObjectChanged -= OnCollectionChanged;
        _attachedItemsCollection = null;
      }
      IObservable coll = ItemsSource as IObservable;
      if (coll != null)
      {
        coll.ObjectChanged += OnCollectionChanged;
        _attachedItemsCollection = coll;
      }
      InvalidateItems();
    }

    void OnCollectionChanged(IObservable itemsSource)
    {
      InvalidateItems();
    }

    void OnItemTemplateChanged(Property property)
    {
      InvalidateItems();
    }

    void OnItemsPanelChanged(Property property)
    {
      _templateApplied = false;
      InvalidateItems();
    }

    void OnItemContainerStyleChanged(Property property)
    {
      InvalidateItems();
    }

    #endregion

    #region Public properties

    public Property ItemsPanelProperty
    {
      get { return _itemsPanelProperty; }
    }

    /// <summary>
    /// Gets or sets the template that defines the panel that controls the layout of items.
    /// </summary>
    public ItemsPanelTemplate ItemsPanel
    {
      get { return _itemsPanelProperty.GetValue() as ItemsPanelTemplate; }
      set { _itemsPanelProperty.SetValue(value); }
    }

    public Property ItemsSourceProperty
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

    public Property ItemContainerStyleProperty
    {
      get { return _itemContainerStyleProperty; }
    }

    /// <summary>
    /// Gets or sets the Style that is applied to the container element generated for each item.
    /// </summary>
    public Style ItemContainerStyle
    {
      get { return _itemContainerStyleProperty.GetValue() as Style; }
      set { _itemContainerStyleProperty.SetValue(value); }
    }

    public Property ItemContainerStyleSelectorProperty
    {
      get { return _itemContainerStyleSelectorProperty; }
    }

    /// <summary>
    /// Gets or sets custom style-selection logic for a style that can be applied to each generated container element.
    /// </summary>
    public StyleSelector ItemContainerStyleSelector
    {
      get { return _itemContainerStyleSelectorProperty.GetValue() as StyleSelector; }
      set { _itemContainerStyleSelectorProperty.SetValue(value); }
    }

    public Property ItemTemplateProperty
    {
      get { return _itemTemplateProperty; }
    }

    /// <summary>
    /// Gets or sets the DataTemplate used to display each item. For subclasses displaying
    /// hierarchical data, this should be a <see cref="HierarchicalDataTemplate"/>.
    /// </summary>
    public DataTemplate ItemTemplate
    {
      get { return _itemTemplateProperty.GetValue() as DataTemplate; }
      set { _itemTemplateProperty.SetValue(value); }
    }

    public Property ItemTemplateSelectorProperty
    {
      get { return _itemTemplateSelectorProperty; }
    }

    /// <summary>
    /// Gets or sets the custom logic for choosing a template used to display each item.
    /// </summary>
    public DataTemplateSelector ItemTemplateSelector
    {
      get { return _itemTemplateSelectorProperty.GetValue() as DataTemplateSelector; }
      set { _itemTemplateSelectorProperty.SetValue(value); }
    }

    public Property CurrentItemProperty
    {
      get { return _currentItem; }
    }

    public object CurrentItem
    {
      get { return _currentItem.GetValue(); }
      set { _currentItem.SetValue(value); }
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
      IEnumerator enumer = ItemsSource.GetEnumerator();
      ItemsPresenter presenter = FindItemsPresenter();
      if (presenter == null) return false;

      if (!_templateApplied)
      {
        presenter.ApplyTemplate(ItemsPanel);
        _itemsHostPanel = null;
        _templateApplied = true;
      }

      if (_itemsHostPanel == null)
        _itemsHostPanel = presenter.TemplateControl.FindElement(ItemsHostFinder.Instance) as Panel;
      if (_itemsHostPanel == null) return false;

      _itemsHostPanel.Children.Clear();
      UIElementCollection children = new UIElementCollection(null);
      while (enumer.MoveNext())
      {
        UIElement container = PrepareItemContainer(enumer.Current);
        children.Add(container);
      }
      children.SetParent(_itemsHostPanel);
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


    public bool DoUpdateItems()
    {
      if (_prepare)
        if (Prepare())
          _prepare = false;
      return false;
    }

    public void SetFocusOnFirstItem()
    {
      ItemsPresenter presenter = FindItemsPresenter();
      if (presenter != null)
      {
        Panel panel = presenter.FindElement(ItemsHostFinder.Instance) as Panel;
        if (panel != null)
        {
          FrameworkElement focusable = FocusManager.FindFirstFocusableElement(panel);
          if (focusable != null)
            focusable.HasFocus = true;
        }
      }
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
  }
}
