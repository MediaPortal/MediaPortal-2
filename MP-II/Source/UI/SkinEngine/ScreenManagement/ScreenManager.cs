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
using System.Threading;
using MediaPortal.Core.Messaging;
using MediaPortal.Core.PluginManager;
using MediaPortal.Core.Localization;
using MediaPortal.Presentation.Screens;
using MediaPortal.Presentation.SkinResources;
using MediaPortal.Presentation.Workflow;
using MediaPortal.SkinEngine.ContentManagement;
using MediaPortal.SkinEngine.Controls.Visuals;
using MediaPortal.Core;
using MediaPortal.Core.Logging;
using MediaPortal.Core.Settings;
using MediaPortal.SkinEngine.Players;
using MediaPortal.SkinEngine.Settings;
using MediaPortal.SkinEngine.SkinManagement;
using MediaPortal.Utilities;

namespace MediaPortal.SkinEngine.ScreenManagement
{
  public class ScreenManager : IScreenManager
  {
    #region Consts

    public const string HOME_SCREEN = "home";

    public const string ERROR_DIALOG_HEADER = "[Dialogs.ErrorHeaderText]";
    public const string ERROR_LOADING_SKIN_RESOURCE_TEXT = "[ScreenManager.ErrorLoadingSkinResource]";
    public const string SCREEN_MISSING_TEXT = "[ScreenManager.ScreenMissing]";
    public const string SCREEN_BROKEN_TEXT = "[ScreenManager.ScreenBroken]";
    public const string BACKGROUND_SCREEN_MISSING_TEXT = "[ScreenManager.BackgroundScreenMissing]";
    public const string BACKGROUND_SCREEN_BROKEN_TEXT = "[ScreenManager.BackgroundScreenBroken]";

    public const string MODELS_REGISTRATION_LOCATION = "/Models";

    #endregion

    #region Classes & Delegates

    protected delegate void ScreenExecutor(Screen screen);

    /// <summary>
    /// Model loader used in the loading process of normal screens. GUI models requested via this
    /// model loader are requested from the workflow manger and thus attached to the workflow manager's current context.
    /// </summary>
    protected class WorkflowManagerModelLoader : IModelLoader
    {
      public object GetOrLoadModel(Guid modelId)
      {
        return ServiceScope.Get<IWorkflowManager>().GetModel(modelId);
      }
    }

    /// <summary>
    /// This class manages the backbround screen and holds its dynamic model resources.
    /// </summary>
    protected class BackgroundData : IPluginItemStateTracker, IModelLoader
    {
      #region Protected fields

      protected ScreenManager _parent;
      protected IDictionary<Guid, object> _models = new Dictionary<Guid, object>();
      protected Screen _backgroundScreen = null;

      #endregion

      #region Ctor

      public BackgroundData(ScreenManager parent)
      {
        _parent = parent;
      }

      #endregion

      public Screen BackgroundScreen
      {
        get { return _backgroundScreen; }
      }

      public bool Load(string backgroundName)
      {
        Unload();
        Screen background = GetBackground(backgroundName, this);
        if (background == null)
          return false;
        background.Prepare();
        lock (_parent.SyncRoot)
        {
          _backgroundScreen = background;
          _backgroundScreen.ScreenState = Screen.State.Running;
        }
        background.Show();
        return true;
      }

      public void Unload()
      {
        Screen oldBackground;
        ICollection<Guid> oldModels;
        lock (_parent.SyncRoot)
        {
          if (_backgroundScreen == null)
            return;
          oldBackground = _backgroundScreen;
          oldBackground.ScreenState = Screen.State.Closing;
          _backgroundScreen.Hide();
          _backgroundScreen = null;
          oldModels = new List<Guid>(_models.Keys);
          _models.Clear();
        }
        oldBackground.Hide();
        IPluginManager pluginManager = ServiceScope.Get<IPluginManager>();
        foreach (Guid modelId in oldModels)
          pluginManager.RevokePluginItem(MODELS_REGISTRATION_LOCATION, modelId.ToString(), this);
      }

      #region IPluginItemStateTracker implementation

      public string UsageDescription
      {
        get { return "ScreenManager: Usage of model in background screen"; }
      }

      public bool RequestEnd(PluginItemRegistration itemRegistration)
      {
        lock (_parent.SyncRoot)
          return !_models.ContainsKey(new Guid(itemRegistration.Metadata.Id));
      }

