#region Copyright (C) 2007-2009 Team MediaPortal

/*
    Copyright (C) 2007-2009 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal II

    MediaPortal II is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal II is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
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
    /// Shows the specified screen. All dialogs will be closed.
    /// If the screen was not prepared yet, this method will prepare the screen first before
    /// showing it.
    /// </summary>
    /// <param name="screenName">Name of the screen to be shown.</param>
    /// <returns><c>true</c>, if the screen could successfully be shown, else <c>false</c>.</returns>
    bool ShowScreen(string screenName);

    /// <summary>
    /// Exchanges the current screen with the screen with the given <paramref name="screenName"/>.
    /// All current dialogs will be left open.
    /// </summary>
    /// <param name="screenName">Name of the screen to be shown instead of the current screen.</param>
    /// <returns><c>true</c>, if the screen could successfully be shown, else <c>false</c>.</returns>
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
  }
}
