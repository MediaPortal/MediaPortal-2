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

namespace MediaPortal.Frontend.Player
{
  /// <summary>
  /// One chapter as the media carries it: where it begins, and what the media calls it when it calls it anything.
  /// </summary>
  /// <remarks>
  /// A position and a name, and nothing else. A screen draws a chapter as a mark at
  /// <see cref="PositionMs"/> of the media's length, which is a question of arithmetic rather than of engine, so
  /// no engine's chapter index - which counts from one on some containers and from nought on others, and counts
  /// menu entries rather than chapters on a disc - needs to reach the screen.
  /// </remarks>
  public sealed class PlaybackChapter
  {
    public PlaybackChapter(long positionMs, string? name = null)
    {
      PositionMs = positionMs < 0 ? 0 : positionMs;
      Name = string.IsNullOrWhiteSpace(name) ? null : name!.Trim();
    }

    /// <summary>Where the chapter begins, in ms from the start of the media. Never negative.</summary>
    public long PositionMs { get; }

    /// <summary>What the media calls this chapter, or null when the media does not name it.</summary>
    public string? Name { get; }

    public override string ToString()
    {
      return Name == null ? PositionMs + " ms" : PositionMs + " ms '" + Name + "'";
    }
  }
}
