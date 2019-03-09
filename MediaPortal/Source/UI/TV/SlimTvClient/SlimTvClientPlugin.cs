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

using MediaPortal.Common;
using MediaPortal.Common.PluginManager;
using MediaPortal.Plugins.SlimTv.Client.Helpers;
using MediaPortal.Plugins.SlimTv.Client.MediaExtensions;
using MediaPortal.Plugins.SlimTv.Client.Notifications;
using MediaPortal.Plugins.SlimTv.Client.TvHandler;
using MediaPortal.Plugins.SlimTv.Interfaces;

namespace MediaPortal.Plugins.SlimTv.Client
{
  public class SlimTvClientPlugin : IPluginStateTracker
  {
    #region IPluginStateTracker implementation

    public void Activated(PluginRuntime pluginRuntime)
    {
      ServiceRegistration.Set<ITvHandler>(new SlimTvHandler());
      ServiceRegistration.Set<ISlimTvNotificationService>(new SlimTvNotificationService());
      // Register recording section in MediaLibrary
      RecordingsLibrary.RegisterOnMediaLibrary();

      // Dummy call to static instance which creates required message handlers
      var channels = ChannelContext.Instance.Channels;
    }

    public bool RequestEnd()
    {
      return true;
    }

    public void Stop()
    {
      ServiceRegistration.RemoveAndDispose<ITvHandler>();
    }

    public void Continue() { }

    public void Shutdown() { }

    #endregion
  }
}
