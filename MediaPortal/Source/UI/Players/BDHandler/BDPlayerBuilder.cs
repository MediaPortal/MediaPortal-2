#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
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
using System.Linq;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Settings;
using MediaPortal.Plugins.BDHandler.Settings.Configuration;
using MediaPortal.UI.Players.Video.Tools;
using MediaPortal.UI.Presentation.Players;
using MediaPortal.Plugins.BDHandler.Settings;

namespace MediaPortal.UI.Players.Video
{
  public class BDPlayerBuilder : IPlayerBuilder
  {
    #region Constructor

    public BDPlayerBuilder()
    {
      BDPlayerSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<BDPlayerSettings>();
      if (settings.BDSourceFilter == null)
      {
        // Try to init settings with the first available source filter
        CodecInfo sourceFilter = BDSourceFilterConfig.SupportedSourceFilters.FirstOrDefault(codecInfo => FilterGraphTools.IsThisComObjectInstalled(new Guid(codecInfo.CLSID)));
        if (sourceFilter != null)
        {
          settings.BDSourceFilter = sourceFilter;
          ServiceRegistration.Get<ISettingsManager>().Save(settings);
        }
      }

      Enabled = settings.BDSourceFilter != null;

      if (Enabled)
        LogInfo("Detected BluRay Source Filter '{0}' on the system.", settings.BDSourceFilter.Name);
      else
        LogWarn("No BluRay Source Filter was detected on the system.");
    }

    #endregion

    #region Protected fields

    private const string LOG_PREFIX = "BDHandler:";

    protected string _pluginDirectory = null;
    protected bool Enabled { get; set; }

    #endregion
    
    #region IPlayerBuilder implementation

    public IPlayer GetPlayer(MediaItem mediaItem)
    {
      string mimeType;
      string title;
      if (!mediaItem.GetPlayData(out mimeType, out title))
        return null;
      if (Enabled && mimeType == "video/bluray")
      {
        IResourceLocator locator = mediaItem.GetResourceLocator();
        BDPlayer player = new BDPlayer();
        try
        {
          player.SetMediaItem(locator, title);
        }
        catch (Exception ex)
        {
          ServiceRegistration.Get<ILogger>().Warn("Error playing media item '{0}'", ex, locator);
          player.Dispose();
          return null;
        }
        return player;
      }
      return null;
    }

    #endregion

    #region Logging

    private static string FormatPrefix(string format)
    {
      return string.Format("{0} {1}", LOG_PREFIX, format);
    }

    public static void LogInfo(string format, params object[] args)
    {
      ServiceRegistration.Get<ILogger>().Info(FormatPrefix(format), args);
    }

    public static void LogWarn(string format, params object[] args)
    {
      ServiceRegistration.Get<ILogger>().Warn(FormatPrefix(format), args);
    }

    public static void LogDebug(string format, params object[] args)
    {
      ServiceRegistration.Get<ILogger>().Debug(FormatPrefix(format), args);
    }

    public static void LogError(string format, params object[] args)
    {
      ServiceRegistration.Get<ILogger>().Error(FormatPrefix(format), args);
    }

    #endregion
  }
}
