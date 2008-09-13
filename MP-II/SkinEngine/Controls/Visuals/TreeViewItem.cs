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
using MediaPortal.SkinEngine.Controls.Visuals.Styles;
using MediaPortal.Utilities.DeepCopy;

namespace MediaPortal.SkinEngine.Controls.Visuals
{
  public class TreeViewItem : HeaderedItemsControl
  {
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
      base.DeepCopy(source, copyManager);
      Attach();
      OnItemTemplateChanged(null);
    }

    void OnItemTemplateChanged(Property property)
    {
      if (!(ItemTemplate is HierarchicalDataTemplate)) return;
      HierarchicalDataTemplate hdt = (HierarchicalDataTemplate) ItemTemplate;
      hdt.ItemsSourceProperty.Attach(OnTemplateItemsSourceChanged);
      ItemsSource = hdt.ItemsSource;
    }

    void OnTemplateItemsSourceChanged(Property property)
    {
      ItemsSource = (IEnumerable) property.GetValue();
    }

    protected override bool Prepare()
    {
      if (!IsExpanded)
        return true;
      return base.Prepare();
    }
  }
}
