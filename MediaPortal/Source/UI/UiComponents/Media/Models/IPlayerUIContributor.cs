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
using MediaPortal.UI.Presentation.Players;

namespace MediaPortal.UiComponents.Media.Models
{
  /// <summary>
  /// Model attending a FSC or CP state of a specific player type.
  /// </summary>
  /// <remarks>
  /// Implementors of this interface must provide a parameterless constructor.
  /// <para>
  /// The interface infrastructure is quite complicated in the media part. What is the reason for that?
  /// </para>
  /// <para>
  /// The media plugin provides a media navigation function for all kinds of media items (audio/video, all sub types like
  /// normal video, images, DVD, videos with sub title etc).
  /// We group all kinds of players by their AV-Type (all kinds of video players form the "V"ideo group and all kinds of audio
  /// players form the "A"udio group). All media items which are played by a player of the video group can be together in
  /// one single playlist and all audio media items can be in a single playlist.
  /// So, while advancing in a single playlist, multiple player types can be shown in a sequence; normal video players,
  /// image players, DVD players, subtitled players etc.
  /// All those players need special FSC and CP screens because they need to present different information and functions to the
  /// user (e.g. DVD players show buttons for the DVD menu).
  /// But because of the fact that all of those share a single playlist and thus are located in a single player context,
  /// all of them need to share a single FSC workflow state and a single CP workflow state. Those states are attended by the
  /// <see cref="VideoPlayerModel"/> respectively by the <see cref="AudioPlayerModel"/>.
  /// To make it possible that different screens can be shown per specific player type, there will be an implementation
  /// of this interface for each specific supported player type, which then is accessible via the <see cref="VideoPlayerModel"/>.
  /// </para>
  /// </remarks>
  public interface IPlayerUIContributor : IDisposable
  {
    bool BackgroundDisabled { get; }
    MediaWorkflowStateType MediaWorkflowStateType { get; }
    string Screen { get; }
    void Initialize(MediaWorkflowStateType stateType, IPlayer player);
  }
}