#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
    http://www.team-mediaportal.com

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

#endregion

using System;

namespace MediaPortal.UI.Presentation.Screens
{
  public enum DialogType
  {
    OkDialog,
    YesNoDialog
  }

  public enum DialogButtonType
  {
    Ok,
    Yes,
    No,
    Cancel
  }

  /// <summary>
  /// Dialog management API.
  /// </summary>
  /// <remarks>
  /// This service provides an API to show general dialogs. To show a dialog, use method <see cref="ShowDialog"/>.
  /// To track the outcome of that dialog, you can register at the message channel <see cref="DialogManagerMessaging.CHANNEL"/> or
  /// use the convenience class <see cref="DialogCloseWatcher"/>.
  /// </remarks>
  public interface IDialogManager
  {
    /// <summary>
    /// Shows a generic dialog with the specified header, text and type.
    /// </summary>
    /// <param name="headerText">The header text to display.</param>
    /// <param name="text">The dialog text to show.</param>
    /// <param name="type">The type of the dialog. Depending on the type, different buttons
    /// will be shown.</param>
    /// <param name="showCancelButton">If set to <c>true</c>, an additional cancel button will be
    /// shown.</param>
    /// <param name="focusedButton">Used to set the button which should get the focus. Leave <c>null</c> to not set the
    /// focus.</param>
    /// <returns>Dialog handle. The dialog handle will be used in the dialog result message.</returns>
    Guid ShowDialog(string headerText, string text, DialogType type, bool showCancelButton, DialogButtonType? focusedButton);
  }
}
