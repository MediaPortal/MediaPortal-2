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
using System.Collections.Generic;
using MediaPortal.Presentation.Screen;
using MediaPortal.Presentation.SkinResources;
using MediaPortal.Presentation.Workflow;
using MediaPortal.SkinEngine.ContentManagement;
using MediaPortal.SkinEngine.Controls.Visuals;
using MediaPortal.Core;
using MediaPortal.Core.Logging;
using MediaPortal.Presentation.Players;
using MediaPortal.Core.Settings;
using MediaPortal.Control.InputManager;
using MediaPortal.SkinEngine.Settings;
using MediaPortal.SkinEngine.SkinManagement;

namespace MediaPortal.SkinEngine.ScreenManagement
{
  public class ScreenManager : IScreenManager
  {
    protected class WorkflowManagerModelLoader : IModelLoader
    {
      public object GetOrLoadModel(Guid modelId)
      {
        return ServiceScope.Get<IWorkflowManager>().GetModel(modelId);
      }
    }

    #region Variables

    public const string HOME_SCREEN = "home";

    private readonly object _syncRoot = new object();
    private Screen _currentScreen = null;
    private readonly Stack<Screen> _dialogStack = new Stack<Screen>();

    private readonly SkinManager _skinManager;
    private Skin _skin = null;
    private Theme _theme = null;

    // Albert78: Next fields are to be removed
    private string _dialogTitle;
    private string[] _dialogLines = new string[3];
    private bool _dialogResponse;  // Yes = true, No = false

    #endregion

    public ScreenManager()
    {
      SkinSettings screenSettings = ServiceScope.Get<ISettingsManager>().Load<SkinSettings>();
      _skinManager = new SkinManager();

      string skinName = screenSettings.Skin;
      string themeName = screenSettings.Theme;
      if (string.IsNullOrEmpty(skinName))
      {
        skinName = SkinManager.DEFAULT_SKIN;
        themeName = null;
      }

      // Prepare the skin and theme - the theme will be activated in method MainForm_Load
      ServiceScope.Get<ILogger>().Debug("ScreenManager: Loading skin '{0}', theme '{1}'", skinName, themeName);
      PrepareSkinAndTheme(skinName, themeName);

      // Update the settings with our current skin/theme values
      if (screenSettings.Skin != SkinName || screenSettings.Theme != ThemeName)
      {
        screenSettings.Skin = _skin.Name;
        screenSettings.Theme = _theme == null ? null : _theme.Name;
        ServiceScope.Get<ISettingsManager>().Save(screenSettings);
      }
    }

    public ISkinResourceManager SkinResourceManager
    {
      get { return _skinManager; }
    }

    /// <summary>
    /// Disposes all resources which were allocated by the screen manager.
    /// </summary>
    public void Dispose()
    {
      _skinManager.Dispose();
    }

    /// <summary>
    /// Prepares the skin and theme, this will load the skin and theme instances and
    /// set it as the current skin and theme in the <see cref="SkinContext"/>.
    /// After calling this method, the <see cref="SkinContext.SkinResources"/>
    /// contents can be requested.
    /// </summary>
    /// <param name="skinName">The name of the skin to be prepared.</param>
    /// <param name="themeName">The name of the theme for the specified skin to be prepared,
    /// or <c>null</c> for the default theme of the skin.</param>
    protected void PrepareSkinAndTheme(string skinName, string themeName)
    {
      // Release old resources
      _skinManager.ReleaseSkinResources();

      // Prepare new skin data
      Skin skin = _skinManager.Skins.ContainsKey(skinName) ? _skinManager.Skins[skinName] : null;
      if (skin == null)
        skin = _skinManager.DefaultSkin;
      if (skin == null)
        throw new Exception(string.Format("Skin '{0}' not found", skinName));
      Theme theme = themeName == null ? null :
          (skin.Themes.ContainsKey(themeName) ? skin.Themes[themeName] : null);
      if (theme == null)
        theme = skin.DefaultTheme;

      if (!skin.IsValid)
        throw new ArgumentException(string.Format("Skin '{0}' is invalid", skin.Name));
      if (theme != null)
        if (!theme.IsValid)
          throw new ArgumentException(string.Format("Theme '{0}' of skin '{1}' is invalid", theme.Name, skin.Name));

      SkinResources skinResources = theme == null ? skin : (SkinResources) theme;
      Fonts.FontManager.Load(skinResources);

      // Initialize SkinContext with new values
      SkinContext.SkinResources = skinResources;
      SkinContext.SkinName = skin.Name;
      SkinContext.ThemeName = theme == null ? null : theme.Name;
      SkinContext.SkinHeight = skin.NativeHeight;
      SkinContext.SkinWidth = skin.NativeWidth;

      _skin = skin;
      _theme = theme;
    }

