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

namespace MediaPortal.Core.WindowManager
{
  /// <summary>
  /// interface for a generic window manager
  /// </summary>
  public interface IWindowManager
  {
    /// <summary>
    /// Closes the current dialog.
    /// </summary>
    void CloseDialog();

    /// <summary>
    /// Shows the dialog.
    /// </summary>
    /// <param name="window">dialog window name.</param>
    void ShowDialog(string window);

    /// <summary>
    /// Prepares the window by loading & initializing the window 
    /// but dont show it (yet)
    /// </summary>
    /// <param name="window">The window.</param>
    void PrepareWindow(string window);

    /// <summary>
    /// Shows the window.
    /// If window is not yet prepared, then this method will prepare the window and then show it
    /// </summary>
    /// <param name="windowName">Name of the window.</param>
    void ShowWindow(string windowName);

    /// <summary>
    /// Shows the previous window.
    /// </summary>
    void ShowPreviousWindow();

    /// <summary>
    /// Gets the current window.
    /// </summary>
    /// <value>The current window.</value>
    IWindow CurrentWindow { get; }

    /// <summary>
    /// Reloads the current window
    /// </summary>
    void Reload();

    /// <summary>
    /// switches the gui to another skin
    /// </summary>
    /// <param name="newSkinName">name of the skin to use.</param>
    void SwitchSkin(string newSkinName);

    /// <summary>
    /// Gets the name of the current skin used.
    /// </summary>
    /// <value>The name of the current skin used.</value>
    string SkinName { get;}

    /// <summary>
    /// Switches the gui to a different theme.
    /// </summary>
    /// <param name="newThemeName">name of the theme.</param>
    void SwitchTheme(string newThemeName);

    /// <summary>
    /// Gets the name of the current theme used
    /// </summary>
    /// <value>The name of the current theme used.</value>
    string ThemeName { get;}
  }
}
