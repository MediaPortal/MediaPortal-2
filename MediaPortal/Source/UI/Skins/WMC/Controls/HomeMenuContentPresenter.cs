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

using MediaPortal.UI.SkinEngine.Controls.Visuals;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UiComponents.WMCSkin.Models;

namespace MediaPortal.UiComponents.WMCSkin.Controls
{
  public class HomeMenuContentPresenter : AnimatedScrollContentPresenter
  {
    public override void BringIntoView(UIElement element, RectangleF elementBounds)
    {
      if (IsSelectedItem(element))
        base.BringIntoView(element, elementBounds);
    }

    protected bool IsSelectedItem(UIElement element)
    {
      var lvi = element.FindParentOfType<ListViewItem>();
      if (lvi != null)
      {
        var item = lvi.Context as ListItem;
        return item != null && item.Selected;
      }
      return false;
    }
  }
}
