#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal II

    MediaPortal II is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal II is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.IO;
using MediaPortal.Core;
using MediaPortal.Core.Logging;
using MediaPortal.Core.MediaManagement;
using MediaPortal.Core.PluginManager;
using MediaPortal.Core.Settings;
using MediaPortal.UI.Presentation.Players;

namespace Ui.Players.BassPlayer
{
  public class BassPlayerPlugin : IPluginStateTracker, IPlayerBuilder
  {
    #region Protected fields

    protected string _pluginDirectory = null;

    #endregion

    #region IPluginStateTracker implementation

    public void Activated(PluginRuntime pluginRuntime)
    {
      _pluginDirectory = pluginRuntime.Metadata.GetAbsolutePath(string.Empty);
    }

    public bool RequestEnd()
    {
      return true;
    }

    public void Stop() { }

    public void Continue() { }

    void IPluginStateTracker.Shutdown() { }

    #endregion

    #region IPlayerBuilder implementation

    public IPlayer GetPlayer(IResourceLocator locator, string mimeType)
    {
      IResourceAccessor accessor = locator.CreateAccessor();
      string ext = Path.GetExtension(accessor.ResourcePathName);

      // First check the Mime Type
      if (!string.IsNullOrEmpty(mimeType) && !mimeType.Contains("audio"))
        return null;
      BassPlayerSettings settings = ServiceScope.Get<ISettingsManager>().Load<BassPlayerSettings>();
      if (settings.SupportedExtensions.IndexOf(ext) > -1)
      {
        BassPlayer player = BassPlayer.Create(_pluginDirectory);
        try
        {
          player.SetMediaItemLocator(locator);
        }
        catch (Exception e)
        {
          ServiceScope.Get<ILogger>().Warn("BassPlayer: Error playing media item '{0}'", e, locator);
          player.Dispose();
          return null;
        }
        return player;
      }
      return null;
    }

    #endregion
  }
}
