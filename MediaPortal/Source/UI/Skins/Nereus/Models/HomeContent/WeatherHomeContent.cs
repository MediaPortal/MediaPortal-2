#region Copyright (C) 2007-2020 Team MediaPortal

/*
    Copyright (C) 2007-2020 Team MediaPortal
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
using MediaPortal.UI.Presentation.Models;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UiComponents.Media.Models;

namespace MediaPortal.UiComponents.Nereus.Models.HomeContent
{
  public class WeatherHomeContent : AbstractHomeContent
  {
    protected override void PopulateBackingList()
    {
      _backingList.Add(new MediaShortcutListWrapper(new List<ListItem>
      {
        new LocationShortcut(),
        new SetupShortcut(),
        new RefreshShortcut(),
      }));
    }
  }

  public class LocationShortcut : WorkflowNavigationShortcutItem
  {
    public LocationShortcut() : base(new Guid("9A20A26F-2EF0-4a45-8F92-42D911AE1D8F")) { }
  }

  public class SetupShortcut : WorkflowNavigationShortcutItem
  {
    public SetupShortcut() : base(new Guid("F1CE62B4-32CA-46e8-BCFB-250FE07911B2")) { }
  }

  public class RefreshShortcut : WorkflowActionShortcutItem
  {
    public RefreshShortcut() : base(new Guid("92BDB53F-4159-4dc2-B212-6083C820A214"), "Refresh") { }
  }

}
