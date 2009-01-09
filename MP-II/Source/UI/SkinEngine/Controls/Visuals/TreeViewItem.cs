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
using MediaPortal.Presentation.DataObjects;
using MediaPortal.SkinEngine.Controls.Visuals;
using MediaPortal.SkinEngine.Controls.Visuals.Templates;
using MediaPortal.Utilities.DeepCopy;

namespace MediaPortal.SkinEngine.Controls.Visuals
{
  public class TreeViewItem : HeaderedItemsControl, ISearchableItem
  {
    #region Protected fields

    protected Property _dataStringProperty = new Property(typeof(string), "");

    #endregion

    #region Ctor

    public TreeViewItem()
    {
      Attach();
    }

    void Attach()
    {
      ItemTemplateProperty.Attach(OnItemTemplateChanged);
    }

    void Detach()
    {
      ItemTemplateProperty.Detach(OnItemTemplateChanged);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      Detach();
      object oldItemTemplate = ItemTemplate;
      base.DeepCopy(source, copyManager);
      DataString = copyManager.GetCopy(DataString);
      Attach();
      OnItemTemplateChanged(_itemTemplateProperty, oldItemTemplate);
    }

    #endregion

    void OnItemTemplateChanged(Property property, object oldValue)
    {
      if (oldValue is HierarchicalDataTemplate)
        ((HierarchicalDataTemplate) oldValue).ItemsSourceProperty.Detach(OnTemplateItemsSourceChanged);
      if (!(ItemTemplate is HierarchicalDataTemplate)) return;
      HierarchicalDataTemplate hdt = (HierarchicalDataTemplate) ItemTemplate;
      hdt.ItemsSourceProperty.Attach(OnTemplateItemsSourceChanged);
      ItemsSource = hdt.ItemsSource;
      hdt.DataStringProperty.Attach(OnTemplateDataStringChanged);
      DataString = hdt.DataString;
    }

    void OnTemplateItemsSourceChanged(Property property, object oldValue)
    {
      ItemsSource = (IEnumerable) property.GetValue();
    }

    void OnTemplateDataStringChanged(Property property, object oldValue)
    {
      DataString = (string) property.GetValue();
    }

    #region Public properties

    public Property DataStringProperty
    {
      get { return _dataStringProperty; }
    }

    /// <summary>
    /// Returns a string representation for the current <see cref="TreeViewItem"/>. This is used
    /// by the scrolling engine to find the appropriate element when the user starts to type the first
    /// letters to move the focus to a child entry.
    /// </summary>
    /// <remarks>
    /// This value be automatically bound to the <see cref="HierarchicalDataTemplate.DataString"/> property.
    /// </remarks>
    public string DataString
    {
      get { return (string) _dataStringProperty.GetValue(); }
      set { _dataStringProperty.SetValue(value); }
    }

    #endregion

    protected override bool Prepare()
    {
      if (!IsExpanded)
        return true;
      return base.Prepare();
    }
  }
}
