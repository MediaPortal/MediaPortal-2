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
using MediaPortal.Presentation.Players;
using MediaPortal.Media.MediaManagement;

namespace MediaPortal.SkinEngine.Players
{
  public class PlayerFactory : IPlayerBuilder
  {
    public PlayerFactory()
    {
    }

    public bool CanPlay(IMediaItem mediaItem, Uri uri)
    {
      string path = uri.AbsoluteUri.ToLower();
      if (path.IndexOf(".ts") >= 0)
      {
        return true;
      }
      if (path.IndexOf(".ifo") >= 0)
      {
        return true;
      }
      if (path.IndexOf(".wmv") >= 0 || path.IndexOf(".mpg") >= 0 || path.IndexOf(".avi") >= 0 || path.IndexOf(".mkv") >= 0)
      {
        return true;
      }
      if (mediaItem.MetaData.ContainsKey("MimeType"))
      {
        string mimeType = mediaItem.MetaData["MimeType"] as string;
        if (mimeType != null)
        {
          if (mimeType.Contains("video"))
          {
            return true;
          }
        }
      }
      return false;
    }

    public IPlayer GetPlayer(IMediaItem mediaItem, Uri uri)
    {
      string path = uri.AbsoluteUri.ToLower();
      if (path.IndexOf(".ts") >= 0)
      {
        return new TsVideoPlayer();
      }
      if (path.IndexOf(".ifo") >= 0)
      {
        return new DvdPlayer();
      }
      if (path.IndexOf(".wmv") >= 0 || path.IndexOf(".mpg") >= 0 || path.IndexOf(".avi") >= 0 || path.IndexOf(".mkv") >= 0)
      {
        return new VideoPlayer();
      }
      if (mediaItem.MetaData.ContainsKey("MimeType"))
      {
        string mimeType = mediaItem.MetaData["MimeType"] as string;
        if (mimeType != null)
        {
          if (mimeType.Contains("video") )
          {
            return new VideoPlayer();
          }
        }
      }
      return null;
    }

  }
}
