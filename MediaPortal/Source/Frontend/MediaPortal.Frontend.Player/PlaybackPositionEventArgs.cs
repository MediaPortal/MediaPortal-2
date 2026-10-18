/*
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

using System;

namespace MediaPortal.Frontend.Player
{
  /// <summary>
  /// Where playback has got to, as the engine reported it.
  /// </summary>
  /// <remarks>
  /// <para>
  /// A screen needs this more often than it needs state changes - a progress bar that only moves when something
  /// is pressed is obviously broken - so the seam carries position events rather than making every screen poll.
  /// Polling is what the old tree did, and it is how the old tree ended up with a position read from a filter
  /// graph on the UI thread several times a second.
  /// </para>
  /// <para>
  /// Both values are in milliseconds and both are what the engine believed at the moment it raised the event,
  /// which is not necessarily what a call made a moment ago asked for: a seek is answered by the position the
  /// decoder reached afterwards, so a screen that animates a slider should follow the events rather than its own
  /// requests.
  /// </para>
  /// </remarks>
  public sealed class PlaybackPositionEventArgs : EventArgs
  {
    public PlaybackPositionEventArgs(long timeInMs, long lengthInMs)
    {
      TimeInMs = timeInMs;
      LengthInMs = lengthInMs;
    }

    /// <summary>Position in the media, in ms. Never negative.</summary>
    public long TimeInMs { get; }

    /// <summary>Length of the media in ms; 0 when the engine does not know it yet, or it has none (a live stream).</summary>
    public long LengthInMs { get; }

    /// <summary>
    /// 0 to 1, or -1 when the length is unknown - which is different from 0, because a screen shows a live stream
    /// as "no end" and a stopped file as the beginning.
    /// </summary>
    public double Fraction
    {
      get
      {
        if (LengthInMs <= 0)
          return -1;
        double fraction = (double)TimeInMs / LengthInMs;
        return fraction < 0 ? 0 : (fraction > 1 ? 1 : fraction);
      }
    }
  }
}