      public void Stop(PluginItemRegistration itemRegistration)
      {
        Unload();
      }

      public void Continue(PluginItemRegistration itemRegistration) { }

      #endregion

      #region IModelLoader implementation

      public object GetOrLoadModel(Guid modelId)
      {
        object result;
        lock (_parent.SyncRoot)
          if (_models.TryGetValue(modelId, out result))
            return result;
        result = ServiceScope.Get<IPluginManager>().RequestPluginItem<object>(
            MODELS_REGISTRATION_LOCATION, modelId.ToString(), this);
        if (result == null)
          throw new ArgumentException(string.Format("ScreenManager: Model with id '{0}' is not available", modelId));
        lock (_parent.SyncRoot)
          _models[modelId] = result;
        return result;
      }

      #endregion
    }

    #endregion

    #region Protected fields

    protected readonly object _syncObj = new object(); // Synchronize field access
    protected readonly SkinManager _skinManager;

    protected readonly BackgroundData _backgroundData;
    protected readonly Stack<Screen> _dialogStack = new Stack<Screen>();
    protected Screen _currentScreen = null;
    protected int _numPendingOperations = 0;

    protected bool _backgroundDisabled = false;

    protected Skin _skin = null;
    protected Theme _theme = null;

    protected AsynchronousMessageQueue _messageQueue;

    #endregion

    public ScreenManager()
    {
      SkinSettings screenSettings = ServiceScope.Get<ISettingsManager>().Load<SkinSettings>();
      _skinManager = new SkinManager();
      _backgroundData = new BackgroundData(this);

      string skinName = screenSettings.Skin;
      string themeName = screenSettings.Theme;
      if (string.IsNullOrEmpty(skinName))
      {
        skinName = SkinManager.DEFAULT_SKIN;
        themeName = null;
      }
      SubscribeToMessages();

      // Prepare the skin and theme - the theme will be activated in method MainForm_Load
      ServiceScope.Get<ILogger>().Debug("ScreenManager: Loading skin '{0}', theme '{1}'", skinName, themeName);
      PrepareSkinAndTheme_NeedLocks(skinName, themeName);

      // Update the settings with our current skin/theme values
      if (screenSettings.Skin != SkinName || screenSettings.Theme != ThemeName)
      {
        screenSettings.Skin = _skin.Name;
        screenSettings.Theme = _theme == null ? null : _theme.Name;
        ServiceScope.Get<ISettingsManager>().Save(screenSettings);
      }
    }

    void OnMessageReceived(AsynchronousMessageQueue queue, QueueMessage message)
    {
      if (message.ChannelName == ScreenManagerMessaging.CHANNEL)
      {
        ScreenManagerMessaging.MessageType messageType = (ScreenManagerMessaging.MessageType) message.MessageType;
        Screen screen;
        switch (messageType)
        {
          case ScreenManagerMessaging.MessageType.ShowScreen:
            screen = (Screen) message.MessageData[ScreenManagerMessaging.SCREEN];
            bool closeDialogs = (bool) message.MessageData[ScreenManagerMessaging.CLOSE_DIALOGS];
            DoShowScreen(screen, closeDialogs);
            DecPendingOperations();
            break;
          case ScreenManagerMessaging.MessageType.ShowDialog:
            screen = (Screen) message.MessageData[ScreenManagerMessaging.SCREEN];
            DialogCloseCallbackDlgt dialogCloseCallback = (DialogCloseCallbackDlgt) message.MessageData[ScreenManagerMessaging.DIALOG_CLOSE_CALLBACK];
            DoShowDialog(screen, dialogCloseCallback);
            DecPendingOperations();
            break;
          case ScreenManagerMessaging.MessageType.CloseDialog:
            string dialogName = (string) message.MessageData[ScreenManagerMessaging.DIALOG_NAME];
            DoCloseDialog(dialogName);
            DecPendingOperations();
            break;
          case ScreenManagerMessaging.MessageType.ReloadScreens:
            DoReloadScreens();
            DecPendingOperations();
            break;
        }
      }
    }

    void SubscribeToMessages()
    {
      _messageQueue = new AsynchronousMessageQueue(this, new string[]
        {
          ScreenManagerMessaging.CHANNEL,
        });
      _messageQueue.MessageReceived += OnMessageReceived;
      _messageQueue.Start();
    }

