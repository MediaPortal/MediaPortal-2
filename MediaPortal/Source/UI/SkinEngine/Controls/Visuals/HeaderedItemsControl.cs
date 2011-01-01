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
using MediaPortal.Core.General;
using MediaPortal.UI.SkinEngine.Controls.Visuals.Templates;
using MediaPortal.UI.SkinEngine.MpfElements;
using MediaPortal.UI.SkinEngine.Xaml.Interfaces;
using MediaPortal.Utilities.DeepCopy;

namespace MediaPortal.UI.SkinEngine.Controls.Visuals
{
  public class HeaderedItemsControl : ItemsControl
  {
    #region Protected fields

    protected AbstractProperty _isExpandedProperty;
    protected AbstractProperty _isExpandableProperty;
    protected AbstractProperty _forceExpanderProperty;

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
    }

    void Attach()
    {
      _forceExpanderProperty.Attach(OnForceExpanderChanged);
    }

    void Detach()
    {
      _forceExpanderProperty.Attach(OnForceExpanderChanged);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      Detach();
      base.DeepCopy(source, copyManager);
      HeaderedItemsControl c = (HeaderedItemsControl) source;
      IsExpanded = c.IsExpanded;
      ForceExpander = c.ForceExpander;
      Attach();
      CheckExpandable();
    }

    #endregion

    void OnForceExpanderChanged(AbstractProperty prop, object oldVal)
    {
      CheckExpandable();
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
      bool result = ForceExpander || Items.Count > 0;
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

    #endregion

    protected override FrameworkElement PrepareItemContainer(object dataItem)
    {
// ReSharper disable UseObjectOrCollectionInitializer
      TreeViewItem container = new TreeViewItem
// ReSharper restore UseObjectOrCollectionInitializer
        {
            Content = dataItem,
            Context = dataItem,
            ForceExpander = ForceExpander,
            Screen = Screen
        };
      // Set this after the other properties have been initialized to avoid duplicate work
      container.Style = ItemContainerStyle;

      // We need to copy the item data template for the child containers, because the
      // data template contains specific data for each container. We need to "personalize" the
      // data template copy by assigning its LogicalParent.
      IEnumerable<IBinding> deferredBindings;
      DataTemplate childItemTemplate = MpfCopyManager.DeepCopyCutLP(ItemTemplate, out deferredBindings);
      childItemTemplate.LogicalParent = container;
      container.ContentTemplate = childItemTemplate;

      // Re-use some properties for our children
      container.ItemContainerStyle = ItemContainerStyle;
      container.ItemsPanel = ItemsPanel;
      container.ItemTemplate = ItemTemplate;
      // Bindings need to be activated because our ContentTemplate will provide the children's items source
      MpfCopyManager.ActivateBindings(deferredBindings);
      return container;
    }
  }
}
