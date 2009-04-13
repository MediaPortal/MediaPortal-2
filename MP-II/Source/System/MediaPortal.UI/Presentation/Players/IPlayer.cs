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
using MediaPortal.Core.MediaManagement;

namespace MediaPortal.Presentation.Players
{
  public enum PlaybackState
  {
    Playing,
    Paused,
    Stopped,
    Ended
  };

  /// <summary>
  /// Generic interface for all kinds of players.
  /// Instances, which are passed via this interface, are already prepared to play a media resource.
  /// To get a player for another resource, the player manager has to be called.
  /// Typically, players support sub interfaces of this interface. Players typically will implement
  /// additional additive interfaces as well, like <see cref="ISubtitlePlayer"/>, <see cref="ISeekable"/>, etc.
  /// </summary>
  /// <remarks>
  /// Different kinds of players have very different kinds of methods and properties.
  /// The player interfaces hierarchy reflects this fact.
  /// Methods and properties, which are common to all players, are introduced by this interface.
  /// Note that each player needs also to implement the <see cref="CurrentTime"/> and <see cref="Duration"/>
  /// properties, even picture players (and other players without a "native" play time). Those players should
  /// implement these properties as if their media content would have a play duration (i.e. they should return a
  /// small amount of time to present their content, maybe 3 seconds). The reason is, when used in a playlist,
  /// this media contents will also be shown for their desired <see cref="Duration"/>, for example pictures will
  /// be shown as slideshow.
  /// <i>FIXME: Hint: This interface and its sub interfaces are still being subject to change during the
  /// rework of the players subsystem.</i>
  /// </remarks>
  public interface IPlayer
  {
    /// <summary>
    /// Gets the Name of the Player
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Returns a unique id for this player.
    /// </summary>
    Guid PlayerId { get; }

    /// <summary>
    /// Gets the playback state of this player.
    /// </summary>
    PlaybackState State { get; }

    /// <summary>
    /// Returns the current play time.
    /// </summary>
    TimeSpan CurrentTime { get; set; }

    /// <summary>
    /// Returns the playing duration of the media item.
    /// </summary>
    TimeSpan Duration { get; }

    /// <summary>
    /// Overrides any special player audio setting. If this property is set to <c>false</c>, the player isn't
    /// allowed to play audio.
    /// </summary>
    bool IsAudioEnabled { get; set; }

    /// <summary>
    /// Returns the title of the currently playing media item, or <c>null</c> if the player doesn't know the title.
    /// In this case, the current item's title of the player's playlist can be used.
    /// </summary>
    /// <remarks>
    /// This property is implemented here because the player might tell another name for the media item than the
    /// name/title of the <see cref="MediaItem"/> instance used to instantiate the player. Especially when the player
    /// is a streaming player, only the player knows the current item's title.
    /// </remarks>
    string MediaItemTitle { get; }

    /// <summary>
    /// Will be called from outside to update the <see cref="CurrentTime"/> property to the current
    /// media position.
    /// </summary>
    void UpdateTime();

    /// <summary>
    /// Stops playback.
    /// </summary>
    void Stop();

    /// <summary>
    /// Pauses playback.
    /// </summary>
    void Pause();

    /// <summary>
    /// Resumes playback.
    /// </summary>
    void Resume();

    /// <summary>
    /// Restarts playback from the beginning.
    /// </summary>
    void Restart();
  }
}
