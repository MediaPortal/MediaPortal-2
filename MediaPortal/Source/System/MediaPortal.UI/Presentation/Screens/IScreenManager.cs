#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

namespace MediaPortal.UI.Presentation.Screens
{
  public delegate void DialogCloseCallbackDlgt(string dialogName);

  /// <summary>
  /// External interface for the screen manager. The screen manager is responsible
  /// for managing (logical) screens in the current skin and theme.
  /// This interface will be used by the MediaPortal application modules to communicate with
  /// the screen manager.
  /// </summary>
  public interface IScreenManager
  {
    /// <summary>
    /// Gets the logical name of the current skin used.
    /// </summary>
    string SkinName { get;}

    /// <summary>
    /// Gets the name of the current theme used.
    /// </summary>
    string ThemeName { get;}

    /// <summary>
    /// Gets the name of the currently active screen (or dialog).
    /// </summary>
    string ActiveScreenName { get; }

    /// <summary>
    /// Gets the name of the currently active background screen.
    /// </summary>
    string ActiveBackgroundScreenName { get; }

    /// <summary>
    /// Returns <c>true</c> if a dialog is currently visible, else <c>false</c>.
    /// </summary>
    bool IsDialogVisible { get; }

    /// <summary>
    /// Gets or sets the flag which disables the screenmanager from rendering the background screen.
    /// This can be used for temporary providing another background in some screens.
    /// </summary>
    bool BackgroundDisabled { get; set; }

    /// <summary>
    /// Switches the GUI to the specified skin, using the default theme of the skin.
    /// </summary>
    /// <param name="newSkinName">Logical name of the skin.</param>
    void SwitchSkin(string newSkinName);

    /// <summary>
    /// Switches the GUI to the specified theme.
    /// </summary>
    /// <param name="newThemeName">Logical name of the theme.</param>
    void SwitchTheme(string newThemeName);

    /// <summary>
    /// Shows the screen with the given <paramref name="screenName"/>. All dialogs will be closed.
    /// </summary>
    /// <param name="screenName">Name of the screen to be shown.</param>
    /// <returns><c>true</c>, if the screen could successfully be prepared, else <c>false</c>.</returns>
    bool ShowScreen(string screenName);

    /// <summary>
    /// Exchanges the current screen with the screen with the given <paramref name="screenName"/>.
    /// All current dialogs will be left open.
    /// </summary>
    /// <param name="screenName">Name of the screen to be shown instead of the current screen.</param>
    /// <returns><c>true</c>, if the screen could successfully be prepared, else <c>false</c>.</returns>
    bool ExchangeScreen(string screenName);

    /// <summary>
    /// Shows the dialog screen with the specified name.
    /// </summary>
    /// <param name="dialogName">The logical screen name of the dialog to show.</param>
    /// <returns><c>true</c>, if the dialog screen could successfully be shown, else <c>false</c>.</returns>
    bool ShowDialog(string dialogName);

    /// <summary>
    /// Shows the dialog screen with the specified name and calls the specified notification
    /// callback method when the dialog is closed.
    /// </summary>
    /// <param name="dialogName">The logical screen name of the dialog to show.</param>
    /// <param name="dialogCloseCallback">Callback delegate method to be called when the dialog
    /// gets closed, or <c>null</c>.</param>
    /// <returns><c>true</c>, if the dialog screen could successfully be shown, else <c>false</c>.</returns>
    bool ShowDialog(string dialogName, DialogCloseCallbackDlgt dialogCloseCallback);

    /// <summary>
    /// Shows the specified screen as background layer.
    /// </summary>
    /// <param name="backgroundName">Name of a screen to show as background. This will typically be a media
    /// player or a background image.</param>
    /// <returns><c>true</c>, if the background screen could successfully be set, else <c>false</c>.</returns>
    bool SetBackgroundLayer(string backgroundName);

    /// <summary>
    /// Closes the topmost dialog.
    /// </summary>
    void CloseDialog();

    /// <summary>
    /// Reloads background, current screen and all dialogs.
    /// </summary>
    void Reload();

    /// <summary>
    /// Avoids exchanges of the screen and changes of dialogs until <see cref="EndBatchUpdate"/> is called.
    /// </summary>
    /// <remarks>
    /// This method will collect update requests of screens and dialogs which are done in methods <see cref="ShowScreen"/>,
    /// <see cref="ExchangeScreen"/>, <see cref="ShowDialog(string)"/>, <see cref="ShowDialog(string,DialogCloseCallbackDlgt)"/>
    /// and <see cref="SetBackgroundLayer"/>. Between the calls of <see cref="StartBatchUpdate"/> and
    /// <see cref="EndBatchUpdate"/>, the render thread will continue to render the screen/dialogs which were active
    /// at the time when <see cref="StartBatchUpdate"/> was called.
    /// When <see cref="EndBatchUpdate"/> is called, the collected screen and dialog updates are executed at once.
    /// The screen update sequence requests will be adjusted when there are calls which can be overridden by the sequence,
    /// i.e. if there are multiple calls to <see cref="ShowScreen"/> or <see cref="ExchangeScreen"/>, the final sequence
    /// which is executed by method <see cref="EndBatchUpdate"/> will only invoke the last screen update.
    /// It is safe to call this method multiple times; for each call to <see cref="StartBatchUpdate"/>, a call to
    /// <see cref="EndBatchUpdate"/> is necessary.
    /// </remarks>
    void StartBatchUpdate();

    /// <summary>
    /// Ends batch update mode and executes all screen/dialog changes which were recorded between the call of
    /// <see cref="StartBatchUpdate"/> and the call to this method.
    /// </summary>
    /// <remarks>
    /// As call pairs of <see cref="StartBatchUpdate"/>/<see cref="EndBatchUpdate"/> can be stacked, only the last
    /// <see cref="EndBatchUpdate"/> will trigger the screen updates.
    /// </remarks>
    void EndBatchUpdate();
  }
}
