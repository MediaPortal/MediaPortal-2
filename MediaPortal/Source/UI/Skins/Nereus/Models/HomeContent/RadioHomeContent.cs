#region Copyright (C) 2007-2021 Team MediaPortal

/*
    Copyright (C) 2007-2021 Team MediaPortal
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

using MediaPortal.Common;
using MediaPortal.Common.Commands;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Models;
using MediaPortal.UI.Presentation.Workflow;
using System;
using System.Collections.Generic;

namespace MediaPortal.UiComponents.Nereus.Models.HomeContent
{
  public class RadioHomeContent : AbstractHomeContent
  {
    public RadioHomeContent()
    {
      _availableMediaLists.Add(new LastPlayRadioList());
      _availableMediaLists.Add(new FavoriteRadioList());
      _availableMediaLists.Add(new CurrentRadioList());
      _availableMediaLists.Add(new CurrentSchedulesList());

      _shortcutLists.Add(new MediaShortcutListWrapper(new List<ListItem>
      {
        new LiveRadioShortcut(),
        new RadioEPGShortcut(),
        new RadioSchedulesShortcut(),
        new RadioRecordingsShortcut(),
        new RadioSearchShortcut()
      }));
    }

    protected override IContentListModel GetContentListModel()
    {
      return GetMediaListModel();
    }
  }

  public class LastPlayRadioList : MediaListItemsListWrapper
  {
    public LastPlayRadioList()
      : base("LastPlayRadio", "[Nereus.Home.LatestPlayed]")
    { }
  }

  public class FavoriteRadioList : MediaListItemsListWrapper
  {
    public FavoriteRadioList()
      : base("FavoriteRadio", "[Nereus.Home.Favorites]")
    { }
  }

  public class CurrentRadioList : MediaListItemsListWrapper
  {
    public CurrentRadioList()
      : base("CurrentRadioPrograms", "[Nereus.Home.CurrentPrograms]")
    { }
  }

  public class CurrentRadioSchedulesList : MediaListItemsListWrapper
  {
    public CurrentRadioSchedulesList()
      : base("CurrentSchedules", "[Nereus.Home.CurrentSchedules]")
    { }
  }

  public class LiveRadioShortcut : WorkflowNavigationShortcutItem
  {
    public LiveRadioShortcut() : base(new Guid("55F6CC8D-1D98-426F-8733-E6DF2861F706")) { }
  }

  public class RadioEPGShortcut : WorkflowNavigationShortcutItem
  {
    public RadioEPGShortcut() : base(new Guid("64AEE61A-7E45-450D-AA65-F4C109E3A7B3")) { }
  }

  public class RadioSchedulesShortcut : WorkflowNavigationShortcutItem
  {
    public RadioSchedulesShortcut() : base(new Guid("88842E97-2EF9-4658-AD35-8D74E3C689A4")) { }
  }

  public class RadioRecordingsShortcut : WorkflowNavigationShortcutItem
  {
    public RadioRecordingsShortcut() : base(new Guid("9D5B01A7-035F-46CF-8246-3C158C6CA960")) { }
  }

  public class RadioSearchShortcut : SearchShortcutItem
  {
    public RadioSearchShortcut()
    {
      Command = new MethodDelegateCommand(() =>
      {
        var wm = ServiceRegistration.Get<IWorkflowManager>();
        wm.NavigatePush(new Guid("CB5D4851-27D2-4222-B6A0-703EDC2071B5"));
      });
    }
  }
}
