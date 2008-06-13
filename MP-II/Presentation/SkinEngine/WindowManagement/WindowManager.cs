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
using Presentation.SkinEngine.Controls.Visuals;
using MediaPortal.Core;
using MediaPortal.Core.Logging;
using MediaPortal.Presentation.Players;
using MediaPortal.Presentation.WindowManager;
using MediaPortal.Core.Settings;
using MediaPortal.Control.InputManager;
using Presentation.SkinEngine.SkinManagement;

namespace Presentation.SkinEngine
{
  public class WindowManager : IWindowManager
  {
    public const string STARTUP_SCREEN = "home";

    #region Variables

    private readonly Dictionary<string, Window> _windowCache = new Dictionary<string, Window>();
    private readonly Stack<string> _history = new Stack<string>();
    private Window _currentWindow = null;
    private Window _currentDialog = null;
    private Skin _skin = null;
    private Theme _theme = null;
    private static SkinManager _skinManager = new SkinManager();
    public TimeUtils _utils = new TimeUtils();

    private string _dialogTitle;
    private string[] _dialogLines = new string[3];
    private bool _dialogResponse;  // Yes = true, No = false

    #endregion

    public WindowManager()
    {
      WindowSettings windowSettings = new WindowSettings();
      ServiceScope.Get<ISettingsManager>().Load(windowSettings);

      string skinName = windowSettings.Skin;
      string themeName = windowSettings.Theme;
      if (string.IsNullOrEmpty(skinName))
      {
        skinName = SkinManager.DEFAULT_SKIN;
        themeName = null;
      }

      // Prepare the skin and theme - the theme will be activated in method MainForm_Load
      ServiceScope.Get<ILogger>().Debug("WindowManager: Loading skin '{0}', theme '{1}'", skinName, themeName);
      PrepareSkinAndTheme(skinName, themeName);

      // Update the settings with our current skin/theme values
      if (windowSettings.Skin != SkinName || windowSettings.Theme != ThemeName)
      {
        windowSettings.Skin = _skin.Name;
        windowSettings.Theme = _theme == null ? null : _theme.Name;
        ServiceScope.Get<ISettingsManager>().Save(windowSettings);
      }
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
      Skin skin = _skinManager.Skins.ContainsKey(skinName) ? _skinManager.Skins[skinName] : null;
      if (skin == null)
        skin = _skinManager.DefaultSkin;
      if (skin == null)
        throw new Exception(string.Format("Skin '{0}' not found", skinName));
      Theme theme = themeName == null ? null :
          (skin.Themes.ContainsKey(themeName) ? skin.Themes[themeName] : null);
      if (theme == null)
        theme = skin.DefaultTheme;

      // Initialize SkinContext with new values
      SkinContext.SkinResources = theme == null ? skin : (SkinResources) theme;
      SkinContext.SkinName = skin.Name;
      SkinContext.ThemeName = theme == null ? null : theme.Name;
      SkinContext.SkinHeight = skin.Height;
      SkinContext.SkinWidth = skin.Width;

      // Release old resources
      if (_skin != null && _skin != skin && _skin != _skinManager.DefaultSkin)
        _skin.Release();
      if (_theme != null && _theme != theme && _theme != _skinManager.DefaultSkin.DefaultTheme)
        _theme.Release();

      _skin = skin;
      _theme = theme;
    }

    protected void InternalCloseWindow()
    {
      if (_currentWindow == null)
        return;
      lock (_history)
      {
        _currentWindow.WindowState = Window.State.Closing;
        _currentWindow.HasFocus = false;
        _currentWindow.DetachInput();
        _currentWindow.Hide();
        _currentWindow = null;
      }
    }

    protected void InternalCloseCurrentWindows()
    {
      CloseDialog();
      InternalCloseWindow();
    }

    protected void InternalShowWindow(Window window)
    {
      CloseDialog();
      lock (_history)
      {
        _currentWindow = window;
        _currentWindow.HasFocus = true;
        _currentWindow.WindowState = Window.State.Running;
        _currentWindow.AttachInput();
        _currentWindow.Show();
      }
    }

    public void ShowStartupScreen()
    {
      ShowWindow(STARTUP_SCREEN);
    }

