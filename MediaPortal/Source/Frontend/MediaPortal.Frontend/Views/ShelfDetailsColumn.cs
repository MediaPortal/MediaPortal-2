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

namespace MediaPortal.Frontend.Views
{
  /// <summary>
  /// The one measure by which a media screen divides its room between its wall and its column of words.
  /// </summary>
  /// <remarks>
  /// <para>
  /// The reference handed its details column a thousand fixed points and its media area whatever was left, which
  /// was a fair thing to do on a screen of a known width. This front end is stood on desktop windows of every
  /// shape as well, and a details column that keeps its thousand points on a small window leaves the wall a
  /// gutter to lay its tiles in - tiles that then wrap into one narrow column, so that a viewer on a laptop sees
  /// a single file of posters where the television shows a wall of them.
  /// </para>
  /// <para>
  /// The column is therefore measured in points of its own rather than left to a grid's proportions, and for two
  /// reasons: a proportion is a share of whatever is left after the content of the other columns has taken what
  /// it insists on, so a wide picture or a long word in the details silently takes points from the wall, while a
  /// told number of points cannot be argued with. The number is the share of the room the reference divided, held
  /// between the width below which the column is no longer a column and the reference's own thousand - and never
  /// so much that the wall is left narrower than two tiles, which is the one question a library screen is asked.
  /// With the details away the column is nothing at all and the wall has the room to itself.
  /// </para>
  /// <para>
  /// It is a class of its own because a second screen wears the same column: the wall of people lays names where
  /// a shelf lays posters and stands the same column of words at the same end of the same room. Two screens that
  /// divide their room by two copies of four numbers drift into two looks within a month, which is what this file
  /// prevents.
  /// </para>
  /// </remarks>
  internal static class ShelfDetailsColumn
  {
    /// <summary>
    /// The share of the room the column is offered: the reference's own split, measured at the width it drew
    /// for - a thousand of its nineteen hundred and twenty points are its details, which is this share.
    /// </summary>
    internal const double SHARE = 0.52;

    /// <summary>Under this width the column stops being a column and is only a strip, so it is not given less.</summary>
    internal const double MIN = 340;

    /// <summary>The reference's own width, which is the most a column ever asks for.</summary>
    internal const double MAX = 1000;

    /// <summary>
    /// The width a wall is never allowed to fall under: two tiles of the poster shape a media screen is most
    /// often worn in. A column that leaves less than that lays the wall out in one long line of single tiles,
    /// which is a list wearing a grid's clothes.
    /// </summary>
    internal const double WALL_MIN = 470;

    /// <summary>
    /// The line a media screen's words stop above: nothing the column says or shows stands under it.  The viewer
    /// drew the line on 2026-11-10 at seventy lines and asked for thirty more the same day after seeing it.
    /// </summary>
    internal const double FLOOR = 100;

    /// <summary>
    /// The least height a column of words is worth being measured for - one story's worth, which is also what a
    /// story box asks for itself.  Under this the measuring stands back: a window of that shape has no room for
    /// the column at all, and there the clipping, not a guess made here, keeps the words off the black.
    /// </summary>
    internal const double WORDS_FLOOR = 160;

    /// <summary>
    /// How tall the box of words stands in a room <paramref name="roomHeight"/> lines high when its first line
    /// begins <paramref name="wordsTop"/> lines into the room: everything down to the floor the viewer drew, and
    /// never less than one story's worth.  A box only keeps to a height it is handed, and a column that is handed
    /// none grows to whatever its words ask for - which is how a long reading used to run past the bottom of the
    /// screen and cut its last line through the middle of a glyph.
    /// </summary>
    internal static double WordsHeightFor(double roomHeight, double wordsTop)
    {
      if (roomHeight <= 0 || wordsTop < 0)
        return double.NaN;

      double room = roomHeight - FLOOR - wordsTop;
      return room < WORDS_FLOOR ? WORDS_FLOOR : room;
    }

    /// <summary>
    /// How many points of a room of <paramref name="roomWidth"/> the column of words takes, or nought when the
    /// viewer has turned the words away and the wall stands over the whole room.
    /// </summary>
    internal static double WidthFor(double roomWidth, bool shown)
    {
      if (!shown || roomWidth <= 0)
        return 0;

      double details = Math.Min(Math.Clamp(roomWidth * SHARE, MIN, MAX), roomWidth - WALL_MIN);
      return details < 0 ? 0 : details;
    }
  }
}
