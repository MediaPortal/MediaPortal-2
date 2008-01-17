#define TESTXAML
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
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Drawing;
using SkinEngine.Controls.Visuals;
using MediaPortal.Core;
using MediaPortal.Core.Logging;
using MediaPortal.Core.Players;
using MediaPortal.Core.InputManager;
using MediaPortal.Core.WindowManager;
using MediaPortal.Core.Settings;

using SkinEngine.Skin;

namespace SkinEngine
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
    private Window _previousWindow = null;
    private List<Window> _history = new List<Window>();
    private ISkinLoader _skinLoader;
    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="WindowManager"/> class.
    /// </summary>
    public WindowManager()
    {
      _skinLoader = new Loader();
      _windows = new List<Window>();
      ServiceScope.Get<IPlayerFactory>().Register(new SkinEngine.Players.PlayerFactory());
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WindowManager"/> class.
    /// </summary>
    /// <param name="loader">The loader.</param>
    public WindowManager(ISkinLoader loader)
    {
      if (loader == null)
      {
        throw new ArgumentNullException("loader");
      }
      _skinLoader = loader;
      _windows = new List<Window>();
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
      SkinContext.ThemeName = settings.Theme;
#if TESTXAML
      ShowWindow("movies");
#else

      PrepareWindow("homevista");
      ShowWindow("homevista");
#endif
    }

    public void SwitchTheme(string newThemeName)
    {
      if (newThemeName == SkinContext.ThemeName) return;
      CloseDialog();
      lock (_history)
      {
        ServiceScope.Get<ILogger>().Info("WindowManager:Switch to theme:{0}", newThemeName);
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
    /// <summary>
    /// Switches to the skin specified.
    /// </summary>
    /// <param name="newSkinName">name of the skin.</param>
    public void SwitchSkin(string newSkinName)
    {
      if (newSkinName == SkinContext.SkinName) return;
      CloseDialog();
      lock (_history)
      {
        ServiceScope.Get<ILogger>().Info("WindowManager:Switch to skin:{0}", newSkinName);
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
      get { return _currentWindow; }
    }

    /// <summary>
    /// Gets the skin loader.
    /// </summary>
    /// <value>The skin loader.</value>
    public ISkinLoader SkinLoader
    {
      get { return _skinLoader; }
    }

    /// <summary>
    /// Renders the current window
    /// </summary>
    public void Render()
    {
      lock (_history)
      {
        SkinContext.Now = DateTime.Now;
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
    /// Gets the window with the specified name.
    /// </summary>
    /// <param name="windowName">Name of the window.</param>
    /// <returns></returns>
    public Window GetWindow(string windowName)
    {
      try
      {
        //show waitcursor while loading a new window
        if (_currentWindow != null)
        {
          _currentWindow.WaitCursorVisible = true;
        }
        foreach (Window window in _windows)
        {
          if (window.Name == windowName)
          {
            return window;
          }
        }
#if TESTXAML
        Window win = new Window(windowName);
        XamlLoader loader = new XamlLoader();
        UIElement root = loader.Load(windowName + ".xaml") as UIElement;
        if (root == null) return null;
        win.Visual = root;
#else
        Window win = new Window(windowName);
        _skinLoader.Load(win, windowName + ".xml");
#endif
        //Don't show window here.
        //That is done at the appriopriate time by all methods calling this one.
        //Calling show here will result in the model loading its data twice
        _windows.Add(win);
        return win;
      }
      finally
      {
        //hide the waitcursor again
        if (_currentWindow != null)
        {
          _currentWindow.WaitCursorVisible = false;
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
        _currentDialog.WindowState = Window.State.Closing;
        _currentDialog.HasFocus = false;
        _currentDialog.Hide();
        while (_currentDialog != null && _currentDialog.IsAnimating && SkinContext.IsRendering)
        {
          Thread.Sleep(10);
        }
        _currentDialog = null;
        _currentWindow.Show(false);
        _currentWindow.HasFocus = true;
      }
    }

    /// <summary>
    /// opens a dialog
    /// </summary>
    /// <param name="window">The window.</param>
    public void ShowDialog(string window)
    {
      ServiceScope.Get<ILogger>().Debug("WindowManager:Show dialog:{0}", window);
      CloseDialog();
      _currentDialog = GetWindow(window);
      if (_currentDialog == null)
      {
        return;
      }
      // _currentWindow.HasFocus = false;
      _currentWindow.Hide();

      _currentDialog.Show(true);
      _currentDialog.HasFocus = true;
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
      _currentWindow.HasFocus = true;
      _currentWindow.WindowState = Window.State.Running;
      _currentWindow.Show(true);
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
      ServiceScope.Get<ILogger>().Debug("WindowManager:Show window:{0}", windowName);
      Window window = GetWindow(windowName);
      if (window == null)
      {
        return;
      }
      if (window.History)
      {
        _history.Add(window);
      }
      
#if TESTXAML
      lock (_history)
      {
#endif
        if (_currentDialog != null)
        {
          _currentDialog.WindowState = Window.State.Closing;
          _currentDialog.HasFocus = false;
          _currentDialog.Hide();
          while (_currentDialog.IsAnimating && SkinContext.IsRendering)
          {
            Thread.Sleep(10);
          }
        }
        _previousWindow = _currentWindow;
        if (_previousWindow != null)
        {
          _previousWindow.WindowState = Window.State.Closing;
          _previousWindow.HasFocus = false;
          _previousWindow.Hide();
        }
        _currentWindow = window;
        _currentWindow.HasFocus = true;
        _currentWindow.WindowState = Window.State.Running;
        _currentWindow.Show(true);
        _currentDialog = null;
        DateTime dt = DateTime.Now;
        while (SkinContext.IsRendering)
        {
          bool isAnimating = false;
          if (_currentWindow.IsAnimating)
          {
            isAnimating = true;
          }
          if (_previousWindow != null && _previousWindow.IsAnimating)
          {
            isAnimating = true;
          }
          if (!isAnimating)
          {
            break;
          }
          Thread.Sleep(10);
          TimeSpan ts = DateTime.Now - dt;
          if (ts.TotalSeconds >= 2)
          {
            Trace.WriteLine("animation timeout after :" + ts.TotalSeconds.ToString());
            if (_previousWindow != null && _previousWindow.IsAnimating)
              Trace.WriteLine("  prev window still animating:" + _previousWindow.Name);
            if (_currentWindow != null && _currentWindow.IsAnimating)
              Trace.WriteLine("  cur window still animating:" + _currentWindow.Name);
            break;
          }
        }
        _previousWindow = null;

        if (_currentWindow != null && _currentWindow.Name == "login")
        {
          Thread tStart = new Thread(new ThreadStart(ShowHomeMenu));
          tStart.Start();
        }

#if TESTXAML
      }
#endif
    }
    void ShowHomeMenu()
    {
      while (_currentWindow.IsAnimating) Thread.Sleep(10);

      _previousWindow = _currentWindow;

      _previousWindow.WindowState = Window.State.Closing;
      _previousWindow.HasFocus = false;
      _previousWindow.Hide();



      _currentWindow = GetWindow("homeVista");
      _history.Add(_currentWindow);
      _currentWindow.HasFocus = true;
      _currentWindow.WindowState = Window.State.Running;
      _currentWindow.Show(true);
    }

    /// <summary>
    /// Shows the previous window.
    /// </summary>
    public void ShowPreviousWindow()
    {
      if (_history.Count == 0)
      {
        return;
      }
      ServiceScope.Get<ILogger>().Debug("WindowManager:Show previous window");
      Window window = _history[_history.Count - 1];
      if (_currentDialog != null)
      {
        CloseDialog();
        return;
      }
      
#if TESTXAML
#else
      if (_history.Count <= 1)
      {
        return;
      }
#endif
      _previousWindow = _currentWindow;
      if (_previousWindow != null)
      {
        _previousWindow.WindowState = Window.State.Closing;
        _previousWindow.HasFocus = false;
        _previousWindow.Hide();
      }
      if (_currentWindow.History)
      {
        _history.RemoveAt(_history.Count - 1);
      }
      _currentWindow = _history[_history.Count - 1];
      _currentWindow.HasFocus = true;
      _currentWindow.WindowState = Window.State.Running;
      _currentWindow.Show(true);

      DateTime dt = DateTime.Now;
      while (SkinContext.IsRendering)
      {
        bool isAnimating = false;
        if (_currentWindow.IsAnimating)
        {
          isAnimating = true;
        }
        if (_previousWindow != null && _previousWindow.IsAnimating)
        {
          isAnimating = true;
        }
        if (!isAnimating)
        {
          break;
        }
        Thread.Sleep(10);
        TimeSpan ts = DateTime.Now - dt;
        if (ts.TotalSeconds >= 2)
        {
          Trace.WriteLine("animation timeout after :" + ts.TotalSeconds.ToString());
          if (_previousWindow != null && _previousWindow.IsAnimating)
            Trace.WriteLine("  prev window still animating:" + _previousWindow.Name);
          if (_currentWindow != null && _currentWindow.IsAnimating)
            Trace.WriteLine("  cur window still animating:" + _currentWindow.Name);
          break;
        }
      }
      _previousWindow = null;
    }


  }
}
