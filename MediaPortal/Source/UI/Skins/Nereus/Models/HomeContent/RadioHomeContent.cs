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

using MediaPortal.Common;
using MediaPortal.Common.Commands;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UiComponents.Media.Models;
using System;
using System.Collections.Generic;
using MediaPortal.UI.Presentation.Models;

namespace MediaPortal.UiComponents.Nereus.Models.HomeContent
{
  public class RadioHomeContent : AbstractHomeContent
  {
    public RadioHomeContent()
    {
      _availableLists.Add(new LastPlayRadioList());
      _availableLists.Add(new FavoriteRadioList());
      _availableLists.Add(new CurrentRadioList());
      _availableLists.Add(new CurrentRadioSchedulesList());
    }

    protected override void PopulateBackingList()
    {
      _backingList.Add(new MediaShortcutListWrapper(new List<ListItem>
      {
        new LiveRadioShortcut(),
        new RadioEPGShortcut(),
        new RadioSchedulesShortcut(),
        new RadioRecordingsShortcut(),
        new RadioSearchShortcut()
      }));

      UpdateListsFromAvailableLists();
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
      : base("CurrentRadioSchedules", "[Nereus.Home.CurrentSchedules]")
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
    public RadioSchedulesShortcut() : base(new Guid("9C095D73-D65D-42E6-9997-EF328C33F7F8")) { }
  }

  public class RadioRecordingsShortcut : WorkflowNavigationShortcutItem
  {
    public RadioRecordingsShortcut() : base(new Guid("714970E9-A7EB-4F9C-B372-1E60E3671A8F")) { }
  }

  public class RadioSearchShortcut : SearchShortcutItem
  {
    public RadioSearchShortcut()
    {
      Command = new MethodDelegateCommand(() =>
      {
        var wm = ServiceRegistration.Get<IWorkflowManager>();
        wm.NavigatePush(new Guid("F6B76F5F-1E37-4C4D-BB32-79AFB7A67951"));
      });
    }
  }
}
