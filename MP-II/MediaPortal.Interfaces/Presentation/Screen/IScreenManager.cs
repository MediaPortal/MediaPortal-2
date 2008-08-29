#region Copyright (C) 2007-2008 Team MediaPortal

/*
    Copyright (C) 2007-2008 Team MediaPortal
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

using System;

namespace MediaPortal.Presentation.Screen
{
  /// <summary>
  /// External interface for the screen manager. The screen manager is responsible
  /// for managing (logical) screens in the current skin and theme.
  /// This interface will be used by the MediaPortal application modules to communicate with
  /// the screen manager.
  /// </summary>
  public interface IScreenManager
  {
    /// <summary>
    /// Gets access to the skin resource accessor, which can load resource files in the context
    /// of the currently active skin.
    /// </summary>
    IResourceAccessor SkinResourceContext { get; }

    /// <summary>
    /// Closes the top most main dialog (together with it's children).
    /// </summary>
    void CloseDialog();

    /// <summary>
    /// Shows a (main) dialog with the specified name.
    /// </summary>
    /// <param name="dialogName">The dialog name.</param>
    void ShowDialog(string dialogName);

    /// <summary>
    /// Shows a child dialog with the specified name. The dialog will be marked as child and will 
    /// be closed when main dialog is closed.
    /// </summary>
    /// <param name="dialogName">The dialog name.</param>
    void ShowChildDialog(string dialogName);

    /// <summary>
    /// Prepares the specified screen by loading & initializing it.
    /// The screen won't be shown (yet).
    /// </summary>
    /// <param name="screenName">The name of the screen to be prepared.</param>
    /// <returns><c>true</c>, if the specified screen is available and could be loaded,
    /// <c>false</c> if the screen isn't available or if there was a error while loading it.</returns>
    bool PrepareScreen(string screenName);

    /// <summary>
    /// Shows the specified screen.
    /// If the screen was not prepared yet, this method will prepare the screen first before
    /// showing it.
    /// </summary>
    /// <param name="screenName">Name of the screen to be shown.</param>
    bool ShowScreen(string screenName);

    /// <summary>
    /// Closes a currently visible dialog or shows the previous screen from the screen history.
    /// </summary>
    void ShowPreviousScreen();

    void Reset();

    /// <summary>
    /// Switches the GUI to the specified skin, using the default theme of the skin.
    /// </summary>
    /// <param name="newSkinName">Logical name of the skin.</param>
    void SwitchSkin(string newSkinName);

    /// <summary>
    /// Gets the logical name of the current skin used.
    /// </summary>
    string SkinName { get;}

    /// <summary>
    /// Switches the GUI to the specified theme.
    /// </summary>
    /// <param name="newThemeName">Logical name of the theme.</param>
    void SwitchTheme(string newThemeName);

    /// <summary>
    /// Gets the name of the current theme used.
    /// </summary>
    string ThemeName { get;}

    /// <summary>
    /// Gets the name of the currently shown screen (or dialog).
    /// </summary>
    string CurrentScreenName { get; }

    /// <summary>
    /// Returns <c>true</c> if a dialog is currently visible, else <c>false</c>.
    /// </summary>
    bool IsDialogVisible { get; }

    #region To be removed

    /// <summary>
    /// Gets / Sets the Title of a Dialog
    /// </summary>
    [Obsolete("This method will be replaced by a generic approach in the future")]
    string DialogTitle { get; set; }

    /// <summary>
    /// Gets / Sets Dialog Line 1
    /// </summary>
    [Obsolete("This method will be replaced by a generic approach in the future")]
    string DialogLine1 { get; set; }

    /// <summary>
    /// Gets / Sets Dialog Line 2
    /// </summary>
    [Obsolete("This method will be replaced by a generic approach in the future")]
    string DialogLine2 { get; set; }

    /// <summary>
    /// Gets / Sets Dialog Line 3
    /// </summary>
    [Obsolete("This method will be replaced by a generic approach in the future")]
    string DialogLine3 { get; set; }

    /// <summary>
    /// Gets the Dialog Response (Yes/No)
    /// </summary>
    /// <returns></returns>
    [Obsolete("This method will be replaced by a generic approach in the future")]
    bool GetDialogResponse();

    /// <summary>
    /// Sets the Dialog Response
    /// </summary>
    /// <param name="response"></param>
    [Obsolete("This method will be replaced by a generic approach in the future")]
    void SetDialogResponse(string response);

    #endregion
  }
}
