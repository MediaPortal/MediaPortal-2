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
using MediaPortal.Common.PluginManager;
using MediaPortal.UiComponents.Media.Models;
using MediaPortal.UI.Services.UserManagement;

namespace MediaPortal.UiComponents.Media
{
  public class MediaPlugin : IPluginStateTracker
  {
    private UserMessageHandler _userMessageHandler;

    #region IPluginStateTracker implementation

    public void Activated(PluginRuntime pluginRuntime)
    {
      _userMessageHandler = new UserMessageHandler();
      _userMessageHandler.RequestRestrictions += RequestRestrictions;
    }

    public bool RequestEnd()
    {
      return true;
    }

    public void Stop()
    {
      _userMessageHandler.Dispose();
    }

    public void Continue() { }

    public void Shutdown() { }

    #endregion

    private void RequestRestrictions(object sender, EventArgs eventArgs)
    {
      // Register restrictions for MediaItemActions.
      MediaItemsActionModel model = new MediaItemsActionModel();
      model.BuildExtensions();
    }
  }
}
