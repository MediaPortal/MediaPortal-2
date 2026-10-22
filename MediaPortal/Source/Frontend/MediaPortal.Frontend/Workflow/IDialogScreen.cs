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
  /// A stop of the workflow that is asked as a <b>dialog</b> rather than walked into as a screen: the place the
  /// viewer came from stays lit behind it, the skin's header and the breadcrumb step back, and the answer the
  /// dialog gets is handed to the screen underneath.
  /// </summary>
  /// <remarks>
  /// <para>
  /// The reference kept these in a dialog system of its own (<c>dialogSystemState</c>): a dialog was a screen
  /// raised over the master, never a master replacing the master, so the menu editor showed the home behind it
  /// and the home's own header said nothing while the editor was up. Only the dialog's frame was ever drawn -
  /// and a dialog that painted its own black ground would hide the very screen it is a question about.
  /// </para>
  /// <para>
  /// A dialog is still a frame, so Back means exactly what it means everywhere else: it closes this dialog and
  /// leaves the viewer on the screen behind, with the scroll position and the selection they left. What it is
  /// not is a stop that takes the television over; see <see cref="IFullScreenScreen"/>, which is the opposite
  /// request - a screen that wants everything, dialog included nothing.
  /// </para>
  /// </remarks>
  public interface IDialogScreen
  {
  }
}