    void UnsubscribeFromMessages()
    {
      if (_messageQueue == null)
        return;
      _messageQueue.Shutdown();
      _messageQueue = null;
    }

    protected internal void IncPendingOperations()
    {
      lock (_syncObj)
      {
        _numPendingOperations++;
        Monitor.PulseAll(_syncObj);
      }
    }

    protected internal void DecPendingOperations()
    {
      lock (_syncObj)
      {
        _numPendingOperations--;
        Monitor.PulseAll(_syncObj);
      }
    }

    /// <summary>
    /// Waits until all screens pending to be hidden are hidden and all screens pending to be shown are shown.
    /// </summary>
    protected internal void WaitForPendingOperations()
    {
      lock (_syncObj)
        if (_numPendingOperations > 0)
          // Wait until all outstanding screens have been hidden asynchronously
          Monitor.Wait(_syncObj);
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
    protected void PrepareSkinAndTheme_NeedLocks(string skinName, string themeName)
    {
      lock (_syncObj)
      {
        // Release old resources
        _skinManager.ReleaseSkinResources();
  
        Skin skin;
        Theme theme;
        try
        {
          // Prepare new skin data
          skin = _skinManager.Skins.ContainsKey(skinName) ? _skinManager.Skins[skinName] : null;
          if (skin == null)
            skin = _skinManager.DefaultSkin;
          if (skin == null)
            throw new Exception(string.Format("Skin '{0}' not found", skinName));
          theme = themeName == null ? null :
              (skin.Themes.ContainsKey(themeName) ? skin.Themes[themeName] : null);
          if (theme == null)
            theme = skin.DefaultTheme;
  
          if (!skin.IsValid)
            throw new ArgumentException(string.Format("Skin '{0}' is invalid", skin.Name));
          if (theme != null)
            if (!theme.IsValid)
              throw new ArgumentException(string.Format("Theme '{0}' of skin '{1}' is invalid", theme.Name, skin.Name));
        }
        catch (ArgumentException ex)
        {
          ServiceScope.Get<ILogger>().Error("ScreenManager: Error loading skin '{0}', theme '{1}'", ex, skinName, themeName);
          // Fall back to current skin/theme
          skin = _skin;
          theme = _theme;
        }
  
        SkinResources skinResources = theme == null ? skin : (SkinResources) theme;
        Fonts.FontManager.Load(skinResources);
  
        _skinManager.InstallSkinResources(skinResources);
  
        _skin = skin;
        _theme = theme;
      }
    }

    protected internal void DoShowScreen(Screen screen, bool closeDialogs)
    {
      ServiceScope.Get<ILogger>().Debug("ScreenManager: Showing screen '{0}'...", screen.Name);
      lock (_syncObj)
      {
        if (closeDialogs)
          DoCloseDialogs();
        DoExchangeScreen(screen);
      }
    }

    protected internal void DoShowDialog(Screen dialog, DialogCloseCallbackDlgt dialogCloseCallback)
    {
      dialog.Prepare();
      lock (_syncObj)
      {
        if (_dialogStack.Count == 0)
        {
          if (_currentScreen != null)
            _currentScreen.DetachInput();
        }
        else
          _dialogStack.Peek().DetachInput();

        _dialogStack.Push(dialog);

        if (dialogCloseCallback != null)
          dialog.Closed += (sender, e) => dialogCloseCallback(dialog.Name);
        dialog.AttachInput();
        dialog.ScreenState = Screen.State.Running;
      }
      // Don't hold the lock while showing the screen
      dialog.Show();
    }

    protected internal void DoCloseDialog(string dialogName)
    {
      Screen oldDialog;
      lock(_syncObj)
      {
        // Do we have a dialog?
        if (_dialogStack.Count == 0)
          return;
        oldDialog = _dialogStack.Pop();
        if (oldDialog.Name != dialogName)
          return;

        oldDialog.ScreenState = Screen.State.Closing;
        oldDialog.DetachInput();

        // Is this the last dialog?
        if (_dialogStack.Count == 0)
        {
          if (_currentScreen != null)
            _currentScreen.AttachInput();
        }
        else
          _dialogStack.Peek().AttachInput();
      }
      oldDialog.Hide();
    }

    protected internal void DoCloseDialogs()
    {
      while (true)
      {
        string dialogName;
        lock (_syncObj)
        {
          if (_dialogStack.Count == 0)
            break;
          dialogName = _dialogStack.Peek().Name;
        }
        DoCloseDialog(dialogName);
      }
    }

    protected internal void DoCloseScreen()
    {
      Screen screen;
      lock (_syncObj)
      {
        if (_currentScreen == null)
          return;
        screen = _currentScreen;
        _currentScreen = null;

        screen.ScreenState = Screen.State.Closing;
        screen.DetachInput();
      }
      screen.Hide();
    }

    protected internal void DoCloseCurrentScreenAndDialogs(bool closeBackgroundLayer)
    {
      if (closeBackgroundLayer)
        _backgroundData.Unload();
      DoCloseDialogs();
      DoCloseScreen();
    }

    protected internal void DoExchangeScreen(Screen screen)
    {
      screen.Prepare();
      lock (_syncObj)
      {
        DoCloseScreen();
        _currentScreen = screen;
      }
      screen.ScreenState = Screen.State.Running;
      screen.AttachInput();
      screen.Show();
    }

    protected internal void DoReloadScreens()
    {
      ServiceScope.Get<ILogger>().Debug("ScreenManager: Reload");
      // Remember all open screens
      string backgroundName;
      string screenName;
      List<string> dialogNamesReverse;
      lock (_syncObj)
      {
        backgroundName = _backgroundData.BackgroundScreen == null ? null : _backgroundData.BackgroundScreen.Name;
        screenName = _currentScreen.Name;
        dialogNamesReverse = new List<string>(_dialogStack.Count);
        foreach (Screen dialog in _dialogStack)
          // We should move the dialog's Closed delegate to the new dialog, but it is not possible to copy a delegate.
          // To not clear the delegate here might lead to missbehaviour, but clearing it without copying it will be even
          // worse because modules might rely on this behaviour.
          dialogNamesReverse.Add(dialog.Name);
      }

      // Close all
      DoCloseCurrentScreenAndDialogs(true);

      // Reload background
      if (backgroundName != null)
        _backgroundData.Load(backgroundName);

      // Reload screen
      Screen screen = GetScreen(screenName);
      if (screen == null)
          // Error message was shown in GetScreen()
        return;
      DoExchangeScreen(screen);

      // Reload dialogs
      foreach (string dialogName in dialogNamesReverse)
      {
        Screen dialog = GetScreen(dialogName);
        // We should have copied the dialog's Closed delegate of the old dialog instead of using null here... but it's not possible to
        // copy it
        DoShowDialog(dialog, null);
      }
    }

    protected IList<Screen> GetAllScreens()
    {
      return GetScreens(true, true, true);
    }

    protected IList<Screen> GetScreens(bool background, bool main, bool dialogs)
    {
      lock (_syncObj)
      {
        IList<Screen> result = new List<Screen>();
        if (background)
        {
          Screen backgroundScreen = _backgroundData.BackgroundScreen;
          if (backgroundScreen != null)
            result.Add(backgroundScreen);
        }
        if (main)
        {
          if (_currentScreen != null)
            result.Add(_currentScreen);
        }
        if (dialogs)
        {
          Screen[] dialogsArray = _dialogStack.ToArray();
          Array.Reverse(dialogsArray);
          CollectionUtils.AddAll(result, dialogsArray);
        }
        return result;
      }
    }

    protected void ForEachScreen(ScreenExecutor executor)
    {
      foreach (Screen screen in GetAllScreens())
        executor(screen);
    }

    public bool InstallBackgroundManager()
    {
      // No locking here
      return _skinManager.InstallBackgroundManager(_skin);
    }

    public void UninstallBackgroundManager()
    {
      // No locking here
      _skinManager.UninstallBackgroundManager();
    }

    /// <summary>
    /// Disposes all resources which were allocated by the screen manager.
    /// </summary>
    public void Shutdown()
    {
      lock (_syncObj)
      {
        UnsubscribeFromMessages();
        DoCloseCurrentScreenAndDialogs(true);
      }
      _skinManager.Dispose();
    }

    public ISkinResourceManager SkinResourceManager
    {
      get { return _skinManager; }
    }

    public object SyncRoot
    {
      get { return _syncObj; }
    }

    /// <summary>
    /// Renders the current screens (background, main and dialogs).
    /// </summary>
    public void Render()
    {
      IList<Screen> disabledScreens = GetScreens(_backgroundDisabled, false, false);
      IList<Screen> enabledScreens = GetScreens(!_backgroundDisabled, true, true);
      lock (_syncObj)
      {
        SkinContext.Now = DateTime.Now;
        foreach (Screen screen in disabledScreens)
          screen.Animate();
        foreach (Screen screen in enabledScreens)
        {
          screen.Animate();
          screen.Render();
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
      lock (_syncObj)
      {
        if (newSkinName == _skin.Name &&
            newThemeName == (_theme == null ? null : _theme.Name)) return;
        ServiceScope.Get<ILogger>().Info("ScreenManager: Switching to skin '{0}', theme '{1}'",
            newSkinName, newThemeName);

        string currentScreenName = _currentScreen == null ? null : _currentScreen.Name;
        Screen backgroundScreen = _backgroundData.BackgroundScreen;
        string currentBackgroundName = backgroundScreen == null ? null : backgroundScreen.Name;

        UninstallBackgroundManager();
        _backgroundData.Unload();

        DoCloseCurrentScreenAndDialogs(true);

        PlayersHelper.ReleaseGUIResources();

        // FIXME Albert78: Find a better way to make ContentManager observe the current skin
        ContentManager.Clear();

        WaitForPendingOperations();

        PrepareSkinAndTheme_NeedLocks(newSkinName, newThemeName);
        PlayersHelper.ReallocGUIResources();

        if (!InstallBackgroundManager())
          _backgroundData.Load(currentBackgroundName);

        Screen screen = GetScreen(currentScreenName);
        DoExchangeScreen(screen);
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
    protected static UIElement LoadScreen(string screenName)
    {
      return SkinContext.SkinResources.LoadScreenFile(screenName, new WorkflowManagerModelLoader()) as UIElement;
    }

    /// <summary>
    /// Loads the root UI element for the specified background screen from the current skin.
    /// </summary>
    /// <param name="screenName">The background screen to load.</param>
    /// <param name="loader">Model loader for the new background screen.</param>
    /// <returns>Root UI element for the specified screen.</returns>
    protected static UIElement LoadBackgroundScreen(string screenName, IModelLoader loader)
    {
      return SkinContext.SkinResources.LoadBackgroundScreenFile(screenName, loader) as UIElement;
    }

    /// <summary>
    /// Gets the screen with the specified name.
    /// </summary>
    /// <param name="screenName">Name of the screen to return.</param>
    /// <returns>screen or <c>null</c>, if an error occured while loading the screen.</returns>
    public static Screen GetScreen(string screenName)
    {
      Screen result = new Screen(screenName);
      try
      {
        UIElement root = LoadScreen(screenName);
        if (root == null)
        {
          ServiceScope.Get<ILogger>().Error("ScreenManager: Cannot load screen '{0}'", screenName);
          ServiceScope.Get<IDialogManager>().ShowDialog(ERROR_LOADING_SKIN_RESOURCE_TEXT,
              LocalizationHelper.CreateResourceString(SCREEN_MISSING_TEXT).Evaluate(screenName),
              DialogType.OkDialog, false, null);
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
              DialogType.OkDialog, false, null);
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
    /// <param name="loader">Model loader for the new background screen.</param>
    /// <returns>screen or <c>null</c>, if an error occured while loading the screen.</returns>
    public static Screen GetBackground(string screenName, IModelLoader loader)
    {
      Screen result = new Screen(screenName);
      try
      {
        UIElement root = LoadBackgroundScreen(screenName, loader);
        if (root == null)
        {
          ServiceScope.Get<ILogger>().Error("ScreenManager: Cannot load background screen '{0}'", screenName);
          ServiceScope.Get<IDialogManager>().ShowDialog(ERROR_LOADING_SKIN_RESOURCE_TEXT,
              LocalizationHelper.CreateResourceString(BACKGROUND_SCREEN_MISSING_TEXT).Evaluate(screenName),
              DialogType.OkDialog, false, null);
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
              DialogType.OkDialog, false, null);
        }
        catch (Exception)
        {
          ServiceScope.Get<ILogger>().Error("ScreenManager: Error showing generic dialog for error message");
          return null;
        }
        return null;
      }
    }

    public string CurrentDialogName
    {
      get
      {
        lock (_syncObj)
          return _dialogStack.Count > 0 ? _dialogStack.Peek().Name : null;
      }
    }

    public void Reset()
    {
      ForEachScreen(screen => screen.Reset());
    }

    public void Exit()
    {
      ForEachScreen(screen => screen.Deallocate());
      Fonts.FontManager.Unload();
    }

    #region IScreenManager implementation

    public string SkinName
    {
      get
      {
        lock (_syncObj)
          return _skin.Name;
      }
    }

    public string ThemeName
    {
      get
      {
        lock (_syncObj)
          return _theme == null ? null : _theme.Name;
      }
    }

    public string ActiveScreenName
    {
      get
      {
        lock (_syncObj)
          return CurrentDialogName ?? (_currentScreen == null ? null : _currentScreen.Name);
      }
    }

    public string ActiveBackgroundScreenName
    {
      get
      {
        // No locking necessary
        Screen backgroundScreen = _backgroundData.BackgroundScreen;
        return backgroundScreen == null ? null : backgroundScreen.Name;
      }
    }

    public bool IsDialogVisible
    {
      get
      {
        lock (_syncObj)
          return _dialogStack.Count > 0;
      }
    }

    public bool BackgroundDisabled
    {
      get
      {
        lock (_syncObj)
          return _backgroundDisabled;
      }
      set
      {
        lock (_syncObj)
        {
          if (value)
            ServiceScope.Get<ILogger>().Debug("ScreenManager: Disabling background screen rendering");
          else
            ServiceScope.Get<ILogger>().Debug("ScreenManager: Enabling background screen rendering");
          _backgroundDisabled = value;
        }
      }
    }

    public void SwitchTheme(string newThemeName)
    {
      SwitchSkinAndTheme(_skin.Name, newThemeName);
    }

    public void SwitchSkin(string newSkinName)
    {
      SwitchSkinAndTheme(newSkinName, null);
    }

    public bool ShowScreen(string screenName)
    {
      IncPendingOperations();
      ServiceScope.Get<ILogger>().Debug("ScreenManager: Preparing to show screen '{0}'...", screenName);
      Screen newScreen = GetScreen(screenName);
      if (newScreen == null)
          // Error message was shown in GetScreen()
        return false;

      ScreenManagerMessaging.SendMessageShowScreen(newScreen, true);
      return true;
    }

    public bool ExchangeScreen(string screenName)
    {
      IncPendingOperations();
      ServiceScope.Get<ILogger>().Debug("ScreenManager: Preparing to show screen '{0}'...", screenName);
      Screen newScreen = GetScreen(screenName);
      if (newScreen == null)
          // Error message was shown in GetScreen()
        return false;

      ScreenManagerMessaging.SendMessageShowScreen(newScreen, false);
      return true;
    }

    public bool ShowDialog(string dialogName)
    {
      IncPendingOperations();
      return ShowDialog(dialogName, null);
    }

    public bool ShowDialog(string dialogName, DialogCloseCallbackDlgt dialogCloseCallback)
    {
      IncPendingOperations();
      ServiceScope.Get<ILogger>().Debug("ScreenManager: Preparing to show dialog '{0}'...", dialogName);
      Screen newDialog = GetScreen(dialogName);
      if (newDialog == null)
        return false;
      ScreenManagerMessaging.SendMessageShowDialog(newDialog, dialogCloseCallback);
      return true;
    }

    public void CloseDialog()
    {
      IncPendingOperations();
      ServiceScope.Get<ILogger>().Debug("ScreenManager: Preparing to close topmost dialog");
      ScreenManagerMessaging.SendMessageCloseDialog(CurrentDialogName);
    }

    public bool SetBackgroundLayer(string backgroundName)
    {
      ServiceScope.Get<ILogger>().Debug("ScreenManager: Setting background screen '{0}'...", backgroundName);
      // No locking necessary
      if (backgroundName == null)
      {
        _backgroundData.Unload();
        return true;
      }
      else
        return _backgroundData.Load(backgroundName);
    }

    public void Reload()
    {
      IncPendingOperations();
      ServiceScope.Get<ILogger>().Debug("ScreenManager: Preparing to reload screens");
      ScreenManagerMessaging.SendMessageReloadScreens();
    }

    #endregion
  }
}
