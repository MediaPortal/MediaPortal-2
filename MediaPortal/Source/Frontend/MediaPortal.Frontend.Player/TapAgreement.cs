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
  /// When the listener of the spectrum is to be moved onto the place the playing player stands, and when it is
  /// left alone. Kept apart from both players because it is the whole of the agreement policy and can be asked
  /// about without starting two engines.
  /// </summary>
  /// <remarks>
  /// <para>
  /// The listener is its own engine: it starts when a screen asks for it, which is usually some seconds after
  /// the song began to be heard, and it is paced by its own decoder rather than by the sound a viewer hears,
  /// since it plays with no audio output at all (VisualSpectrumTap.ListeningArguments). Left to itself it
  /// therefore answers for a different moment of the song than the one being heard - measured five seconds
  /// apart on a real song, which is a strip of bars dancing a verse behind the music.
  /// </para>
  /// <para>
  /// A seek is not free for a listener, though: it drops the buffer the pictures are drawn from, so the strip
  /// goes blank for a moment. Correcting every few tenths of a second would therefore hide the sound behind
  /// its own corrections, which is why a small difference waits for its turn while a large one is mended as
  /// fast as it is noticed.
  /// </para>
  /// </remarks>
  public static class TapAgreement
  {
    /// <summary>
    /// How far apart the two may stand before the difference is worth a seek. Under this the bars answer for a
    /// moment of the song that is a fifth of a second off, which the eye cannot pick out of a moving strip.
    /// </summary>
    public const long ApartWorthMendingMs = 300;

    /// <summary>
    /// A difference this large is not a drift but a missed seek - the listener started while the song was
    /// already running, or a viewer jumped through the file - and is mended as often as it is noticed.
    /// </summary>
    public const long FarApartMs = 1500;

    /// <summary>How soon after one mending the next may follow, for a small difference.</summary>
    public const long MendingWaitMs = 2000;

    /// <summary>How soon after one mending the next may follow, for a large one.</summary>
    public const long MendFarApartWaitMs = 250;

    /// <summary>
    /// Whether the listener is to be put at <paramref name="playingMs"/> now, given where it stands, how far it
    /// stands from there, and how long ago it was last put somewhere.
    /// </summary>
    /// <param name="listenerMs">Where the listener stands in the file.</param>
    /// <param name="playingMs">
    /// Where the player the viewer hears stands. Nought or less is not a place but an answer of "not started
    /// yet", which there is nothing to agree with.
    /// </param>
    /// <param name="msSinceLastMending">
    /// How long ago the listener was last moved, or <see cref="long"/>.MaxValue when it never was.
    /// </param>
    public static bool WantsMending(long listenerMs, long playingMs, long msSinceLastMending)
    {
      if (playingMs <= 0 || listenerMs < 0)
        return false;

      long apart = Math.Abs(listenerMs - playingMs);
      if (apart < ApartWorthMendingMs)
        return false;

      return msSinceLastMending >= (apart > FarApartMs ? MendFarApartWaitMs : MendingWaitMs);
    }
  }
}
