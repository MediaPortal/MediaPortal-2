﻿#region Copyright (C) 2007-2018 Team MediaPortal

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

using MediaPortal.Common;
using MediaPortal.Common.Commands;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UiComponents.Media.Models;
using System;
using System.Collections.Generic;

namespace MediaPortal.UiComponents.Nereus.Models.HomeContent
{
  public class TVHomeContent : AbstractHomeContent
  {
    protected override void PopulateBackingList()
    {
      MediaListModel mlm = GetMediaListModel();

      _backingList.Add(new MediaShortcutListWrapper(new List<ListItem>
      {
        new LiveTVShortcut(),
        new EPGShortcut(),
        new SchedulesShortcut(),
        new RecordingsShortcut(),
        new TVSearchShortcut()
      }));

      _backingList.Add(new LastPlayTVList(mlm.Lists["LastPlayTV"].AllItems));
      _backingList.Add(new FavoriteTVList(mlm.Lists["FavoriteTV"].AllItems));
      _backingList.Add(new CurrentTVList(mlm.Lists["CurrentPrograms"].AllItems));
      _backingList.Add(new CurrentSchedulesList(mlm.Lists["CurrentSchedules"].AllItems));
    }
  }

  public class LastPlayTVList : ItemsListWrapper
  {
    public LastPlayTVList(ItemsList mediaList)
      : base(mediaList, "[Nereus.Home.LatestPlayed]")
    { }
  }

  public class FavoriteTVList : ItemsListWrapper
  {
    public FavoriteTVList(ItemsList mediaList)
      : base(mediaList, "[Nereus.Home.Favorites]")
    { }
  }

  public class CurrentTVList : ItemsListWrapper
  {
    public CurrentTVList(ItemsList mediaList)
      : base(mediaList, "[Nereus.Home.CurrentPrograms]")
    { }
  }

  public class CurrentSchedulesList : ItemsListWrapper
  {
    public CurrentSchedulesList(ItemsList mediaList)
      : base(mediaList, "[Nereus.Home.CurrentSchedules]")
    { }
  }

  public class LiveTVShortcut : WorkflowNavigationShortcutItem
  {
    public LiveTVShortcut() : base(new Guid("C7646667-5E63-48c7-A490-A58AC9518CFA")) { }
  }

  public class EPGShortcut : WorkflowNavigationShortcutItem
  {
    public EPGShortcut() : base(new Guid("7323BEB9-F7B0-48c8-80FF-8B59A4DB5385")) { }
  }

  public class SchedulesShortcut : WorkflowNavigationShortcutItem
  {
    public SchedulesShortcut() : base(new Guid("88842E97-2EF9-4658-AD35-8D74E3C689A4")) { }
  }

  public class RecordingsShortcut : WorkflowNavigationShortcutItem
  {
    public RecordingsShortcut() : base(new Guid("9D5B01A7-035F-46CF-8246-3C158C6CA960")) { }
  }

  public class TVSearchShortcut : SearchShortcutItem
  {
    public TVSearchShortcut()
    {
      Command = new MethodDelegateCommand(() =>
      {
        var wm = ServiceRegistration.Get<IWorkflowManager>();
        wm.NavigatePush(new Guid("CB5D4851-27D2-4222-B6A0-703EDC2071B5"));
      });
    }
  }
}
