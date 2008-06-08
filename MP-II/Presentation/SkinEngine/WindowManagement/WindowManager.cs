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
using System.IO;
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
    private Window _currentWindow;
    private Window _currentDialog;
    public Utils _utils = new Utils();

    private string _dialogTitle;
    private string[] _dialogLines = new string[3];
    private bool _dialogResponse;  // Yes = true, No = false

    #endregion

    public void ShowStartupScreen()
    {
      ShowWindow(STARTUP_SCREEN);
    }

    public void SwitchTheme(string newThemeName)
    {
      SwitchSkinAndTheme(SkinContext.Skin.Name, newThemeName);
    }

    public void SwitchSkin(string newSkinName)
    {
      SwitchSkinAndTheme(newSkinName, null);
    }

    public void SwitchSkinAndTheme(string newSkinName, string newThemeName)
    {
      if (newSkinName == SkinName && newThemeName == ThemeName) return;
      CloseDialog();
      lock (_history)
      {
        ServiceScope.Get<ILogger>().Info("WindowManager: Switch to skin '{0}'", newSkinName);
        string windowName = _currentWindow.Name;
        _currentDialog = null;
        _currentWindow = null;

        _windowCache.Clear();

        IInputManager inputMgr = ServiceScope.Get<IInputManager>();
        inputMgr.Reset();
        ServiceScope.Get<PlayerCollection>().Dispose();
        Fonts.FontManager.Free();
        ContentManager.Clear();

        SkinContext.PrepareSkinAndTheme(newSkinName, newThemeName);
        SkinContext.ActivateTheme();

        Fonts.FontManager.Reload();
        Fonts.FontManager.Alloc();

        // If the new skin is not "compatible" with the new skin (which means that
        // the screen names do not match), we need to tidy up the history. We will
        // simply remove all screens from the history which are not present in
        // the new skin. At least the first screen (the home screen) will hopefully remain,
        // if the new skin isn't broken.
        string[] oldScreens = _history.ToArray();
        _history.Clear();
        for (int i = oldScreens.Length - 1; i >= 0; i-- )
        {
          string screenName = oldScreens[i];
          FileInfo skinFile = SkinContext.Skin.GetSkinFile(screenName);
          if (skinFile != null)
            _history.Push(screenName);
        }
        if (SkinContext.Skin.GetSkinFile(windowName) == null && _history.Count > 0)
          windowName = _history.Peek();
        if (windowName == null)
          // This should never occur because our old history always should have contained
          // at least the home screen, which should be present in all skins.
          // But we never know. This is our last fallback.
          windowName = STARTUP_SCREEN;

        _currentWindow = GetWindow(windowName);
        _currentWindow.HasFocus = true;
        _currentWindow.WindowState = Window.State.Running;
        _currentWindow.AttachInput();
        _currentWindow.Show();

      }
      WindowSettings settings = new WindowSettings();
      ServiceScope.Get<ISettingsManager>().Load(settings);
      settings.Skin = SkinContext.Skin.Name;
      Theme theme = SkinContext.Theme;
      settings.Theme = theme == null ? null : theme.Name;
      ServiceScope.Get<ISettingsManager>().Save(settings);
    }

    /// <summary>
    /// Gets the name of the currently active skin.
    /// </summary>
    public string SkinName
    {
      get { return SkinContext.Skin == null ? null : SkinContext.Skin.Name; }
    }

    /// <summary>
    /// Gets the name of the currently active theme.
    /// </summary>
    public string ThemeName
    {
      get { return SkinContext.Theme == null ? null : SkinContext.Theme.Name; }
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

    /// <summary>
    /// Renders the current window
    /// </summary>
    public void Render()
    {
      Utils.Update();
      SkinContext.Now = DateTime.Now;
      lock (_history)
      {
        lock (_windowCache)
        {
          if (_currentWindow != null)
          {
            _currentWindow.Render();
          }
          if (_currentDialog != null)
          {
            _currentDialog.Render();
          }

        }
      }
    }

    public void Reset()
    {
      if (_currentDialog != null)
        _currentDialog.Reset();
      _currentWindow.Reset();
    }

    /// <summary>
    /// Determines wether the windowmanager cache contains a window with the specified name.
    /// </summary>
    /// <param name="windowName">Name of the window to check.</param>
    /// <returns>
    /// <c>true</c> if window exists; otherwise, <c>false</c>.
    /// </returns>
    public bool Contains(string windowName)
    {
      return _windowCache.ContainsKey(windowName);
    }

    /// <summary>
    /// Gets the window with the specified name. If the window is already load, the cached window
    /// will be returned. Else, a new window instance will be created and load from the XAML resource
    /// specified by the <paramref name="windowName"/>.
    /// </summary>
    /// <param name="windowName">Name of the window to return.</param>
    /// <returns></returns>
    public Window GetWindow(string windowName)
    {
      try
      {
        // show waitcursor while loading a new window
        if (_currentWindow != null)
        {   
          // TODO: Wait cursor
          //_currentWindow.WaitCursorVisible = true;
        }

        if (Contains(windowName))
          return _windowCache[windowName];

        Window result = new Window(windowName);
        try
        {
          UIElement root = SkinContext.Skin.LoadSkinFile(windowName) as UIElement;
          if (root == null) return null;
          result.Visual = root;
          // Don't show window here.
          // That is done at the appriopriate time by all methods calling this one.
          // Calling show here will result in the model loading its data twice.
          _windowCache.Add(windowName, result);
        }
        catch (Exception ex)
        {
          ServiceScope.Get<ILogger>().Error("XamlLoader: Error loading skin file for window '{0}'", ex, windowName);
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

    /// <summary>
    /// Closes the dialog.
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

        _currentWindow.AttachInput();
        _currentWindow.Show();
      }
    }

    /// <summary>
    /// opens a dialog
    /// </summary>
    /// <param name="window">The window.</param>
    public void ShowDialog(string window)
    {
      ServiceScope.Get<ILogger>().Debug("WindowManager: Show dialog: {0}", window);
      CloseDialog();
      _currentDialog = GetWindow(window);
      if (_currentDialog == null)
      {
        return;
      }
      _currentWindow.DetachInput();

      _currentDialog.AttachInput();
      _currentDialog.Show();
      _currentDialog.WindowState = Window.State.Running;
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

      lock (_windowCache)
      {
        string name = _currentWindow.Name;
        _currentWindow = null;
        if (_windowCache.ContainsKey(name))
          _windowCache.Remove(name);
        _currentWindow = GetWindow(name);
      }
      if (_currentWindow == null)
        return;
      _currentWindow.WindowState = Window.State.Running;
      _currentWindow.AttachInput();
      _currentWindow.Show();
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
      Window window = GetWindow(windowName);
      if (window == null)
      {
        //TODO: Show error screen
        return;
      }

      lock (_history)
      {
        if (window.History)
        {
          _history.Push(window.Name);
        }

        if (_currentDialog != null)
        {
          _currentDialog.WindowState = Window.State.Closing;
          _currentDialog.DetachInput();
          _currentDialog.Hide();
          _currentDialog = null;
        }
        Window previousWindow = _currentWindow;
        if (previousWindow != null)
        {
          previousWindow.WindowState = Window.State.Closing;
          previousWindow.HasFocus = false;
          previousWindow.DetachInput();
        }
        _currentWindow = window;
        _currentWindow.WindowState = Window.State.Running;
        _currentWindow.AttachInput();
        _currentWindow.Show();

        if (previousWindow != null)
          previousWindow.Hide();
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
        Window previousWindow = _currentWindow;
        if (previousWindow != null)
        {
          previousWindow.WindowState = Window.State.Closing;
          previousWindow.DetachInput();
        }
        if (_currentWindow.History)
          _history.Pop();

        string windowName = _history.Peek();
        _currentWindow = GetWindow(windowName);
        _currentWindow.WindowState = Window.State.Running;
        _currentWindow.AttachInput();
        _currentWindow.Show();

        if (previousWindow != null)
          previousWindow.Hide();
      }
    }

    public Utils Utils
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
