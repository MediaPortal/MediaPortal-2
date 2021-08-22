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
  public class NewsHomeContent : AbstractHomeContent
  {
    protected override void PopulateBackingList()
    {
      _backingList.Add(new MediaShortcutListWrapper(new List<ListItem>
      {
        new NewsSetupShortcut(),
        new NewsRefreshShortcut(),
      }));
    }
  }

  public class NewsSetupShortcut : WorkflowNavigationShortcutItem
  {
    public NewsSetupShortcut() : base(new Guid("66398F9B-A4DE-49F4-840C-4228C9C94F35")) { }
  }

  public class NewsRefreshShortcut : WorkflowActionShortcutItem
  {
    public NewsRefreshShortcut() : base(new Guid("EE1BBF83-AE5C-491C-9978-14737A2B0883"), "Refresh") { }
  }
}
