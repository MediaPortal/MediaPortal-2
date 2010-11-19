#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
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
using MediaPortal.Core;
using MediaPortal.Core.Logging;
using MediaPortal.Core.MediaManagement.ResourceAccess;
using MediaPortal.UI.Players.Video.Tools;
using MediaPortal.UI.Presentation.Players;

namespace MediaPortal.UI.Players.Video
{
  public class BDPlayerBuilder : IPlayerBuilder
  {
    #region Constructor

    public BDPlayerBuilder()
    {
      Enabled = FilterGraphTools.IsThisComObjectInstalled(new Guid(BDPlayer.MpcMpegSourceFilterInfo.CLSID));
      if (Enabled)
        LogInfo("Detected source filer '{0}' on the system.", BDPlayer.MpcMpegSourceFilterInfo.Name);
      else
        LogWarn("'{0}' was not detected on the system.", BDPlayer.MpcMpegSourceFilterInfo.Name);
    }

    #endregion

    #region Protected fields

    private const string LOG_PREFIX = "BDHandler:";

    protected string _pluginDirectory = null;
    protected bool Enabled { get; set; }

    #endregion

    #region IPlayerBuilder implementation

    public IPlayer GetPlayer(IResourceLocator locator, string mimeType)
    {
      if (Enabled && mimeType == "video/bluray")
      {
        BDPlayer player = new BDPlayer();
        try
        {
          player.SetMediaItemLocator(locator);
        }
        catch (Exception)
        {
          LogError("Error playing media item '{0}'", locator);
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