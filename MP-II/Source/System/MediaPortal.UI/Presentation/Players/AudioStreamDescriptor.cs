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

namespace MediaPortal.UI.Presentation.Players
{
  /// <summary>
  /// Object to describe an audio stream to be played.
  /// </summary>
  public class AudioStreamDescriptor
  {
    #region Protected fields

    protected IPlayerContext _playerContext;
    protected string _playerName;
    protected string _audioStreamName;

    #endregion

    #region Ctor

    public AudioStreamDescriptor(IPlayerContext playerContext, string playerName, string audioStreamName)
    {
      _playerContext = playerContext;
      _playerName = playerName;
      _audioStreamName = audioStreamName;
    }

    #endregion

    #region Public properties

    /// <summary>
    /// Returns the information, which player context is providing the audio stream.
    /// </summary>
    public IPlayerContext PlayerContext
    {
      get { return _playerContext; }
    }

    /// <summary>
    /// Returns the name of the player providing the audio stream described by this instance.
    /// </summary>
    public string PlayerName
    {
      get { return _playerName; }
    }

    /// <summary>
    /// Returns the name of the audio stream described by this instance. This stream name is unique in the player
    /// providing the stream, it might not be unique among all players. 
    /// </summary>
    /// <remarks>
    /// To let the user choose between audio streams, to make the choice unique, the <see cref="PlayerName"/> should
    /// be presented together with the <see cref="AudioStreamName"/>, like this:
    /// <code>
    /// AudioStreamDescriptor asd = ...;
    /// string choiceItemName = asd.PlayerName + ": " + asd.AudioStreamName;
    /// ...
    /// </code>
    /// </remarks>
    public string AudioStreamName
    {
      get { return _audioStreamName; }
    }

    #endregion
  }
}
