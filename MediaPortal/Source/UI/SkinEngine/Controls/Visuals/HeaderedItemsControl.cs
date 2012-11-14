#region Copyright (C) 2007-2012 Team MediaPortal

/*
    Copyright (C) 2007-2012 Team MediaPortal
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
using MediaPortal.Common.General;
using MediaPortal.UI.SkinEngine.Controls.Visuals.Templates;
using MediaPortal.UI.SkinEngine.MpfElements;
using MediaPortal.UI.SkinEngine.Xaml;
using MediaPortal.Utilities.DeepCopy;

namespace MediaPortal.UI.SkinEngine.Controls.Visuals
{
  public class HeaderedItemsControl : ItemsControl, ISelectableItemContainer
  {
    #region Protected fields

    protected AbstractProperty _isExpandedProperty;
    protected AbstractProperty _isExpandableProperty;
    protected AbstractProperty _forceExpanderProperty;
    protected AbstractProperty _subItemsProviderProperty;
    protected AbstractProperty _selectedProperty;

    protected IDataDescriptor _attachedContextSource = null;
    protected bool _contextChangedAttached = false;

    #endregion

    #region Ctor

    public HeaderedItemsControl()
    {
      Init();
      Attach();
      CheckExpandable();
    }

    void Init()
    {
      _isExpandedProperty = new SProperty(typeof(bool), false);
      _isExpandableProperty = new SProperty(typeof(bool), false);
      _forceExpanderProperty = new SProperty(typeof(bool), false);
      _subItemsProviderProperty = new SProperty(typeof(SubItemsProvider), null);
      _selectedProperty = new SProperty(typeof(bool), false);
    }

    void Attach()
    {
      _forceExpanderProperty.Attach(OnForceExpanderChanged);
      _subItemsProviderProperty.Attach(OnSubItemsProviderChanged);
      _selectedProperty.Attach(OnSelectedChanged);
    }

    void Detach()
    {
      _forceExpanderProperty.Detach(OnForceExpanderChanged);
      _subItemsProviderProperty.Detach(OnSubItemsProviderChanged);
      _selectedProperty.Detach(OnSelectedChanged);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      Detach();
      base.DeepCopy(source, copyManager);
      HeaderedItemsControl c = (HeaderedItemsControl) source;
      IsExpanded = c.IsExpanded;
      ForceExpander = c.ForceExpander;
      SubItemsProvider = copyManager.GetCopy(c.SubItemsProvider);
      Selected = c.Selected;
      Attach();
      CheckExpandable();
    }

    #endregion

    void OnForceExpanderChanged(AbstractProperty prop, object oldVal)
    {
      CheckExpandable();
    }

    void OnSubItemsProviderChanged(AbstractProperty prop, object oldVal)
    {
      PrepareItems(true);
      InitializeSubItemsSource();
    }

    void OnSelectedChanged(AbstractProperty prop, object oldVal)
    {
      ItemsControl ic = FindParentOfType<ItemsControl>();
      if (ic != null)
        ic.UpdateSelectedItem(this);
    }

    protected override void OnItemsSourceChanged()
    {
      base.OnItemsSourceChanged();
      CheckExpandable();
    }

    protected override void OnItemsChanged()
    {
      base.OnItemsChanged();
      CheckExpandable();
    }

    protected void CheckExpandable()
    {
      // We must consider both the Items count and the ItemsSource count because
      // if ItemsSource is set, the TreeViewItem avoids the eager setup of the Items
      bool result = ForceExpander || _items.Count > 0;
      if (!result)
      {
        IEnumerable itemsSource = ItemsSource;
        if (itemsSource != null)
        {
          IEnumerator enumer = itemsSource.GetEnumerator();
          result = enumer.MoveNext();
        }
      }
      IsExpandable = result;
    }

    #region Public properties

    public bool IsExpanded
    {
      get { return (bool) _isExpandedProperty.GetValue(); }
      set { _isExpandedProperty.SetValue(value); }
    }

    public AbstractProperty IsExpandedProperty
    {
      get { return _isExpandedProperty; }
    }

    public bool IsExpandable
    {
      get { return (bool) _isExpandableProperty.GetValue(); }
      set { _isExpandableProperty.SetValue(value); }
    }

    public AbstractProperty IsExpandableProperty
    {
      get { return _isExpandableProperty; }
    }

    public bool ForceExpander
    {
      get { return (bool) _forceExpanderProperty.GetValue(); }
      set { _forceExpanderProperty.SetValue(value); }
    }

    public AbstractProperty ForceExpanderProperty
    {
      get { return _forceExpanderProperty; }
    }

    public AbstractProperty SubItemsProviderProperty
    {
      get { return _subItemsProviderProperty; }
    }

    /// <summary>
    /// Gets or sets the sub items provider which is used to build the sub items datasource for each tree view item.
    /// </summary>
    public SubItemsProvider SubItemsProvider
    {
      get { return (SubItemsProvider) _subItemsProviderProperty.GetValue(); }
      set { _subItemsProviderProperty.SetValue(value); }
    }

    public AbstractProperty SelectedProperty
    {
      get { return _selectedProperty; }
    }

    public bool Selected
    {
      get { return (bool) _selectedProperty.GetValue(); }
      set { _selectedProperty.SetValue(value); }
    }

    #endregion

    void OnDataContextValueChanged(IDataDescriptor dd)
    {
      InitializeSubItemsSource();
    }

    void OnContextChanged(object newContext, object oldContext)
    {
      InitializeSubItemsSource();
    }

    /// <summary>
    /// Initializes the <see cref="ItemsControl.ItemsSource"/> property with the <see cref="SubItemsProvider"/>.
    /// </summary>
    /// <returns><c>true</c>, if the <see cref="ItemsControl.ItemsSource"/> property was changed by this method, else <c>false</c>.</returns>
    protected virtual bool InitializeSubItemsSource()
    {
      SubItemsProvider sip = SubItemsProvider;
      IEnumerable oldItemsSource = ItemsSource;
      if (!_contextChangedAttached)
      {
        ContextChanged += OnContextChanged;
        _contextChangedAttached = true;
      }
      if (_attachedContextSource != null)
        _attachedContextSource.Detach(OnDataContextValueChanged);
      _attachedContextSource = DataContext.EvaluatedSourceValue;
      _attachedContextSource.Attach(OnDataContextValueChanged);
      object context = Context;
      if (context == null)
        return false;
      ItemsSource = sip == null ? null : sip.GetSubItems(context);
      if (oldItemsSource == ItemsSource)
        return false;
      MPF.TryCleanupAndDispose(oldItemsSource);
      CheckExpandable();
      return true;
    }

    protected override void PrepareItemsOverride(bool force)
    {
      SubItemsProvider sip = SubItemsProvider;
      if (sip == null)
        return;
      if (ItemsSource == null)
      {
        if (InitializeSubItemsSource()) // This could trigger a recursive call of PrepareItems(true) if the ItemsSource was changed, that's why we set _preventItemsPreparation above
          force = true;
      }
      base.PrepareItemsOverride(force);
    }

    protected override FrameworkElement PrepareItemContainer(object dataItem)
    {
// ReSharper disable UseObjectOrCollectionInitializer
      TreeViewItem container = new TreeViewItem
// ReSharper restore UseObjectOrCollectionInitializer
        {
            Content = dataItem,
            Context = dataItem,
            ForceExpander = ForceExpander,
            Screen = Screen,
            ElementState = _elementState,
            LogicalParent = this,
        };
      // Set this after the other properties have been initialized to avoid duplicate work
      container.Style = MpfCopyManager.DeepCopyCutLVPs(ItemContainerStyle);
      container.ContentTemplate = MpfCopyManager.DeepCopyCutLVPs(ItemTemplate);

      // Re-use some properties for our children
      container.ItemContainerStyle = MpfCopyManager.DeepCopyCutLVPs(ItemContainerStyle);
      container.ItemsPanel = MpfCopyManager.DeepCopyCutLVPs(ItemsPanel);
      container.ItemTemplate = MpfCopyManager.DeepCopyCutLVPs(ItemTemplate);
      container.SubItemsProvider = MpfCopyManager.DeepCopyCutLVPs(SubItemsProvider);
      return container;
    }
  }
}
