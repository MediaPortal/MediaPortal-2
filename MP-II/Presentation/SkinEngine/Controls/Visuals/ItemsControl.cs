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

using System.Diagnostics;
using System.Collections;
using MediaPortal.Presentation.Properties;
using Presentation.SkinEngine.Controls.Visuals.Styles;
using MediaPortal.Presentation.Collections;
using Presentation.SkinEngine.Controls.Panels;
using MediaPortal.Utilities.DeepCopy;
using Presentation.SkinEngine.MpfElements;

namespace Presentation.SkinEngine.Controls.Visuals
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

    protected ItemsCollection _attachedItemsCollection = null;

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
      ItemsControl c = source as ItemsControl;
      ItemsSource = copyManager.GetCopy(c.ItemsSource);
      ItemTemplateSelector = copyManager.GetCopy(c.ItemTemplateSelector);
      ItemContainerStyle = copyManager.GetCopy(c.ItemContainerStyle);
      ItemContainerStyleSelector = copyManager.GetCopy(c.ItemContainerStyleSelector);
      _prepare = false;
      ItemTemplate = copyManager.GetCopy(c.ItemTemplate);
      ItemsPanel = copyManager.GetCopy(c.ItemsPanel);
      Attach();
    }

    #endregion

    #region Event handlers

    void OnItemsSourceChanged(Property property)
    {
      if (_attachedItemsCollection != null)
      {
        _attachedItemsCollection.Changed -= OnCollectionChanged;
        _attachedItemsCollection = null;
      }
      ItemsCollection coll = ItemsSource as ItemsCollection;
      if (coll != null)
      {
        coll.Changed += OnCollectionChanged;
        _attachedItemsCollection = coll;
      }
      _prepare = true;
      Invalidate();
      if (Window!=null) Window.Invalidate(this);
    }

    void OnCollectionChanged(bool refreshAll)
    {
      _prepare = true;
      if (Window!=null) Window.Invalidate(this);
    }

    void OnHasFocusChanged(Property property)
    {
      if (HasFocus)
        SetFocusOnFirstItem();
    }

    void OnItemsSourcePropChanged(Property property)
    {
      _prepare = true;
      if (Window!=null) Window.Invalidate(this);
      Invalidate();
    }

    void OnItemTemplateChanged(Property property)
    {
      _prepare = true;
      if (Window!=null) Window.Invalidate(this);
      Invalidate();
    }

    void OnItemsPanelChanged(Property property)
    {
      _templateApplied = false;
      _prepare = true;
      if (Window!=null) Window.Invalidate(this);
      Invalidate();
    }

    void OnItemContainerStyleChanged(Property property)
    {
      _prepare = true;
      if (Window!=null) Window.Invalidate(this);
      Invalidate();
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
      get { return _itemsSourceProperty.GetValue() as IEnumerable; }
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
    /// Gets or sets the DataTemplate used to display each item.
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

    public override void Reset()
    {
      Trace.WriteLine("Reset:" + Name);
      base.Reset();
      _prepare = true;
      if (Window!=null) Window.Invalidate(this);
      if (_itemsHostPanel != null)
      {
        _itemsHostPanel.Reset();
        _itemsHostPanel.SetChildren(new UIElementCollection(_itemsHostPanel));
      }
    }

    #region Item generation

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
      if (enumer.MoveNext() == false) return true;
      enumer.Reset();
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
        FrameworkElement container = PrepareItemContainer(enumer.Current);
        children.Add(container);
      }
      children.SetParent(_itemsHostPanel);
      _itemsHostPanel.SetChildren(children);
      _itemsHostPanel.Invalidate();

      return true;
    }

    protected abstract FrameworkElement PrepareItemContainer(object dataItem);

    public override void UpdateLayout()
    {
      DoUpdateItems();
      base.UpdateLayout();
    }

    public bool DoUpdateItems()
    {
      if (_prepare)
        if (Prepare())
          _prepare = false;
      return false;
    }

    public override void DoRender()
    {
      DoUpdateItems();
      base.DoRender();
    }

    // FIXME Albert78: Remove this? Define the meaning of HasFocus property
    //public override bool HasFocus
    //{
    //  get
    //  {
    //    return (FindElement(FocusFinder.Instance) != null);
    //  }
    //  set
    //  {
    //    if (value)
    //    {
    //      if (!HasFocus)
    //        SetFocusOnFirstItem();
    //    }
    //    else
    //    {
    //      UIElement element = FindElement(FocusFinder.Instance);
    //      if (element != null)
    //        element.HasFocus = false;
    //    }
    //  }
    //}

    public void SetFocusOnFirstItem()
    {
      ItemsPresenter presenter = FindItemsPresenter();
      if (presenter != null)
      {
        Panel panel = presenter.FindElement(ItemsHostFinder.Instance) as Panel;
        if (panel != null)
        {
          if (panel.Children != null && panel.Children.Count > 0)
          {
            FrameworkElement element = (FrameworkElement)panel.Children[0];
            element.OnMouseMove(element.ActualPosition.X, element.ActualPosition.Y);
          }
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
