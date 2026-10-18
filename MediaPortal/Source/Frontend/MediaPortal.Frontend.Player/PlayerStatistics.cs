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

using System.Globalization;

namespace MediaPortal.Frontend.Player
{
  /// <summary>
  /// What the engine has counted for the media it is working on: frames decoded, frames dropped.
  /// </summary>
  /// <remarks>
  /// <para>
  /// This exists because of a difference that nothing else on the seam can express. A player that reports
  /// <see cref="PlayerState.Playing"/> with a black picture and a player that reports
  /// <see cref="PlayerState.Playing"/> while decoding nothing are the same state and a different failure - the first
  /// is a presentation problem, the second is a codec problem, and they are fixed by different people reading
  /// different logs. Frames per second is a thing to watch over time (two reads, subtracted), which is why the
  /// numbers are counters and not a rate: a rate would be this assembly's opinion about a clock.
  /// </para>
  /// <para>
  /// A test can use these to say "a real 1080p file really went through the decoder" without anybody looking at
  /// a screen, which is the only way the video half of the player is measurable on a build agent.
  /// </para>
  /// </remarks>
  public readonly record struct PlayerStatistics
  {
    public PlayerStatistics(long decodedVideoFrames, long lostVideoFrames, long decodedAudioFrames)
    {
      DecodedVideoFrames = decodedVideoFrames < 0 ? 0 : decodedVideoFrames;
      LostVideoFrames = lostVideoFrames < 0 ? 0 : lostVideoFrames;
      DecodedAudioFrames = decodedAudioFrames < 0 ? 0 : decodedAudioFrames;
    }

    /// <summary>Video frames the decoder produced since this media was opened.</summary>
    public long DecodedVideoFrames { get; }

    /// <summary>
    /// Video frames the engine gave up on because it could not show them in time. Not zero on a machine that is
    /// too slow for the file, and worth showing somewhere, because "it stutters" is otherwise invisible.
    /// </summary>
    public long LostVideoFrames { get; }

    /// <summary>Audio frames the decoder produced since this media was opened.</summary>
    public long DecodedAudioFrames { get; }

    /// <summary>
    /// Nothing counted. The answer when no media is open, and the answer an engine gives when it does not count;
    /// never null, so that a caller can subtract two readings without asking whether either had media behind it.
    /// </summary>
    public static PlayerStatistics None
    {
      get { return new PlayerStatistics(0, 0, 0); }
    }

    /// <summary>True when no frame of any kind has been counted.</summary>
    public bool NothingCounted
    {
      get { return DecodedVideoFrames == 0 && LostVideoFrames == 0 && DecodedAudioFrames == 0; }
    }

    public override string ToString()
    {
      return string.Format(CultureInfo.InvariantCulture, "{0} video / {1} audio frame(s), {2} lost",
                           DecodedVideoFrames, DecodedAudioFrames, LostVideoFrames);
    }
  }
}
