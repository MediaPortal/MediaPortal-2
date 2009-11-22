#region Copyright (C) 2007-2008 Team MediaPortal

/*
    Copyright (C) 2007-2008 Team MediaPortal
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
using MediaPortal.Core.PluginManager;
using MediaPortal.Core.Settings;
using MediaPortal.UI.Media.MediaManagement;
using MediaPortal.UI.Presentation.Players;

namespace Media.Players.BassPlayer
{
  public class BassPlayerPlugin : IPluginStateTracker, IPlayerBuilder
  {
    #region Fields

    private BassPlayer _BassPlayer;
    private BassPlayerSettings _BassPlayerSettings;
    private bool _Enabled = true;

    #endregion

    #region IPluginStateTracker Members

    void IPluginStateTracker.Activated(PluginRuntime pluginRuntime)
    {
      if (_BassPlayerSettings == null)
        _BassPlayerSettings = ServiceScope.Get<ISettingsManager>().Load<BassPlayerSettings>();

      if (_BassPlayer == null)
        _BassPlayer = BassPlayer.Create(this);
      
      _Enabled = true;
    }

    bool IPluginStateTracker.RequestEnd()
    {
      return true;
    }

    void IPluginStateTracker.Stop()
    {
      if (_BassPlayer != null)
        _BassPlayer.Stop();
      
      _Enabled = false;
    }

    void IPluginStateTracker.Continue()
    {
      _Enabled = true;
    }

    void IPluginStateTracker.Shutdown()
    {
      if (_BassPlayer != null)
      {
        ServiceScope.Get<ISettingsManager>().Save(_BassPlayerSettings);
        _BassPlayer.Dispose();
        _BassPlayer = null;
      }
    }

    #endregion

    #region IPlayerBuilder Members

    public bool CanPlay(IMediaItem mediaItem, Uri uri)
    {
      return _Enabled && IsAudioFile(mediaItem, uri.AbsolutePath);
    }

    public IPlayer GetPlayer(IMediaItem mediaItem, Uri uri)
    {
      if (_Enabled)
        return _BassPlayer;
      else
        return null;
    }

    #endregion

    #region Internal Members

    internal BassPlayerSettings Settings
    {
      get { return _BassPlayerSettings; }
    }
    
    #endregion
    
    #region Private Members

    bool IsAudioFile(IMediaItem mediaItem, string filename)
    {
      string ext = System.IO.Path.GetExtension(filename);

      // First check the Mime Type
      if (mediaItem.MetaData.ContainsKey("MimeType"))
      {
        string mimeType = mediaItem.MetaData["MimeType"] as string;
        if (mimeType != null)
        {
          if (mimeType.Contains("audio"))
          {
            if (_BassPlayerSettings.SupportedExtensions.IndexOf(ext) > -1)
              return true;
          }
        }
      }
      else if (_BassPlayerSettings.SupportedExtensions.IndexOf(ext) > -1)
        return true;
      
      return false;
    }

    #endregion

  }
}

