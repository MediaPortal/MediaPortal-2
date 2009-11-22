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
using MediaPortal.Core.PluginManager;
using MediaPortal.UI.Presentation.Players;

using MediaPortal.UI.Media.MediaManagement;


namespace Media.Players.PicturePlayer
{
  public class PicturePlayerPlugin : IPluginStateTracker, IPlayerBuilder
  {
    PicturePlayerSettings _settings;

    #region IPluginStateTracker implementation

    public void Activated(PluginRuntime pluginRuntime)
    {
      _settings = new PicturePlayerSettings();
    }

    public bool RequestEnd()
    {
      return false; // TODO: The player plugin should be able to be disabled
    }

    public void Stop() { }

    public void Continue() { }

    public void Shutdown() { }

    #endregion

    #region IDisposable Members

    public void Dispose()
    {
    }

    #endregion

    #region IPlayerBuilder Members

    /// <summary>
    /// Determines whether this instance can play the specified media item.
    /// </summary>
    /// <param name="mediaItem">The media item.</param>
    /// <param name="uri">The URI.</param>
    /// <returns>
    /// 	<c>true</c> if this instance can play the specified media item; otherwise, <c>false</c>.
    /// </returns>
    public bool CanPlay(IMediaItem mediaItem, Uri uri)
    {
      return IsImageFile(mediaItem, uri.AbsolutePath);
    }

    /// <summary>
    /// Gets the player.
    /// </summary>
    /// <param name="mediaItem">The media item.</param>
    /// <param name="uri">The URI.</param>
    /// <returns></returns>
    public IPlayer GetPlayer(IMediaItem mediaItem, Uri uri)
    {
      if (IsImageFile(mediaItem, uri.AbsolutePath))
      {
        return new PicturePlayer();
      }
      return null;
    }

    /// <summary>
    /// Determines whether the media item is an image
    /// </summary>
    /// <param name="mediaItem">The media item.</param>
    /// <param name="filename">The filename.</param>
    /// <returns>
    /// 	<c>true</c> if media item is an image; otherwise, <c>false</c>.
    /// </returns>
    bool IsImageFile(IMediaItem mediaItem, string filename)
    {
      string ext = System.IO.Path.GetExtension(filename);

      // First check the Mime Type
      if (mediaItem.MetaData.ContainsKey("MimeType"))
      {
        string mimeType = mediaItem.MetaData["MimeType"] as string;
        if (mimeType != null)
        {
          if (mimeType.Contains("image"))
          {
            if (_settings.SupportedExtensions.IndexOf(ext) > -1)
              return true;
          }
        }
      }

      if (_settings.SupportedExtensions.IndexOf(ext) > -1)
        return true;

      return false;
    }
    #endregion
  }
}
