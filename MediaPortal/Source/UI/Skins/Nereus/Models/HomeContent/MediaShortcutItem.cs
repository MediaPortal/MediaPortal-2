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
using MediaPortal.UiComponents.Media.Models.NavigationModel;
using System;
using System.Reflection;

namespace MediaPortal.UiComponents.Nereus.Models.HomeContent
{
  public class MediaShortcutItem : ListItem
  {
  }

  public class WorkflowActionShortcutItem : MediaShortcutItem
  {
    public WorkflowActionShortcutItem(Guid modelId, string method)
    {
      Command = new MethodDelegateCommand(() =>
      {
        var wm = ServiceRegistration.Get<IWorkflowManager>();
        object model = wm.GetModel(modelId);
        if (model == null)
          return;

        MethodInfo mi = model.GetType().GetMethod(method);
        if (mi == null)
          return;
        
        mi.Invoke(model, new object[] { });
      });
    }
  }

  public class WorkflowNavigationShortcutItem : MediaShortcutItem
  {
    public WorkflowNavigationShortcutItem(Guid stateId)
    {
      Command = new MethodDelegateCommand(() => 
      {
        var wm = ServiceRegistration.Get<IWorkflowManager>();
        wm.NavigatePush(stateId);
      });
    }
  }

  public class MediaScreenShortcutItem : MediaShortcutItem
  {
    public MediaScreenShortcutItem() { }

    public MediaScreenShortcutItem(Guid mediaNavigationRootState, Type filterScreenType)
    {
      Command = new MethodDelegateCommand(() => NavigateToFilterScreen(mediaNavigationRootState, filterScreenType));
    }

    /// <summary>
    /// Navigates to the given media navigation state and shows the given screen.
    /// </summary>
    /// <param name="mediaNavigationRootState">The root media navigation state.</param>
    /// <param name="filterScreenType">The type of screen data to show.</param>
    public void NavigateToFilterScreen(Guid mediaNavigationRootState, Type filterScreenType)
    {
      MediaNavigationConfig config = new MediaNavigationConfig
      {
        DefaultScreenType = filterScreenType,
        AlwaysUseDefaultScreen = true
      };
      MediaNavigationModel.NavigateToRootState(mediaNavigationRootState, config);
    }
  }

  public class GenreShortcutItem : MediaScreenShortcutItem
  {
    public GenreShortcutItem() { }

    public GenreShortcutItem(Guid mediaNavigationRootState, Type filterScreenType)
      : base(mediaNavigationRootState, filterScreenType)
    { }
  }

  public class YearShortcutItem : MediaScreenShortcutItem
  {
    public YearShortcutItem() { }

    public YearShortcutItem(Guid mediaNavigationRootState, Type filterScreenType)
      : base(mediaNavigationRootState, filterScreenType)
    { }
  }

  public class AgeShortcutItem : MediaScreenShortcutItem
  {
    public AgeShortcutItem() { }

    public AgeShortcutItem(Guid mediaNavigationRootState, Type filterScreenType)
      : base(mediaNavigationRootState, filterScreenType)
    { }
  }

  public class ActorShortcutItem : MediaScreenShortcutItem
  {
    public ActorShortcutItem() { }

    public ActorShortcutItem(Guid mediaNavigationRootState, Type filterScreenType)
      : base(mediaNavigationRootState, filterScreenType)
    { }
  }

  public class ArtistShortcutItem : MediaScreenShortcutItem
  {
    public ArtistShortcutItem() { }

    public ArtistShortcutItem(Guid mediaNavigationRootState, Type filterScreenType)
      : base(mediaNavigationRootState, filterScreenType)
    { }
  }

  public class SearchShortcutItem : MediaScreenShortcutItem
  {
    public SearchShortcutItem() { }

    public SearchShortcutItem(Guid mediaNavigationRootState, Type filterScreenType)
      : base(mediaNavigationRootState, filterScreenType)
    { }
  }
}
