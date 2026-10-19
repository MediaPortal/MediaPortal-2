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
  /// A screen that wants the whole window to itself: the window leaves its master chrome - the skin's header
  /// and footer - away while this screen is on top.
  /// </summary>
  /// <remarks>
  /// Both reference skins worked this way: the film was the master screen, not a control placed inside one.
  /// A clock over the opening scene or a now-playing footer across the end credits is what those masters made
  /// sure of by not having them, and a media centre should repeat that care rather than rediscover it.
  /// </remarks>
  public interface IFullScreenScreen
  {
  }
}
