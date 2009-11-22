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
using System.Drawing;
using MediaPortal.Core;
using MediaPortal.UI.Presentation.Players;
using MediaPortal.Core.Messaging;

using MediaPortal.UI.Media.MediaManagement;
using MediaPortal.UI.Presentation.Screens;

namespace Media.Players.PicturePlayer
{
  public class PicturePlayer : IPlayer
  {
    public const string PICTUREVIEWERQUEUE_NAME = "PictureViewer";

    IMediaItem _mediaItem;
    PlaybackState _state;

    /// <summary>
    /// Plays the file
    /// </summary>
    /// <param name="item"></param>
    public void Play(IMediaItem item)
    {
      //here we simply show the picture viewer window
      //from the my pictures window
      //and tell it to show the image
      _state = PlaybackState.Playing;
      _mediaItem = item;
      ServiceScope.Get<IScreenManager>().PrepareScreen("pictureviewer");
      QueueMessage msg = new QueueMessage();
      msg.MessageData["action"] = "show";
      msg.MessageData["mediaitem"] = item;
      ServiceScope.Get<IMessageBroker>().Send(PICTUREVIEWERQUEUE_NAME, msg);
      ServiceScope.Get<IScreenManager>().ShowScreen("pictureviewer");
    }

    #region IPlayer Members

    /// <summary>
    /// stops playback
    /// </summary>
    public void Stop()
    {
      //remove this player
      ServiceScope.Get<IPlayerCollection>().Remove(this);
      //goto the previous window
      ServiceScope.Get<IScreenManager>().ShowPreviousScreen();
    }

    #endregion

    #region IPlayer properties

    /// <summary>
    /// gets the Name of the Player
    /// </summary>
    /// <value></value>
    public string Name
    {
      get
      {
        return "PicturePlayer";
      }
    }

    /// <summary>
    /// gets the playback state
    /// </summary>
    /// <value></value>
    public PlaybackState State
    {
      get { return _state; }
    }

    /// <summary>
    /// gets/sets the width/height for the video window
    /// </summary>
    /// <value></value>
    public System.Drawing.Size DisplaySize
    {
      get
      {
        return new System.Drawing.Size(0, 0);

      }
      set
      {

      }
    }

    public Size Size { get { return new Size(0, 0); } }

    /// <summary>
    /// gets/sets the position on screen where the video should be drawn
    /// </summary>
    /// <value></value>
    public System.Drawing.Point DisplayPosition
    {
      get
      {

        return new System.Drawing.Point(0, 0);
      }
      set
      {

      }
    }

    /// <summary>
    /// gets/sets wheter video is paused
    /// </summary>
    /// <value></value>
    public bool Paused
    {
      get
      {
        return false;
      }
      set
      {

      }
    }

    /// <summary>
    /// returns the current play time
    /// </summary>
    /// <value></value>
    public TimeSpan CurrentTime
    {
      get
      {
        return new TimeSpan(0, 0, 0);
      }
      set
      {

      }
    }


    /// <summary>
    /// returns the duration of the movie
    /// </summary>
    /// <value></value>
    public TimeSpan Duration
    {
      get { return new TimeSpan(0, 0, 3); }
    }

    /// <summary>
    /// Restarts playback from the start.
    /// </summary>
    public void Restart()
    {

    }

    #endregion
  }
}