    private void InternalShowDialog(string dialogName, bool isChild)
    {
      Screen newDialog = GetScreen(dialogName);
      if (newDialog == null)
      {
        ServiceScope.Get<ILogger>().Error("ScreenManager: Unable to show dialog {0}", dialogName);
        return;
      }

      if (_dialogStack.Count == 0)
        _currentScreen.DetachInput();
      else
        _dialogStack.Peek().DetachInput();

      newDialog.AttachInput();
      newDialog.Show();
      newDialog.ScreenState = Screen.State.Running;
      newDialog.IsChildDialog = isChild;
      _dialogStack.Push(newDialog);
    }

    private bool InternalCloseDialog()
    {
      // Do we have a dialog?
      if (_dialogStack.Count > 0)
      {
        Screen oldDialog = _dialogStack.Pop();

        oldDialog.ScreenState = Screen.State.Closing;
        oldDialog.DetachInput();
        oldDialog.Hide();

        // Is this the last dialog?
        if (_dialogStack.Count == 0)
        {
          _currentScreen.AttachInput();
        }
        else
        {
          _dialogStack.Peek().AttachInput();
        }
        return oldDialog.IsChildDialog;
      }
      return false;
    }

    protected void InternalCloseScreen()
    {
      if (_currentScreen == null)
        return;
      lock (_syncRoot)
      {
        _currentScreen.ScreenState = Screen.State.Closing;
        _currentScreen.DetachInput();
        _currentScreen.Hide();
        _currentScreen = null;
      }
    }

    protected void InternalCloseCurrentScreenAndDialogs()
    {
      // Close all dialogs
      for (int i = _dialogStack.Count; i > 0; i--)
      {
        InternalCloseDialog();
      }
      // Close the screen
      InternalCloseScreen();
    }

    protected bool InternalShowScreen(Screen screen)
    {
      lock (_syncRoot)
      {
        _currentScreen = screen;
        _currentScreen.ScreenState = Screen.State.Running;
        _currentScreen.AttachInput();
        _currentScreen.Show();
      }
      return true;
    }

    /// <summary>
    /// Renders the current window and dialog.
    /// </summary>
    public void Render()
    {
      SkinContext.Now = DateTime.Now;
      lock (_syncRoot)
      {
        if (_currentScreen == null)
          return;
        _currentScreen.Render();
        foreach (Screen dialog in _dialogStack)
          dialog.Render();
      }
    }

