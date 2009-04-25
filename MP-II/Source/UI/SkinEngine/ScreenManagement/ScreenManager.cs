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
using MediaPortal.Presentation.Localization;
using MediaPortal.Presentation.Screens;
using MediaPortal.Presentation.SkinResources;
using MediaPortal.Presentation.Workflow;
using MediaPortal.SkinEngine.ContentManagement;
using MediaPortal.SkinEngine.Controls.Visuals;
using MediaPortal.Core;
using MediaPortal.Core.Logging;
using MediaPortal.Presentation.Players;
using MediaPortal.Core.Settings;
using MediaPortal.SkinEngine.Players;
using MediaPortal.SkinEngine.Settings;
using MediaPortal.SkinEngine.SkinManagement;
using MediaPortal.Utilities;

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

    #region Consts

    public const string HOME_SCREEN = "home";

    public const string ERROR_DIALOG_HEADER = "[Dialogs.ErrorHeaderText]";
    public const string ERROR_LOADING_SKIN_RESOURCE_TEXT = "[ScreenManager.ErrorLoadingSkinResource]";
    public const string SCREEN_MISSING_TEXT = "[ScreenManager.ScreenMissing]";
    public const string SCREEN_BROKEN_TEXT = "[ScreenManager.ScreenBroken]";
    public const string BACKGROUND_SCREEN_MISSING_TEXT = "[ScreenManager.BackgroundScreenMissing]";
    public const string BACKGROUND_SCREEN_BROKEN_TEXT = "[ScreenManager.BackgroundScreenBroken]";

    #endregion

    #region Protected fields

    protected readonly object _syncRoot = new object();
    protected Screen _backgroundLayer = null;
    protected Screen _currentScreen = null;
    protected readonly Stack<Screen> _dialogStack = new Stack<Screen>();

    protected readonly SkinManager _skinManager;
    protected readonly WorkflowManagerModelLoader _workflowManagerModelLoader;
    protected Skin _skin = null;
    protected Theme _theme = null;

    #endregion

    public ScreenManager()
    {
      SkinSettings screenSettings = ServiceScope.Get<ISettingsManager>().Load<SkinSettings>();
      _skinManager = new SkinManager();
      _workflowManagerModelLoader = new WorkflowManagerModelLoader();

      string skinName = screenSettings.Skin;
      string themeName = screenSettings.Theme;
      if (string.IsNullOrEmpty(skinName))
      {
        skinName = SkinManager.DEFAULT_SKIN;
        themeName = null;
      }

      // Prepare the skin and theme - the theme will be activated in method MainForm_Load
      ServiceScope.Get<ILogger>().Debug("ScreenManager: Loading skin '{0}', theme '{1}'", skinName, themeName);
      PrepareSkinAndTheme_NeedLock(skinName, themeName);

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
      InternalCloseCurrentScreenAndDialogs(true);
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
    protected void PrepareSkinAndTheme_NeedLock(string skinName, string themeName)
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

      _skinManager.InstallSkinResources(skinResources);

      _skin = skin;
      _theme = theme;
    }

    protected internal void InternalShowDialog(string dialogName, DialogCloseCallbackDlgt dialogCloseCallback)
    {
      Screen newDialog = GetScreen(dialogName);
      if (newDialog == null)
      {
        ServiceScope.Get<ILogger>().Error("ScreenManager: Unable to show dialog {0}", dialogName);
        return;
      }

      lock (_syncRoot)
      {
        if (_dialogStack.Count == 0)
        {
          if (_currentScreen != null)
            _currentScreen.DetachInput();
        }
        else
          _dialogStack.Peek().DetachInput();

        _dialogStack.Push(newDialog);

        if (dialogCloseCallback != null)
          newDialog.Closed += (sender, e) => dialogCloseCallback(dialogName);
        newDialog.AttachInput();
        newDialog.ScreenState = Screen.State.Running;
      }
      // Don't hold the lock while showing the screen
      newDialog.Show();
    }

    protected internal void InternalCloseDialog()
    {
      lock(_syncRoot)
      {
        // Do we have a dialog?
        if (_dialogStack.Count == 0)
          return;
        Screen oldDialog = _dialogStack.Pop();

        oldDialog.ScreenState = Screen.State.Closing;
        oldDialog.DetachInput();
        oldDialog.Hide();

        // Is this the last dialog?
        if (_dialogStack.Count == 0)
        {
          if (_currentScreen != null)
            _currentScreen.AttachInput();
        }
        else
          _dialogStack.Peek().AttachInput();
      }
    }

    protected internal void InternalCloseScreen()
    {
      Screen screen;
      lock (_syncRoot)
      {
        if (_currentScreen == null)
          return;
        screen = _currentScreen;
        _currentScreen = null;

        screen.ScreenState = Screen.State.Closing;
        screen.DetachInput();
      }
      // Don't hold the lock while hiding the screen
      screen.Hide();
    }

    protected internal void InternalCloseCurrentScreenAndDialogs(bool closeBackgroundLayer)
    {
      if (closeBackgroundLayer)
        InternalSetBackgroundLayer(null);
      while (true)
      {
        lock (_syncRoot)
          if (_dialogStack.Count == 0)
            break;
        InternalCloseDialog();
      }
      InternalCloseScreen();
    }

    protected internal void InternalShowScreen(Screen screen)
    {
      lock (_syncRoot)
        _currentScreen = screen;
      screen.ScreenState = Screen.State.Running;
      screen.AttachInput();
      screen.Show();
    }

    protected internal void InternalSetBackgroundLayer(Screen background)
    {
      Screen oldBackground = null;
      lock (_syncRoot)
      {
        if (_backgroundLayer != null)
        {
          _backgroundLayer.ScreenState = Screen.State.Closing;
          oldBackground = _backgroundLayer;
        }
      }
      if (oldBackground != null)
        oldBackground.Hide();
      lock (_syncRoot)
      {
        _backgroundLayer = background;
        if (_backgroundLayer != null)
          _backgroundLayer.ScreenState = Screen.State.Running;
      }
      if (background != null)
        background.Show();
    }

    public void InstallBackgroundManager()
    {
      Skin skin = _skin;
      // No locking here
      if (skin != null)
        skin.InstallBackgroundManager();
    }

    public void UninstallBackgroundManager()
    {
      Skin skin = _skin;
      // No locking here
      if (skin != null)
        skin.UninstallBackgroundManager();
    }

    /// <summary>
    /// Renders the current window and dialog.
    /// </summary>
    public void Render()
    {
      SkinContext.Now = DateTime.Now;
      IList<Screen> renderScreens = new List<Screen>();
      lock (_syncRoot)
      {
        if (_backgroundLayer != null)
          renderScreens.Add(_backgroundLayer);
        if (_currentScreen != null)
          renderScreens.Add(_currentScreen);
        Screen[] dialogs = _dialogStack.ToArray();
        Array.Reverse(dialogs);
        CollectionUtils.AddAll(renderScreens, dialogs);
      }
      foreach (Screen screen in renderScreens)
        screen.Render();
    }

    /// <summary>
    /// Switches the active skin and theme. This method will set the skin with
    /// the specified <paramref name="newSkinName"/> and the theme belonging
    /// to this skin with the specified <paramref name="newThemeName"/>, or the
    /// default theme for this skin.
    /// </summary>
    public void SwitchSkinAndTheme(string newSkinName, string newThemeName)
    {
      lock (_syncRoot)
      {
        if (newSkinName == _skin.Name &&
            newThemeName == (_theme == null ? null : _theme.Name)) return;
        ServiceScope.Get<ILogger>().Info("ScreenManager: Switching to skin '{0}', theme '{1}'",
            newSkinName, newThemeName);

        UninstallBackgroundManager();
        InternalSetBackgroundLayer(null);

        string currentScreenName = _currentScreen == null ? null : _currentScreen.Name;

        InternalCloseCurrentScreenAndDialogs(true);

        PlayersHelper.ReleaseGUIResources();

        // FIXME Albert78: Find a better way to make ContentManager observe the current skin
        ContentManager.Clear();

        try
        {
          PrepareSkinAndTheme_NeedLock(newSkinName, newThemeName);
        }
        catch (Exception ex)
        {
          ServiceScope.Get<ILogger>().Error("ScreenManager: Error loading skin '{0}', theme '{1}'", ex, newSkinName, newThemeName);
          // Continue with old skin
          // TODO: Show error dialog
        }
        PlayersHelper.ReallocGUIResources();

        InstallBackgroundManager();

        _currentScreen = GetScreen(currentScreenName);
        InternalShowScreen(_currentScreen);
      }
      SkinSettings settings = ServiceScope.Get<ISettingsManager>().Load<SkinSettings>();
      settings.Skin = SkinName;
      settings.Theme = ThemeName;
      ServiceScope.Get<ISettingsManager>().Save(settings);
    }

    /// <summary>
    /// Loads the root UI element for the specified screen from the current skin.
    /// </summary>
    /// <param name="screenName">The screen to load.</param>
    /// <returns>Root UI element for the specified screen.</returns>
    protected UIElement LoadRootElement(string screenName)
    {
      return SkinContext.SkinResources.LoadSkinFile(screenName, _workflowManagerModelLoader) as UIElement;
    }

    /// <summary>
    /// Loads the root UI element for the specified background from the current skin.
    /// </summary>
    /// <param name="backgroundName">Name of the background skinfile.</param>
    /// <returns>Root UI element for the specified background.</returns>
    protected UIElement LoadBackgroundElement(string backgroundName)
    {
      return SkinContext.SkinResources.GetBackground(backgroundName) as UIElement;
    }

    /// <summary>
    /// Gets the screen with the specified name.
    /// </summary>
    /// <param name="screenName">Name of the screen to return.</param>
    /// <returns>screen or <c>null</c>, if an error occured while loading the screen.</returns>
    public Screen GetScreen(string screenName)
    {
      Screen result = new Screen(screenName);
      try
      {
        UIElement root = LoadRootElement(screenName);
        if (root == null)
        {
          ServiceScope.Get<ILogger>().Error("ScreenManager: Cannot load screen '{0}'", screenName);
          ServiceScope.Get<IDialogManager>().ShowDialog(ERROR_LOADING_SKIN_RESOURCE_TEXT,
              LocalizationHelper.CreateResourceString(SCREEN_MISSING_TEXT).Evaluate(screenName),
              DialogType.OkDialog, false);
          return null;
        }
        result.Visual = root;
        return result;
      }
      catch (Exception ex)
      {
        ServiceScope.Get<ILogger>().Error("ScreenManager: Error loading skin file for screen '{0}'", ex, screenName);
        try
        {
          ServiceScope.Get<IDialogManager>().ShowDialog(ERROR_LOADING_SKIN_RESOURCE_TEXT,
              LocalizationHelper.CreateResourceString(SCREEN_BROKEN_TEXT).Evaluate(screenName),
              DialogType.OkDialog, false);
        }
        catch (Exception)
        {
          ServiceScope.Get<ILogger>().Error("ScreenManager: Error showing generic dialog for error message");
          return null;
        }
        return null;
      }
    }

    /// <summary>
    /// Gets the background screen with the specified name.
    /// </summary>
    /// <param name="screenName">Name of the background screen to return.</param>
    /// <returns>screen or <c>null</c>, if an error occured while loading the screen.</returns>
    public Screen GetBackground(string screenName)
    {
      Screen result = new Screen(screenName);
      try
      {
        UIElement root = LoadBackgroundElement(screenName);
        if (root == null)
        {
          ServiceScope.Get<ILogger>().Error("ScreenManager: Cannot load background screen '{0}'", screenName);
          ServiceScope.Get<IDialogManager>().ShowDialog(ERROR_LOADING_SKIN_RESOURCE_TEXT,
              LocalizationHelper.CreateResourceString(BACKGROUND_SCREEN_MISSING_TEXT).Evaluate(screenName),
              DialogType.OkDialog, false);
          return null;
        }
        result.Visual = root;
        return result;
      }
      catch (Exception ex)
      {
        ServiceScope.Get<ILogger>().Error("ScreenManager: Error loading skin file for background screen '{0}'", ex, screenName);
        try
        {
          ServiceScope.Get<IDialogManager>().ShowDialog(ERROR_LOADING_SKIN_RESOURCE_TEXT,
              LocalizationHelper.CreateResourceString(BACKGROUND_SCREEN_BROKEN_TEXT).Evaluate(screenName),
              DialogType.OkDialog, false);
        }
        catch (Exception)
        {
          ServiceScope.Get<ILogger>().Error("ScreenManager: Error showing generic dialog for error message");
          return null;
        }
        return null;
      }
    }

    public void Reset()
    {
      lock (_syncRoot)
      {
        foreach (Screen dialog in _dialogStack)
          dialog.Reset();
        if (_backgroundLayer != null)
          _backgroundLayer.Reset();
        if (_currentScreen != null)
          _currentScreen.Reset();
      }
    }

    public void Exit()
    {
      lock (_syncRoot)
      {
        foreach (Screen dialog in _dialogStack)
          dialog.Deallocate();
        if (_currentScreen != null)
          _currentScreen.Deallocate();
      }
      Fonts.FontManager.Unload();
    }

    public void SwitchTheme(string newThemeName)
    {
      SwitchSkinAndTheme(_skin.Name, newThemeName);
    }

    public void SwitchSkin(string newSkinName)
    {
      SwitchSkinAndTheme(newSkinName, null);
    }

    public string SkinName
    {
      get
      {
        lock (_syncRoot)
          return _skin.Name;
      }
    }

    public string ThemeName
    {
      get
      {
        lock (_syncRoot)
          return _theme == null ? null : _theme.Name;
      }
    }

    public string ActiveScreenName
    {
      get
      {
        lock (_syncRoot)
          return _dialogStack.Count > 0 ? _dialogStack.Peek().Name :
              (_currentScreen == null ? null : _currentScreen.Name);
      }
    }

    public string ActiveBackgroundScreenName
    {
      get
      {
        lock (_syncRoot)
          return _backgroundLayer == null ? null : _backgroundLayer.Name;
      }
    }

      public bool IsDialogVisible
    {
      get
      {
        lock (_syncRoot)
          return _dialogStack.Count > 0;
      }
    }

    public void SetBackgroundLayer(string backgroundName)
    {
      Screen background = GetBackground(backgroundName);
      if (background == null)
        // Error message was shown in GetScreen()
        return;
      InternalSetBackgroundLayer(background);
    }

    public void ShowDialog(string dialogName)
    {
      ShowDialog(dialogName, null);
    }

    public void ShowDialog(string dialogName, DialogCloseCallbackDlgt dialogCloseCallback)
    {
      ServiceScope.Get<ILogger>().Debug("ScreenManager: Showing dialog '{0}'...", dialogName);
      InternalShowDialog(dialogName, dialogCloseCallback);
    }

    public void CloseDialog()
    {
      ServiceScope.Get<ILogger>().Debug("ScreenManager: CloseDialog");
      InternalCloseDialog();
    }

    /// <summary>
    /// Reloads the current window.
    /// </summary>
    public void Reload()
    {
      InternalCloseCurrentScreenAndDialogs(true);

      Screen currentScreen;
      lock (_syncRoot)
      {
        if (_currentScreen == null)
          return;
        string name = _currentScreen.Name;
        currentScreen = GetScreen(name);
      }
      if (currentScreen == null)
          // Error message was shown in GetScreen()
        return;
      InternalShowScreen(currentScreen);
    }

    public bool ShowScreen(string screenName)
    {
      ServiceScope.Get<ILogger>().Debug("ScreenManager: Showing screen '{0}'...", screenName);
      Screen newScreen = GetScreen(screenName);
      if (newScreen == null)
          // Error message was shown in GetScreen()
        return false;

      InternalCloseCurrentScreenAndDialogs(false);

      InternalShowScreen(newScreen);
      return true;
    }
  }
}
