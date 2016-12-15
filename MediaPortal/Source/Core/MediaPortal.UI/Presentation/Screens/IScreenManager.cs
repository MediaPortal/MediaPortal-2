#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
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
using System.Collections.Generic;
using MediaPortal.UI.Presentation.SkinResources;

namespace MediaPortal.UI.Presentation.Screens
{
  public delegate void DialogCloseCallbackDlgt(string dialogName, Guid dialogInstanceId);

  /// <summary>
  /// Descriptor of an open dialog of the ScreenManager.
  /// </summary>
  public interface IDialogData
  {
    /// <summary>
    /// Name of the dialog.
    /// </summary>
    string DialogName { get; }

    /// <summary>
    /// Unique id of this dialog instance.
    /// </summary>
    Guid DialogInstanceId { get; }
  }

  /// <summary>
  /// External interface for the screen manager. The screen manager is responsible
  /// for managing (logical) screens in the current skin and theme.
  /// This interface will be used by the MediaPortal application modules to communicate with
  /// the screen manager.
  /// </summary>
  public interface IScreenManager
  {
    /// <summary>
    /// Returns the currently active resource bundle. This can be a <see cref="ISkin">skin</see> or a <see cref="ITheme">theme</see>.
    /// </summary>
    ISkinResourceBundle CurrentSkinResourceBundle { get; }

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
    /// Returns <c>true</c> if there is a super layer present at the moment, else <c>false</c>.
    /// </summary>
    bool IsSuperLayerVisible { get; }

    /// <summary>
    /// Gets the descriptors to all dialogs of the current dialog stack. The first element in the result list represents
    /// the topmost dialog.
    /// </summary>
    IList<IDialogData> DialogStack { get; }

    /// <summary>
    /// Gets the instance id of the topmost dialog.
    /// </summary>
    Guid? TopmostDialogInstanceId { get; }

    /// <summary>
    /// Gets or sets the flag which disables the screenmanager from rendering the background screen.
    /// This can be used for temporary providing another background in some screens.
    /// </summary>
    bool BackgroundDisabled { get; set; }

    /// <summary>
    /// Switches the active skin and theme. This method will set the skin with the specified <paramref name="newSkinName"/>
    /// and the theme belonging to this skin with the specified <paramref name="newThemeName"/>, or the default theme for this skin.
    /// </summary>
    /// <param name="newSkinName">Name of the skin to load or <c>null</c> to use the current skin.</param>
    /// <param name="newThemeName">Name of the theme to load or <c>null</c>, if the default theme should be used.</param>
    void SwitchSkinAndTheme(string newSkinName, string newThemeName);

    /// <summary>
    /// Reloads skin and theme.
    /// </summary>
    void ReloadSkinAndTheme();

    /// <summary>
    /// Checks if the screen with the given <paramref name="screenName"/> is visible and shows it, if necessary.
    /// </summary>
    /// <param name="screenName">Name of the screen to be shown.</param>
    /// <returns><see cref="Guid"/> instance which uniquely identifies the new screen, if the screen could
    /// successfully be loaded, else <c>null</c>.</returns>
    Guid? CheckScreen(string screenName);

    /// <summary>
    /// Checks if the screen with the given <paramref name="screenName"/> is visible and shows it, if necessary.
    /// </summary>
    /// <param name="screenName">Name of the screen to be shown.</param>
    /// <param name="backgroundEnabled">Whether this screen will be displayed with a background.</param>
    /// <returns><see cref="Guid"/> instance which uniquely identifies the new screen, if the screen could
    /// successfully be loaded, else <c>null</c>.</returns>
    Guid? CheckScreen(string screenName, bool backgroundEnabled);

    /// <summary>
    /// Shows the screen with the given <paramref name="screenName"/>. All dialogs will be closed.
    /// </summary>
    /// <param name="screenName">Name of the screen to be shown.</param>
    /// <returns><see cref="Guid"/> instance which uniquely identifies the new screen, if the screen could
    /// successfully be loaded, else <c>null</c>.</returns>
    Guid? ShowScreen(string screenName);

    /// <summary>
    /// Shows the screen with the given <paramref name="screenName"/>. All dialogs will be closed.
    /// </summary>
    /// <param name="screenName">Name of the screen to be shown.</param>
    /// <param name="backgroundEnabled">Whether this screen will be displayed with a background.</param>
    /// <returns><see cref="Guid"/> instance which uniquely identifies the new screen, if the screen could
    /// successfully be loaded, else <c>null</c>.</returns>
    Guid? ShowScreen(string screenName, bool backgroundEnabled);

    /// <summary>
    /// Exchanges the current screen with the screen with the given <paramref name="screenName"/>.
    /// All current dialogs will be left open.
    /// </summary>
    /// <param name="screenName">Name of the screen to be shown instead of the current screen.</param>
    /// <returns><c>true</c>, if the screen could successfully be loaded, else <c>false</c>.</returns>
    bool ExchangeScreen(string screenName);