    /// <summary>
    /// Renders the current window and dialog.
    /// </summary>
    public void Render()
    {
      TimeUtils.Update();
      SkinContext.Now = DateTime.Now;
      lock (_history)
      {
        lock (_windowCache)
        {
          if (_currentWindow != null)
            _currentWindow.Render();
          if (_currentDialog != null)
            _currentDialog.Render();
        }
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

      lock (_history)
      {
        ServiceScope.Get<ILogger>().Info("WindowManager: Switching to skin '{0}', theme '{1}'",
            newSkinName, newThemeName);

        string currentScreenName = _currentWindow == null ? null : _currentWindow.Name;
        bool currentScreenInHistory = _currentWindow == null ? false : _currentWindow.History;

        InternalCloseCurrentWindows();

        _windowCache.Clear();

        IInputManager inputMgr = ServiceScope.Get<IInputManager>();
        inputMgr.Reset();
        // FIXME Albert78: Find a better way to make PlayerCollection observe the change of the
        // current skin, also InputManager, FontManager and ContentManager
        ServiceScope.Get<PlayerCollection>().Dispose();
        Fonts.FontManager.Free();
        ContentManager.Clear();

        PrepareSkinAndTheme(newSkinName, newThemeName);

        Fonts.FontManager.Reload();
        Fonts.FontManager.Alloc();

        // We will clear the history because we cannot guarantee that the screens in the
        // history will be compatible with the new skin.
        _history.Clear();
        _history.Push(STARTUP_SCREEN);
        if (currentScreenInHistory && _skin.GetSkinFile(currentScreenName) != null)
          _history.Push(currentScreenName);

        if (_currentWindow == null && _skin.GetSkinFile(currentScreenName) != null)
          _currentWindow = GetWindow(currentScreenName);
        if (_currentWindow == null)
          _currentWindow = GetWindow(_history.Peek());
        if (_currentWindow == null)
        {
          // The window is broken in the new skin
          if (_skin == _skinManager.DefaultSkin)
              // We're already loading the default skin, it seems to be broken
            throw new Exception("The default skin seems to be broken, we don't have a fallback anymore");
          // Try it again with the default skin
          SwitchSkinAndTheme(SkinManager.DEFAULT_SKIN, null);
          return;
        }

        InternalShowWindow(_currentWindow);
      }
      WindowSettings settings = new WindowSettings();
      ServiceScope.Get<ISettingsManager>().Load(settings);
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
      return SkinContext.SkinResources.LoadSkinFile(screenName) as UIElement;
    }

    /// <summary>
    /// Gets the window displaying the screen with the specified name. If the window is
    /// already loaded, the cached window will be returned. Else, a new window instance
    /// will be created for the specified <paramref name="screenName"/> and loaded from
    /// a skin file. The window will not be shown yet.
    /// </summary>
    /// <param name="screenName">Name of the screen to return the window instance for.</param>
    /// <returns>Window or <c>null</c>, if an error occured loading the window.</returns>
    public Window GetWindow(string screenName)
    {
      try
      {
        // show waitcursor while loading a new window
        if (_currentWindow != null)
        {
          // TODO: Wait cursor
          //_currentWindow.WaitCursorVisible = true;
        }

        if (_windowCache.ContainsKey(screenName))
          return _windowCache[screenName];

        Window result = new Window(screenName);
        try
        {
          UIElement root = LoadSkinFile(screenName);
          if (root == null) return null;
          result.Visual = root;
          _windowCache.Add(screenName, result);
        }
        catch (Exception ex)
        {
          ServiceScope.Get<ILogger>().Error("WindowManager: Error loading skin file for window '{0}'", ex, screenName);
          // TODO Albert78: Show error dialog with skin loading message
          return null;
        }
        return result;
      }
      finally
      {
        // hide the waitcursor again
        if (_currentWindow != null)
        {
          // TODO: Wait cursor
          //_currentWindow.WaitCursorVisible = false;
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

    /// <summary>
    /// Gets the name of the currently active skin.
    /// </summary>
    public string SkinName
    {
      get { return _skin.Name; }
    }

    /// <summary>
    /// Gets the name of the currently active theme.
    /// </summary>
    public string ThemeName
    {
      get { return _theme == null ? null : _theme.Name; }
    }

    /// <summary>
    /// Gets the current window.
    /// </summary>
    /// <value>The current window.</value>
    public IWindow CurrentWindow
    {
      get
      {
        if (_currentDialog != null)
          return _currentDialog;
        return _currentWindow;
      }
    }

    public void Reset()
    {
      if (_currentDialog != null)
        _currentDialog.Reset();
      if (_currentWindow != null)
      _currentWindow.Reset();
    }

    /// <summary>
    /// Closes the opened dialog, if one is open.
    /// </summary>
    public void CloseDialog()
    {
      if (_currentDialog == null)
        return;
      lock (_history)
      {
        _currentDialog.WindowState = Window.State.Closing;
        _currentDialog.DetachInput();
        _currentDialog.Hide();
        _currentDialog = null;

        if (_currentWindow != null)
        {
          _currentWindow.AttachInput();
          _currentWindow.Show();
        }
      }
    }

    /// <summary>
    /// Shows the dialog with the specified name.
    /// </summary>
    /// <param name="dialogName">The dialog name.</param>
    public void ShowDialog(string dialogName)
    {
      ServiceScope.Get<ILogger>().Debug("WindowManager: Show dialog: {0}", dialogName);
      CloseDialog();
      lock (_history)
      {
        _currentDialog = GetWindow(dialogName);
        if (_currentDialog == null)
        {
          return;
        }
        _currentWindow.DetachInput();

        _currentDialog.AttachInput();
        _currentDialog.Show();
        _currentDialog.WindowState = Window.State.Running;
      }

      while (_currentDialog != null)
      {
        System.Windows.Forms.Application.DoEvents();
        System.Threading.Thread.Sleep(10);
      }
    }

    /// <summary>
    /// Reloads the current window.
    /// </summary>
    public void Reload()
    {
      CloseDialog();
      InternalCloseWindow();

      Window currentWindow;
      lock (_windowCache)
      {
        string name = _currentWindow.Name;
        if (_windowCache.ContainsKey(name))
          _windowCache.Remove(name);
        currentWindow = GetWindow(name);
      }
      if (currentWindow == null)
        // Error message was shown in GetWindow()
        return;
      InternalShowWindow(currentWindow);
    }

    public void PrepareWindow(string windowName)
    {
      GetWindow(windowName);
    }

    /// <summary>
    /// Shows the window with the specified name.
    /// </summary>
    /// <param name="windowName">Name of the window.</param>
    public void ShowWindow(string windowName)
    {
      ServiceScope.Get<ILogger>().Debug("WindowManager: Show window: {0}", windowName);
      Window newWindow = GetWindow(windowName);
      if (newWindow == null)
        // Error message was shown in GetWindow()
        return;

      lock (_history)
      {
        if (newWindow.History)
        {
          _history.Push(newWindow.Name);
        }

        CloseDialog();
        InternalCloseWindow();
        InternalShowWindow(newWindow);
      }
    }

    /// <summary>
    /// Shows the previous window from the window history.
    /// </summary>
    public void ShowPreviousWindow()
    {
      lock (_history)
      {
        if (_history.Count == 0)
        {
          return;
        }
        ServiceScope.Get<ILogger>().Debug("WindowManager: Show previous window");
        if (_currentDialog != null)
        {
          CloseDialog();
          return;
        }

        if (_history.Count <= 1)
        {
          return;
        }

        if (_currentWindow.History)
          _history.Pop();
        InternalCloseWindow();

        Window newWindow = GetWindow(_history.Peek());
        if (newWindow == null)
          // Error message was shown in GetWindow()
          return;
        InternalShowWindow(newWindow);
      }
    }

    // FIXME Albert78: Move this, if needed, to an own service in ServiceScope
    public TimeUtils TimeUtils
    {
      get
      {
        return _utils;
      }
      set
      {
        _utils = value;
      }
    }

    /// <summary>
    /// Sets a Dialog Response
    /// </summary>
    /// <param name="response"></param>
    public void SetDialogResponse(string response)
    {
      if (response.ToLower() == "yes")
        _dialogResponse = true;
      else
        _dialogResponse = false;

      CloseDialog();
    }

    /// <summary>
    /// Gets the Dialog Response
    /// </summary>
    /// <returns></returns>
    public bool GetDialogResponse()
    {
      return _dialogResponse;
    }

    /// <summary>
    /// Gets / Sets the Dialopg Title
    /// </summary>
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

    /// <summary>
    /// Gets / Sets Dialog Line 1
    /// </summary>
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
    
    /// <summary>
    /// Gets / Sets Dialog Line 2
    /// </summary>
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

    /// <summary>
    /// Gets / Sets Dialog Line 3
    /// </summary>
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
