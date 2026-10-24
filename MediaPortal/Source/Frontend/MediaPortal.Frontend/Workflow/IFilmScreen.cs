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

namespace MediaPortal.Frontend.Workflow
{
  /// <summary>
  /// A screen whose picture <b>is</b> moving image: the film, the recording, the channel that is on now. The
  /// shell reads this to leave such a screen exactly as it is behind a dialog, rather than photographing it and
  /// showing the photograph.
  /// </summary>
  /// <remarks>
  /// <para>
  /// This is not the same question as <see cref="IFullScreenScreen"/>, which only says that the screen wants the
  /// window's chrome away. Throwing a screen out of focus behind a question is done with one still picture of it,
  /// taken as the question goes up - see <c>ShellWindow.BlurScreenBehind</c> - and a picture of a moving image is
  /// wrong the moment it is taken: the film plays on behind a frozen frame of itself, and a dialog asked over a
  /// film is a question about the film. A screen that merely fills the window and stands still - the wake at the
  /// door, with its one turn of a ring already gone - has nothing to go stale, and is thrown out of focus like
  /// any other ground.
  /// </para>
  /// <para>
  /// The reference had no need of the distinction: it dimmed its master behind a dialog and never blurred
  /// anything, so a moving picture merely got darker. Blurring is what this front end does, and blurring brings
  /// the question of what is worth taking a picture of.
  /// </para>
  /// </remarks>
  public interface IFilmScreen
  {
  }
}
