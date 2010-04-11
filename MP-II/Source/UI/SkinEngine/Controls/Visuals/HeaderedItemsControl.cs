#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System.Collections;
using MediaPortal.Core.General;
using MediaPortal.UI.SkinEngine.Controls.Visuals.Templates;
using MediaPortal.UI.SkinEngine.MpfElements;
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
      IsExpanded = copyManager.GetCopy(c.IsExpanded);
      ForceExpander = copyManager.GetCopy(c.ForceExpander);
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

    protected override UIElement PrepareItemContainer(object dataItem)
    {
      TreeViewItem container = new TreeViewItem
        {
            Content = dataItem,
            Context = dataItem,
            Style = ItemContainerStyle,
            ForceExpander = ForceExpander,
            Screen = Screen
        };

      DataTemplate childItemTemplate = MpfCopyManager.DeepCopyCutLP(ItemTemplate);
      childItemTemplate.LogicalParent = container;
      container.ContentTemplate = childItemTemplate;

      // Re-use some properties for our children
      container.ItemContainerStyle = ItemContainerStyle;
      container.ItemsPanel = ItemsPanel;
      container.ItemTemplate = ItemTemplate;
      return container;
    }
  }
}