    /// <summary>
    /// Exchanges the current screen with the screen with the given <paramref name="screenName"/>.
    /// All current dialogs will be left open.
    /// </summary>
    /// <param name="screenName">Name of the screen to be shown instead of the current screen.</param>
    /// <param name="backgroundEnabled">Whether this screen will be displayed with a background.</param>
    /// <returns><c>true</c>, if the screen could successfully be loaded, else <c>false</c>.</returns>
    bool ExchangeScreen(string screenName, bool backgroundEnabled);

    /// <summary>
    /// Shows the dialog screen with the specified name.
    /// </summary>
    /// <returns><see cref="Guid"/> instance which uniquely identifies the new dialog, if the dialog screen could
    /// successfully be loaded, else <c>null</c>.</returns>
    Guid? ShowDialog(string dialogName);

    /// <summary>
    /// Shows the dialog screen with the specified name and calls the specified notification
    /// callback method when the dialog is closed.
    /// </summary>
    /// <remarks>
    /// The close delegate won't be called when the system goes down while the dialog is still being shown.
    /// </remarks>
    /// <param name="dialogName">The logical screen name of the dialog to show.</param>
    /// <param name="dialogCloseCallback">Callback delegate method to be called when the dialog
    /// gets closed, or <c>null</c>.</param>
    /// <returns><see cref="Guid"/> instance which uniquely identifies the new dialog, if the dialog screen could
    /// successfully be loaded, else <c>null</c>.</returns>
    Guid? ShowDialog(string dialogName, DialogCloseCallbackDlgt dialogCloseCallback);

    /// <summary>
    /// Shows the specified background screen as background layer.
    /// </summary>
    /// <remarks>
    /// The screen given by <paramref name="backgroundName"/> must be a background, typically the skin engine
    /// will search it in the skin's backgrounds directory. See the docs of the skin engine being used.
    /// </remarks>
    /// <param name="backgroundName">Name of a screen to show as background. This will typically be a media
    /// player or a background image.</param>
    /// <returns><c>true</c>, if the background screen could successfully be loaded, else <c>false</c>.</returns>
    bool SetBackgroundLayer(string backgroundName);

    /// <summary>
    /// Shows the specified screen as super layer.
    /// </summary>
    /// <remarks>
    /// The super layer is used for showing busy indicators, volume indicators and other screens which must be
    /// shown on top of all other visible screens. The screen given by <paramref name="superLayerName"/> must be
    /// a super layer screen which will typically be searched in the skin's super layer directory. See the docs of
    /// the skin engine being used.
    /// </remarks>
    /// <param name="superLayerName">Name of the super layer screen to show as super layer.</param>
    /// <returns><c>true</c>, if the super layer screen could successfully be loaded, else <c>false</c>.</returns>
    bool SetSuperLayer(string superLayerName);

    /// <summary>
    /// Closes the dialog with the given instance id.
    /// </summary>
    /// <param name="dialogInstanceId">Instance id of the dialog to close.</param>
    void CloseDialog(Guid dialogInstanceId);

    /// <summary>
    /// Closes all dialogs until a dialog with the given instance id is found.
    /// </summary>
    /// <remarks>
    /// If no dialog with the given <paramref name="dialogInstanceId"/> is found, no dialogs are removed from the dialog stack.
    /// </remarks>
    /// <param name="dialogInstanceId">Instance id of the dialog to close.</param>
    /// <param name="inclusive">If set to <c>true</c>, the dialog with the given <paramref name="dialogInstanceId"/>
    /// will be removed too, else only dialogs on top of it are removed.</param>
    void CloseDialogs(Guid dialogInstanceId, bool inclusive);

    /// <summary>
    /// Closes the topmost dialog.
    /// </summary>
    void CloseTopmostDialog();

    /// <summary>
    /// Reloads background, current screen and all dialogs.
    /// </summary>
    void Reload();

    /// <summary>
    /// Avoids exchanges of the screen and changes of dialogs until <see cref="EndBatchUpdate"/> is called.
    /// </summary>
    /// <remarks>
    /// This method will collect update requests of screens and dialogs which are done in methods <see cref="ShowScreen(string)"/>,
    /// <see cref="ShowScreen(string,bool)"/>, <see cref="ExchangeScreen(string)"/>, <see cref="ExchangeScreen(string,bool)"/>,
    /// <see cref="ShowDialog(string)"/>, <see cref="ShowDialog(string,DialogCloseCallbackDlgt)"/>
    /// and <see cref="SetBackgroundLayer"/>. Between the calls of <see cref="StartBatchUpdate"/> and
    /// <see cref="EndBatchUpdate"/>, the render thread will continue to render the screen/dialogs which were active
    /// at the time when <see cref="StartBatchUpdate"/> was called.
    /// When <see cref="EndBatchUpdate"/> is called, the collected screen and dialog updates are executed at once.
    /// The screen update sequence requests will be adjusted when there are calls which can be overridden by the sequence,
    /// i.e. if there are multiple calls to <see cref="ShowScreen(string)"/>/<see cref="ShowScreen(string,bool)"/> or
    /// <see cref="ExchangeScreen(string)"/>/<see cref="ExchangeScreen(string,bool)"/>, the final sequence
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
