#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaPortal.Common;
using MediaPortal.Common.Commands;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UiComponents.Media.Models;

namespace MediaPortal.UiComponents.Nereus.Models.HomeContent
{
  public class WeatherHomeContent : AbstractHomeContent
  {
    protected override void PopulateBackingList()
    {
      MediaListModel mlm = GetMediaListModel();

      _backingList.Add(new MediaShortcutListWrapper(new List<ListItem>
      {
        new LocationShortcut(),
        new RefreshShortcut(),
      }));

    }
  }

  public class LocationShortcut : WorkflowNavigationShortcutItem
  {
    public LocationShortcut() : base(new Guid("9A20A26F-2EF0-4a45-8F92-42D911AE1D8F")) { }
  }

  public class RefreshShortcut : WorkflowNavigationShortcutItem
  {
    public RefreshShortcut() : base(new Guid("7AEB11DE-BA40-40a2-933A-B00BBD151B08")) { }
  }

}