    /// <summary>
    /// Switches the active skin and theme. This method will set the skin with
    /// the specified <paramref name="newSkinName"/> and the theme belonging
    /// to this skin with the specified <paramref name="newThemeName"/>, or the
    /// default theme for this skin.
    /// </summary>
    public void SwitchSkinAndTheme(string newSkinName, string newThemeName)
    {
      if (newSkinName == _skin.Name &&
          newThemeName == (_theme == null ? null : _theme.Name)) return;

      lock (_syncRoot)
      {
        ServiceScope.Get<ILogger>().Info("ScreenManager: Switching to skin '{0}', theme '{1}'",
            newSkinName, newThemeName);

        string currentScreenName = _currentScreen == null ? null : _currentScreen.Name;

        InternalCloseCurrentScreenAndDialogs();

        // FIXME Albert78: Find a better way to make the InputManager, PlayerCollection and
        // ContentManager observe the current skin
        ServiceScope.Get<IInputManager>().Reset();
        ServiceScope.Get<IPlayerCollection>().Dispose();
        ContentManager.Clear();

        try
        {
          PrepareSkinAndTheme(newSkinName, newThemeName);
        }
        catch (Exception ex)
        {
          ServiceScope.Get<ILogger>().Error("ScreenManager: Error loading skin '{0}', theme '{1}'", ex, newSkinName, newThemeName);
          // Continue with old skin
          // TODO: Show error dialog
        }

        if (_skin.GetSkinFilePath(currentScreenName) != null)
          _currentScreen = GetScreen(currentScreenName);
        if (_currentScreen == null)
          _currentScreen = GetScreen(HOME_SCREEN);
        if (_currentScreen == null)
        { // The new skin is broken, so reset to default skin
          if (_skin == _skinManager.DefaultSkin)
              // We're already loading the default skin, it seems to be broken
            throw new Exception("The default skin seems to be broken, we don't have a fallback anymore");
          // Try it again with the default skin
          SwitchSkinAndTheme(SkinManager.DEFAULT_SKIN, null);
          return;
        }

        InternalShowScreen(_currentScreen);
      }
      SkinSettings settings = ServiceScope.Get<ISettingsManager>().Load<SkinSettings>();
      settings.Skin = SkinName;
      settings.Theme = ThemeName;
      ServiceScope.Get<ISettingsManager>().Save(settings);
    }

    /// <summary>
    /// Loads the specified screen from the current skin.
    /// </summary>
    /// <param name="screenName">The screen to load.</param>
    protected UIElement LoadSkinFile(string screenName)
    {
      return SkinContext.SkinResources.LoadSkinFile(screenName, new WorkflowManagerModelLoader()) as UIElement;
    }

    /// <summary>
    /// Gets the window displaying the screen with the specified name. If the window is
    /// already loaded, the cached window will be returned. Else, a new window instance
    /// will be created for the specified <paramref name="screenName"/> and loaded from
    /// a skin file. The window will not be shown yet.
    /// </summary>
    /// <param name="screenName">Name of the screen to return the window instance for.</param>
    /// <returns>screen or <c>null</c>, if an error occured while loading the window.</returns>
    public Screen GetScreen(string screenName)
    {
      try
      {
        // show waitcursor while loading a new window
        if (_currentScreen != null)
        {
          // TODO: Wait cursor
          //_currentScreen.WaitCursorVisible = true;
        }

        Screen result = new Screen(screenName);
        try
        {
          UIElement root = LoadSkinFile(screenName);
          if (root == null)
          {
            ServiceScope.Get<ILogger>().Error("ScreenManager: Cannot load screen '{0}'", screenName);
            return null;
          }
          result.Visual = root;
        }
        catch (Exception ex)
        {
          ServiceScope.Get<ILogger>().Error("ScreenManager: Error loading skin file for screen '{0}'", ex, screenName);
          // TODO Albert78: Show error dialog with skin loading message
          return null;
        }
        return result;
      }
      finally
      {
        // hide the waitcursor again
        if (_currentScreen != null)
        {
          // TODO: Wait cursor
          //_currentScreen.WaitCursorVisible = false;
        }
      }
    }

    public void SwitchTheme(string newThemeName)
    {
      SwitchSkinAndTheme(SkinContext.SkinName, newThemeName);
    }

    public void SwitchSkin(string newSkinName)
    {
      SwitchSkinAndTheme(newSkinName, null);
    }

    public string SkinName
    {
      get { return _skin.Name; }
    }

