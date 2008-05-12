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
using System.Runtime.InteropServices;
using System.Drawing;
using Presentation.SkinEngine.Controls.Visuals;
using MediaPortal.Core;
using MediaPortal.Core.Logging;
using MediaPortal.Presentation.Players;
using MediaPortal.Presentation.WindowManager;
using MediaPortal.Core.Settings;

using Presentation.SkinEngine.Loader;

namespace Presentation.SkinEngine
{
  public class WindowManager : IWindowManager
  {
    [StructLayout(LayoutKind.Sequential)]
    public struct MSG
    {
      public IntPtr hwnd;
      public uint message;
      public IntPtr wParam;
      public IntPtr lParam;
      public uint time;
      Point P;
    }

    #region variables
    [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
    public static extern int MsgWaitForMultipleObjects(int nCount, int pHandles, bool fWaitAll, int dwMilliseconds, int dwWakeMask);

    [DllImport("user32.dll")]
    static extern bool PeekMessage(out MSG lpMsg, IntPtr hWnd, uint MsgFilterMin, uint wMsgFilterMax, uint wRemoveMsg);

    [DllImport("user32.dll")]
    static extern bool TranslateMessage([In] ref MSG lpMsg);

    [DllImport("user32.dll")]
    static extern IntPtr DispatchMessage([In] ref MSG lpmsg);


    private List<Window> _windows;
    private Window _currentWindow;
    private Window _currentDialog;
    private Window _previousWindow = null; // FIXME Albert78: Remove this reference - not needed
    private List<Window> _history = new List<Window>();
    public Utils _utils = new Utils();

    private string _dialogTitle;
    private string[] _dialogLines = new string[3];
    private bool _dialogResponse;  // Yes = true, No = false
    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="WindowManager"/> class.
    /// </summary>
    public WindowManager()
    {
      _windows = new List<Window>();
      ServiceScope.Get<IPlayerFactory>().Register(new SkinEngine.Players.PlayerFactory());
    }

    /// <summary>
    /// Loads the skin.
    /// </summary>
    public void LoadSkin()
    {
      WindowSettings settings = new WindowSettings();
      ServiceScope.Get<ISettingsManager>().Load(settings);
      if (settings.Skin == "")
      {
        settings.Skin = "default";
        settings.Theme = "default";
        ServiceScope.Get<ISettingsManager>().Save(settings);
      }
      SkinContext.SkinName = settings.Skin;
      //SkinContext.ThemeName = settings.Theme;

      ShowWindow("home");
    }

    public void SwitchTheme(string newThemeName)
    {
    }

    public string ThemeName
    {
      get
      {
        return "";
      }
    }
#if NOTDEF
    public void SwitchTheme(string newThemeName)
    {
      if (newThemeName == SkinContext.ThemeName) return;
      CloseDialog();
      lock (_history)
      {
        ServiceScope.Get<ILogger>().Info("WindowManager: Switch to theme: {0}", newThemeName);
        string windowName = _currentWindow.Name;
        _currentDialog = null;
        _currentWindow = null;
        _previousWindow = null;

        _windows.Clear();

        IInputManager inputMgr = ServiceScope.Get<IInputManager>();
        inputMgr.Reset();
        ServiceScope.Get<PlayerCollection>().Dispose();
        Scripts.ScriptManager.Instance.Reload();
        Fonts.FontManager.Free();
        ContentManager.Clear();

        SkinContext.ThemeName = newThemeName;
        Fonts.FontManager.Reload();
        Fonts.FontManager.Alloc();

        for (int i = 0; i < _history.Count; ++i)
        {
          _history[i] = GetWindow(_history[i].Name);
        }

        _currentWindow = GetWindow(windowName);
        _currentWindow.HasFocus = true;
        _currentWindow.WindowState = Window.State.Running;
        _currentWindow.Show(true);

      }
      WindowSettings settings = new WindowSettings();
      ServiceScope.Get<ISettingsManager>().Load(settings);
      settings.Theme = newThemeName;
      ServiceScope.Get<ISettingsManager>().Save(settings);
    }

    public string ThemeName
    {
      get
      {
        return SkinContext.ThemeName;
      }
    }
#endif
    /// <summary>
    /// Switches to the skin specified.
    /// </summary>
    /// <param name="newSkinName">name of the skin.</param>
    public void SwitchSkin(string newSkinName)
    {
#if NOTUSED
      if (newSkinName == SkinContext.SkinName) return;
      CloseDialog();
      lock (_history)
      {
        ServiceScope.Get<ILogger>().Info("WindowManager: Switch to skin: {0}", newSkinName);
        string windowName = _currentWindow.Name;
        _currentDialog = null;
        _currentWindow = null;
        _previousWindow = null;

        _windows.Clear();

        IInputManager inputMgr = ServiceScope.Get<IInputManager>();
        inputMgr.Reset();
        ServiceScope.Get<PlayerCollection>().Dispose();
        Scripts.ScriptManager.Instance.Reload();
        Fonts.FontManager.Free();
        ContentManager.Clear();

        SkinContext.SkinName = newSkinName;
        SkinContext.ThemeName = "default";
        ThemeLoader loader = new ThemeLoader();
        SkinContext.Theme = loader.Load(SkinContext.ThemeName);
        Fonts.FontManager.Reload();
        Fonts.FontManager.Alloc();

        for (int i = 0; i < _history.Count; ++i)
        {
          _history[i] = GetWindow(_history[i].Name);
        }

        _currentWindow = GetWindow(windowName);
        _currentWindow.HasFocus = true;
        _currentWindow.WindowState = Window.State.Running;
        _currentWindow.Show(true);

      }
      WindowSettings settings = new WindowSettings();
      ServiceScope.Get<ISettingsManager>().Load(settings);
      settings.Skin = newSkinName;
      settings.Theme = "default";
      ServiceScope.Get<ISettingsManager>().Save(settings);
#endif
    }

    /// <summary>
    /// Gets the name of the skin.
    /// </summary>
    /// <value>The name of the skin.</value>
    public string SkinName
    {
      get
      {
        return SkinContext.SkinName;
      }
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
        lock (_windows)
        {
          if (_previousWindow != null)
          {
            _previousWindow.Render();
          }
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
    /// Determines wether the windowmanager contains a window with the specified name
    /// </summary>
    /// <param name="windowName">Name of the window.</param>
    /// <returns>
    /// 	<c>true</c> if window exists; otherwise, <c>false</c>.
    /// </returns>
    public bool Contains(string windowName)
    {
      foreach (Window window in _windows)
      {
        if (window.Name == windowName)
        {
          return true;
        }
      }
      return false;
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
        foreach (Window window in _windows)
        {
          if (window.Name == windowName)
          {
            return window;
          }
        }
        Window win = new Window(windowName);
        try
        {
          XamlLoader loader = new XamlLoader();
          UIElement root = loader.Load(windowName + ".xaml") as UIElement;
          if (root == null) return null;
          win.Visual = root;
          // Don't show window here.
          // That is done at the appriopriate time by all methods calling this one.
          // Calling show here will result in the model loading its data twice.
          _windows.Add(win);
        }
        catch (Exception ex)
        {
          ServiceScope.Get<ILogger>().Error("XamlLoader: Error loading skin file for window '{0}'", ex, windowName);
          // TODO Albert78: Show error dialog with skin loading message
          return null;
        }
        return win;
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
      if (_currentDialog != null)
      {
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

      lock (_windows)
      {
        string name = _currentWindow.Name;
        _currentWindow = null;
        foreach (Window window in _windows)
        {
          if (window.Name == name)
          {
            _windows.Remove(window);
            break;
          }
        }
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
    /// Shows the window with the specified name
    /// </summary>
    /// <param name="windowName">Name of the window.</param>
    public void ShowWindow(string windowName)
    {
      ServiceScope.Get<ILogger>().Debug("WindowManager: Show window: {0}", windowName);
      Window window = GetWindow(windowName);
      if (window == null)
      {
        return;
      }
      if (window.History)
      {
        _history.Add(window);
      }

      lock (_history)
      {
        if (_currentDialog != null)
        {
          _currentDialog.WindowState = Window.State.Closing;
          _currentDialog.DetachInput();
          _currentDialog.Hide();
          _currentDialog = null;
        }
        _previousWindow = _currentWindow;
        if (_previousWindow != null)
        {
          _previousWindow.WindowState = Window.State.Closing;
          _previousWindow.HasFocus = false;
          _previousWindow.DetachInput();
        }
        _currentWindow = window;
        _currentWindow.WindowState = Window.State.Running;
        _currentWindow.AttachInput();
        _currentWindow.Show();

        if (_previousWindow != null)
          _previousWindow.Hide();
        _previousWindow = null;

        // Albert78, 23.3.08: Why this code?
        //if (_currentWindow != null && _currentWindow.Name == "login")
        //{
        //  Thread tStart = new Thread(new ThreadStart(ShowHomeMenu));
        //  tStart.Start();
        //}
      }
    }

    // Albert78, 23.3.08: See comment at the end of method ShowWindow. This method is nowhere else used than
    // in the region commented out there
    //void ShowHomeMenu()
    //{
    //  while (_currentWindow.IsAnimating) Thread.Sleep(10);

    //  _previousWindow = _currentWindow;

    //  _previousWindow.WindowState = Window.State.Closing;
    //  _previousWindow.HasFocus = false;
    //  _previousWindow.DetachInput();



    //  _currentWindow = GetWindow("home");
    //  _history.Add(_currentWindow);
    //  _currentWindow.WindowState = Window.State.Running;
    //  _currentWindow.AttachInput();
    //  _currentWindow.Show();
    //  _previousWindow.Hide();
    //}

    /// <summary>
    /// Shows the previous window.
    /// </summary>
    public void ShowPreviousWindow()
    {
      if (_history.Count == 0)
      {
        return;
      }
      lock (_history)
      {
        ServiceScope.Get<ILogger>().Debug("WindowManager: Show previous window");
        Window window = _history[_history.Count - 1];
        if (_currentDialog != null)
        {
          CloseDialog();
          return;
        }

        if (_history.Count <= 1)
        {
          return;
        }
        _previousWindow = _currentWindow;
        if (_previousWindow != null)
        {
          _previousWindow.WindowState = Window.State.Closing;
          _previousWindow.DetachInput();
        }
        if (_currentWindow.History)
        {
          _history.RemoveAt(_history.Count - 1);
        }
        _currentWindow = _history[_history.Count - 1];
        _currentWindow.WindowState = Window.State.Running;
        _currentWindow.AttachInput();
        _currentWindow.Show();

        if (_previousWindow != null)
          _previousWindow.Hide();
        _previousWindow = null;
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
