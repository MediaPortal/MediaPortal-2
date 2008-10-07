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

namespace Media.Players.BassPlayer
{
  // Todo: these will become available elsewhere sometime (Mantis 0001647).
  static class MPMessages
  {
    /// <summary>
    /// Notifies MP that playback has started.
    /// </summary>
    public const string Started = "started";

    /// <summary>
    /// Notifies MP to provide the next track from its playlist.
    /// </summary>
    public const string NextFile = "nextfile";
    
    /// <summary>
    /// Notifies MP that playback has ended.
    /// </summary>
    public const string Ended = "ended";
  }

  static class Constants
  {
    public const int Auto = -1;
  }

  public partial class BassStream
  {
    static class Constants
    {
      public const int FloatBytes = 4;
      public const int BassErrorEnded = 45;
    }
  }

  public partial class BassPlayer
  {
    static class Constants
    {
      public const int FloatBytes = 4;
      public const int BassDefaultDevice = -1;
      public const int BassNoSoundDevice = 0;
      public const int BassInvalidHandle = 0;
    }
    
    static class StaticSettings
    {
      public static TimeSpan VizLatencyCorrectionRange = TimeSpan.FromMilliseconds(500);
      public const string AudioDecoderPath = @"MusicPlayer\Plugins\Audio Decoders";
    }
  }
}