    public string ThemeName
    {
      get { return _theme == null ? null : _theme.Name; }
    }

    public string CurrentScreenName
    {
      get { return IsDialogVisible ? _dialogStack.Peek().Name : _currentScreen.Name; }
    }

    public bool IsDialogVisible
    {
      get { return _dialogStack.Count > 0; }
    }

    public void Reset()
    {
      // Reset all dialogs
      foreach (Screen dialog in _dialogStack)
        dialog.Reset();

      // Reset the screen
      if (_currentScreen != null)
        _currentScreen.Reset();
    }

    public void Exit()
    {
      // Deallocate all dialogs
      foreach (Screen dialog in _dialogStack)
        dialog.Deallocate();

      // Deallocate the screen
      if (_currentScreen != null)
        _currentScreen.Deallocate();
      Fonts.FontManager.Unload();
    }

    public void CloseDialog()
    {
      ServiceScope.Get<ILogger>().Debug("ScreenManager: CloseDialog");
      lock (_syncRoot)
      {
        // Close the children and the main dialog
        do
        {
        } while (InternalCloseDialog());
      }
    }

    public void ShowDialog(string dialogName)
    {
      ServiceScope.Get<ILogger>().Debug("ScreenManager: Showing dialog '{0}'...", dialogName);
      lock (_syncRoot)
      {
        InternalShowDialog(dialogName, false);
      }
    }

    public void ShowChildDialog(string dialogName)
    {
      ServiceScope.Get<ILogger>().Debug("ScreenManager: Showing child dialog '{0}'...", dialogName);
      lock (_syncRoot)
      {
        InternalShowDialog(dialogName, true);
      }
    }

    /// <summary>
    /// Reloads the current window.
    /// </summary>
    public void Reload()
    {
      InternalCloseCurrentScreenAndDialogs();

      Screen currentScreen;
      lock (_syncRoot)
      {
        string name = _currentScreen.Name;
        currentScreen = GetScreen(name);
      }
      if (currentScreen == null)
          // Error message was shown in GetScreen()
        return;
      InternalShowScreen(currentScreen);
    }

    public bool PrepareScreen(string windowName)
    {
      return GetScreen(windowName) != null;
    }

    public bool ShowScreen(string windowName)
    {
      ServiceScope.Get<ILogger>().Debug("ScreenManager: Showing screen '{0}'...", windowName);
      Screen newScreen = GetScreen(windowName);
      if (newScreen == null)
          // Error message was shown in GetScreen()
        return false;

      lock (_syncRoot)
      {
        InternalCloseCurrentScreenAndDialogs();

        return InternalShowScreen(newScreen);
      }
    }

    [Obsolete("This method will be replaced by a generic approach in the future")]
    public void SetDialogResponse(string response)
    {
      if (response.ToLower() == "yes")
        _dialogResponse = true;
      else
        _dialogResponse = false;

      CloseDialog();
    }

    [Obsolete("This method will be replaced by a generic approach in the future")]
    public bool GetDialogResponse()
    {
      return _dialogResponse;
    }

    [Obsolete("This method will be replaced by a generic approach in the future")]
    public string DialogTitle
    {
      get
      {
        return _dialogTitle;
      }
      set
      {
        _dialogTitle = value;
      }
    }

    [Obsolete("This method will be replaced by a generic approach in the future")]
    public string DialogLine1
    {
      get
      {
        return _dialogLines[0];
      }
      set
      {
        _dialogLines[0] = value;
      }
    }

    [Obsolete("This method will be replaced by a generic approach in the future")]
    public string DialogLine2
    {
      get
      {
        return _dialogLines[1];
      }
      set
      {
        _dialogLines[1] = value;
      }
    }

    [Obsolete("This method will be replaced by a generic approach in the future")]
    public string DialogLine3
    {
      get
      {
        return _dialogLines[2];
      }
      set
      {
        _dialogLines[2] = value;
      }
    }
  }
}