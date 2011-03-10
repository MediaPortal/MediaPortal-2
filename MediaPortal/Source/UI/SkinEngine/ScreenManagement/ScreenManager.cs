#region Copyright (C) 2007-2011 Team MediaPortal

/*
    Copyright (C) 2007-2011 Team MediaPortal
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
using System.IO;
using System.Linq;
using System.Threading;
using MediaPortal.Core.Messaging;
using MediaPortal.Core.PluginManager;
using MediaPortal.Core.Localization;
using MediaPortal.UI.Presentation.Screens;
using MediaPortal.UI.Presentation.SkinResources;
using MediaPortal.UI.Presentation.UiNotifications;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UI.SkinEngine.ContentManagement;
using MediaPortal.Core;
using MediaPortal.Core.Logging;
using MediaPortal.Core.Settings;
using MediaPortal.UI.SkinEngine.MpfElements;
using MediaPortal.UI.SkinEngine.Players;
using MediaPortal.UI.SkinEngine.Settings;
using MediaPortal.UI.SkinEngine.SkinManagement;

namespace MediaPortal.UI.SkinEngine.ScreenManagement
{
  public enum CloseDialogsMode
  {
    CloseSingleDialog,
    CloseAllOnTopIncluding,
    CloseAllOnTopExcluding
  }

    public enum ScreenType
    {
      ScreenOrDialog,
      Background,
      SuperLayer,
    }

  // TODO: Use _syncObj only for access on local variables; use the screen's own sync object to lock
  // out threads during render and other screen access
  public class ScreenManager : IScreenManager
  {
    #region Consts

    public const string HOME_SCREEN = "home";

    public const string RES_ERROR_LOADING_SKIN_RESOURCE_TEXT_RES = "[ScreenManager.ErrorLoadingSkinResource]";
    public const string RES_SCREEN_MISSING_TEXT = "[ScreenManager.ScreenMissing]";
    public const string RES_SCREEN_BROKEN_TEXT = "[ScreenManager.ScreenBroken]";
    public const string RES_BACKGROUND_SCREEN_MISSING_TEXT = "[ScreenManager.BackgroundScreenMissing]";
    public const string RES_SUPER_LAYER_MISSING_TEXT = "[ScreenManager.SuperLayerScreenMissing]";
    public const string RES_BACKGROUND_SCREEN_BROKEN_TEXT = "[ScreenManager.BackgroundScreenBroken]";
    public const string RES_SUPER_LAYER_BROKEN_TEXT = "[ScreenManager.SuperLayerScreenBroken]";

    public const string MODELS_REGISTRATION_LOCATION = "/Models";

    #endregion

    #region Classes, delegates & enums

    protected delegate void ScreenExecutor(Screen screen);

    /// <summary>
    /// Model loader used in the loading process of normal screens. GUI models requested via this
    /// model loader are requested from the workflow manger and thus attached to the workflow manager's current context.
    /// </summary>
    protected class WorkflowManagerModelLoader : IModelLoader
    {
      public object GetOrLoadModel(Guid modelId)
      {
        return ServiceRegistration.Get<IWorkflowManager>().GetModel(modelId);
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
        Screen background = GetScreen(backgroundName, this, ScreenType.Background);
        if (background == null)
          return false;
        background.Prepare();
        lock (_parent.SyncRoot)
          _backgroundScreen = background;
        return true;
      }

      public void Unload()
      {
        ICollection<Guid> oldModels;
        lock (_parent.SyncRoot)
        {
          if (_backgroundScreen == null)
            return;
          _backgroundScreen.ScreenState = Screen.State.Closed;
          Screen oldBackground = _backgroundScreen;
          _backgroundScreen = null;
          _parent.ScheduleDisposeScreen(oldBackground);
          oldModels = new List<Guid>(_models.Keys);
          _models.Clear();
        }
        IPluginManager pluginManager = ServiceRegistration.Get<IPluginManager>();
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
        result = ServiceRegistration.Get<IPluginManager>().RequestPluginItem<object>(
            MODELS_REGISTRATION_LOCATION, modelId.ToString(), this);
        if (result == null)
          throw new ArgumentException(string.Format("ScreenManager: Model with id '{0}' is not available", modelId));
        lock (_parent.SyncRoot)
          _models[modelId] = result;
        return result;
      }

      #endregion
    }

    protected class DialogSaveDescriptor
    {
      protected string _dialogName;
      protected Guid _dialogId;
      protected DialogCloseCallbackDlgt _closeCallback;

      public DialogSaveDescriptor(string dialogName, Guid dialogId, DialogCloseCallbackDlgt closeCallback)
      {
        _dialogName = dialogName;
        _dialogId = dialogId;
        _closeCallback = closeCallback;
      }

      public string DialogName
      {
        get { return _dialogName; }
      }

      public Guid DialogId
      {
        get { return _dialogId; }
      }

      public DialogCloseCallbackDlgt CloseCallback
      {
        get { return _closeCallback; }
      }
    }

    protected class DialogStackList : LinkedList<DialogData>
    {
      public void Push(DialogData dd)
      {
        AddFirst(dd);
      }

      public DialogData Pop()
      {
        DialogData dd = Peek();
        if (dd != null) RemoveFirst();
        return dd;
      }

      public DialogData Peek()
      {
        return (First == null) ? null : First.Value;
      }
    }

    #endregion

    #region Protected fields

    protected readonly object _syncObj = new object(); // Synchronize field access
    protected readonly SkinManager _skinManager;

    protected readonly BackgroundData _backgroundData;
    protected readonly DialogStackList _dialogStack = new DialogStackList();
    protected Screen _currentScreen = null; // "Normal" screen
    protected Screen _nextScreen = null; // Holds the next screen while the current screen finishes closing
    protected Screen _currentSuperLayer = null; // Layer on top of screen and all dialogs - busy indicator and additional popups
    protected int _numPendingAsyncOperations = 0;
    protected Screen _focusedScreen = null;
    protected DateTime _screenPersistenceTime = DateTime.MaxValue;

    protected bool _backgroundDisabled = false;

    protected Skin _skin = null;
    protected Theme _theme = null;

    protected AsynchronousMessageQueue _messageQueue;
    protected Thread _garbageCollectorThread;
    protected Queue<Screen> _garbageScreens = new Queue<Screen>(10);

    #endregion

    public ScreenManager()
    {
      SkinSettings screenSettings = ServiceRegistration.Get<ISettingsManager>().Load<SkinSettings>();
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
      if (!PrepareSkinAndTheme(skinName, themeName))
        PrepareSkinAndTheme(null, null);

      // Update the settings with our current skin/theme values
      if (screenSettings.Skin != SkinName || screenSettings.Theme != ThemeName)
      {
        screenSettings.Skin = _skin.Name;
        screenSettings.Theme = _theme == null ? null : _theme.Name;
        ServiceRegistration.Get<ISettingsManager>().Save(screenSettings);
      }
      _garbageCollectorThread = new Thread(DoGarbageCollection)
        {
          Name = "ScrMgrGC",  //garbage collector thread
            Priority = ThreadPriority.Lowest,
            IsBackground = true
        };
      _garbageCollectorThread.Start();
    }

    void OnMessageReceived(AsynchronousMessageQueue queue, SystemMessage message)
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
          case ScreenManagerMessaging.MessageType.SetSuperLayer:
            screen = (Screen) message.MessageData[ScreenManagerMessaging.SCREEN];
            DoSetSuperLayer(screen);
            DecPendingOperations();
            break;
          case ScreenManagerMessaging.MessageType.ShowDialog:
            DialogData dialogData = (DialogData) message.MessageData[ScreenManagerMessaging.DIALOG_DATA];
            DoShowDialog(dialogData);
            DecPendingOperations();
            break;
          case ScreenManagerMessaging.MessageType.CloseDialogs:
            Guid dialogInstanceId = (Guid) message.MessageData[ScreenManagerMessaging.DIALOG_INSTANCE_ID];
            CloseDialogsMode mode = (CloseDialogsMode) message.MessageData[ScreenManagerMessaging.CLOSE_DIALOGS_MODE];
            DoCloseDialogs(dialogInstanceId, mode, true, true);
            DecPendingOperations();
            break;
          case ScreenManagerMessaging.MessageType.ReloadScreens:
            DoReloadScreens();
            DecPendingOperations();
            break;
          case ScreenManagerMessaging.MessageType.ScreenClosing:
            screen = (Screen) message.MessageData[ScreenManagerMessaging.SCREEN];
            DoStartClosingScreen(screen);
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

    protected void DoGarbageCollection()
    {
      while (true)
      {
        Screen screen;
        bool active = true;
        while (active)
        {
          lock (_syncObj)
            screen = _garbageScreens.Count == 0 ? null : _garbageScreens.Dequeue();
          if (screen == null)
            active = false;
          else
            screen.Close();
        }
        lock (_syncObj)
          Monitor.Wait(_syncObj);
      }
    }

    protected void ScheduleDisposeScreen(Screen screen)
    {
      lock (_syncObj)
      {
        _garbageScreens.Enqueue(screen);
        Monitor.PulseAll(_syncObj);
      }
    }

    protected internal void IncPendingOperations()
    {
      lock (_syncObj)
      {
        _numPendingAsyncOperations++;
        Monitor.PulseAll(_syncObj);
      }
    }

    protected internal void DecPendingOperations()
    {
      lock (_syncObj)
      {
        _numPendingAsyncOperations--;
        Monitor.PulseAll(_syncObj);
      }
    }

    /// <summary>
    /// Waits until all screens pending to be hidden are hidden and all screens pending to be shown are shown.
    /// </summary>
    protected internal void WaitForPendingOperations()
    {
      lock (_syncObj)
        while (_numPendingAsyncOperations > 0)
          // Wait until all outstanding screens have been hidden asynchronously
          Monitor.Wait(_syncObj);
    }

    /// <summary>
    /// Prepares the skin and theme, this will load the skin and theme instances and
    /// set it as the current skin and theme in the <see cref="SkinContext"/>.
    /// After calling this method, the <see cref="SkinContext.SkinResources"/>
    /// contents can be requested.
    /// </summary>
    /// <param name="skinName">The name of the skin to be prepared or <c>null</c> to use the current skin.</param>
    /// <param name="themeName">The name of the theme for the specified skin to be prepared,
    /// or <c>null</c> for the default theme of the given skin.</param>
    /// <returns><c>true</c>, if the given skin/theme could be prepared, else <c>false</c>.</returns>
    protected bool PrepareSkinAndTheme(string skinName, string themeName)
    {
      ServiceRegistration.Get<ILogger>().Debug("ScreenManager: Trying to load skin '{0}' with theme '{1}'", skinName, themeName);
      Skin defaultSkin = _skinManager.DefaultSkin;
      lock (_syncObj)
      {
        // Release old resources
        _skinManager.ReleaseSkinResources();

        Skin skin;
        Theme theme;
        // Try to prepare new skin/theme
        try
        {
          // Prepare new skin data

          if (skinName == null)
            skin = _skin ?? defaultSkin;
          else if (!_skinManager.Skins.TryGetValue(skinName, out skin))
          {
            ServiceRegistration.Get<ILogger>().Warn("ScreenManager: Skin '{0}' not found", skinName);
            return false;
          }
          if (themeName == null)
            theme = skin.DefaultTheme;
          else if (!skin.Themes.TryGetValue(themeName, out theme))
          {
            ServiceRegistration.Get<ILogger>().Warn("ScreenManager: Theme '{0}' not found in skin '{1}'", themeName, skin.Name);
            return false;
          }

          if (!skin.IsValid)
            throw new ArgumentException(string.Format("Skin '{0}' is invalid", skin.Name));
          if (theme != null)
            if (!theme.IsValid)
              throw new ArgumentException(string.Format("Theme '{0}' of skin '{1}' is invalid", theme.Name, skin.Name));
        }
        catch (ArgumentException ex)
        {
          ServiceRegistration.Get<ILogger>().Error("ScreenManager: Error loading skin '{0}', theme '{1}', fallback to previous skin/theme",
              ex, skinName, themeName);
          // Fall back to current skin/theme
          skin = _skin;
          theme = _theme;
        }

        // Try to apply new skin/theme
        try
        {
          SkinResources skinResources = theme == null ? skin : (SkinResources) theme;
          ServiceRegistration.Get<ILogger>().Info("ScreenManager: Applying skin '{0}', theme '{1}'",
              skin == null ? string.Empty : skin.Name, theme == null ? string.Empty : theme.Name);
          Fonts.FontManager.Load(skinResources);

          _skinManager.InstallSkinResources(skinResources);

          _skin = skin;
          _theme = theme;
        }
        catch (Exception ex)
        {
          // Didn't work - try fallback skin/theme
          ServiceRegistration.Get<ILogger>().Error("ScreenManager: Error applying skin '{0}', theme '{1}'", ex,
              skin == null ? "<undefined>" : skin.Name, theme == null ? "<undefined>" : theme.Name);
          Skin fallbackSkin = _skin ?? defaultSkin; // Either the previous skin (_skin) or the default skin
          Theme fallbackTheme = fallbackSkin.DefaultTheme;
          if (fallbackSkin == skin && fallbackTheme == theme)
          {
            ServiceRegistration.Get<ILogger>().Error("ScreenManager: There is no valid skin to show");
            throw;
          }
          return PrepareSkinAndTheme(fallbackSkin.Name, fallbackTheme == null ? null : fallbackTheme.Name);
        }
      }
      return true;
    }

    protected internal void DoShowScreen(Screen screen, bool closeDialogs)
    {
      ServiceRegistration.Get<ILogger>().Debug("ScreenManager: Showing screen '{0}'...", screen.ResourceName);
      lock (_syncObj)
      {
        if (closeDialogs)
          DoCloseDialogs(true, false);
        DoExchangeScreen(screen);
      }
    }

    public void FocusScreen(Screen screen)
    {
      if (screen != null)
        screen.AttachInput();
      lock (_syncObj)
        _focusedScreen = screen;
    }

    public void UnfocusCurrentScreen()
    {
      Screen screen;
      lock (_syncObj)
      {
        screen = _focusedScreen;
        _focusedScreen = null;
      }
      if (screen != null)
        screen.DetachInput();
    }

    protected internal void DoShowDialog(DialogData dialogData)
    {
      dialogData.DialogScreen.Prepare();
      dialogData.DialogScreen.FireScreenShowingEvent();
      lock (_syncObj)
      {
        UnfocusCurrentScreen();

        DialogData dd = dialogData;
        _dialogStack.Push(dd);
      }
      // Don't hold the lock while focusing the screen
      FocusScreen(dialogData.DialogScreen);
    }

    protected LinkedListNode<DialogData> FindDialogNode(Guid dialogInstanceId)
    {
      lock (_syncObj)
      {
        LinkedListNode<DialogData> node = _dialogStack.First;
        while (node != null && node.Value.DialogInstanceId != dialogInstanceId)
          node = node.Next;
        return node;
      }
    }

    protected internal void DoCloseDialogs(Guid? dialogInstanceId, CloseDialogsMode mode, bool fireCloseDelegates, bool dialogPersistence)
    {
      ICollection<DialogData> oldDialogData = new List<DialogData>();
      lock(_syncObj)
      {
        LinkedListNode<DialogData> bottomDialogNode = dialogInstanceId.HasValue ? FindDialogNode(dialogInstanceId.Value) : _dialogStack.First;
        if (bottomDialogNode == null)
        {
          if (dialogInstanceId.HasValue)
            ServiceRegistration.Get<ILogger>().Warn("ScreenManager.DoCloseDialogs: Dialog to close with dialog instance id '{0}' was not found on the dialog stack", dialogInstanceId.Value);
          return;
        }
        // Remove input attachment
        UnfocusCurrentScreen();

        // Find all dialogs on top of our dialog
        LinkedListNode<DialogData> node;
        if (mode != CloseDialogsMode.CloseSingleDialog)
        {
          node = _dialogStack.First;
          while (node != bottomDialogNode)
          {
            oldDialogData.Add(node.Value);
            node = node.Next;
          }
          // Remove dialogs from stack if no persistence is desired
          if (!dialogPersistence)
            while (_dialogStack.First != bottomDialogNode)
              _dialogStack.RemoveFirst();
        }
        // Add our dialog if dictated by mode
        if (mode != CloseDialogsMode.CloseAllOnTopExcluding)
        {
          oldDialogData.Add(bottomDialogNode.Value);
          if (!dialogPersistence)
            _dialogStack.Remove(bottomDialogNode);
        }
      }
      foreach (DialogData dd in oldDialogData)
        DoCloseDialog(dd, fireCloseDelegates, dialogPersistence);

      CompleteDialogClosures();
    }

    protected internal void DoCloseDialog(DialogData dd, bool fireCloseDelegates, bool dialogPersistence)
    {
      Screen oldDialog = dd.DialogScreen;
      if (dialogPersistence)
      {
        oldDialog.ScreenState = Screen.State.Closing;
        oldDialog.FireScreenClosingEvent();
      }
      else
      {
        oldDialog.ScreenState = Screen.State.Closed;
        ScheduleDisposeScreen(oldDialog);
      }
        
      if (fireCloseDelegates && dd.CloseCallback != null)
        dd.CloseCallback(dd.DialogScreen.ResourceName, dd.DialogInstanceId);
    }

    protected internal void DoCloseDialogs(bool fireCloseDelegates, bool dialogPersistence)
    {
      DoCloseDialogs(null, CloseDialogsMode.CloseAllOnTopIncluding, fireCloseDelegates, dialogPersistence);
    }

    protected internal void CompleteDialogClosures()
    {
      Screen screenToFocus = null;
      lock (_syncObj)
      {
        LinkedListNode<DialogData> node = _dialogStack.Last;
        while (node != null)
        {
          Screen screen = node.Value.DialogScreen;
          if (screen.ScreenState == Screen.State.Closing && screen.DoneClosing)
          {
            LinkedListNode<DialogData> prevNode = node.Previous;
            node.Value.DialogScreen.ScreenState = Screen.State.Closed;
            ScheduleDisposeScreen(screen);
            _dialogStack.Remove(node);
            node = prevNode;
            continue;
          }
          if (screen.ScreenState != Screen.State.Closing)
            screenToFocus = screen;
          node = node.Previous;
        }
        if (screenToFocus == null)
          screenToFocus = _currentScreen;
      }
      FocusScreen(screenToFocus);
    }

    protected internal void DoCloseScreen()
    {
      lock (_syncObj)
      {
        if (_currentScreen == null)
          return;
        Screen screen = _currentScreen;
        _currentScreen = null;
        _screenPersistenceTime = DateTime.MinValue;

        screen.ScreenState = Screen.State.Closed;
        UnfocusCurrentScreen();

        ScheduleDisposeScreen(screen);
      }
    }

    protected internal void DoCloseCurrentScreenAndDialogs(bool closeBackgroundLayer, bool closeSuperLayer, bool fireCloseDelegates)
    {
      if (closeBackgroundLayer)
        _backgroundData.Unload();
      if (closeSuperLayer)
        DoSetSuperLayer(null);
      DoCloseDialogs(fireCloseDelegates, false);
      DoCloseScreen();
    }

    protected internal void DoStartClosingScreen(Screen screen)
    {
      lock (_syncObj)
      {
        if (_currentScreen == null || _currentScreen.ScreenState == Screen.State.Closing || 
          screen == null || screen.ResourceName != _currentScreen.ResourceName)
          return;

        DoCloseDialogs(true, true);
        _currentScreen.FireScreenClosingEvent();
      }
      UnfocusCurrentScreen();
    }

    protected internal void CompleteScreenClosure()
    {
      Screen screen;
      lock (_syncObj)
      {
        // Has the current screen finished closing?
        if (_currentScreen != null && (_currentScreen.ScreenState != Screen.State.Closing || !_currentScreen.DoneClosing))
          return;

        screen = _nextScreen;
        if (screen != null && (_currentScreen == null || screen.ResourceName != _currentScreen.ResourceName))
          screen.FireScreenShowingEvent();

        DoCloseScreen();
        _currentScreen = screen;
        _nextScreen = null;

        BackgroundDisabled = (_currentScreen == null) ? false : !_currentScreen.HasBackground;
      }
      FocusScreen(screen);
    }

    protected internal void DoExchangeScreen(Screen screen)
    {
      screen.Prepare();
      lock (_syncObj)
      {
        if (_nextScreen != null)
        {
          _nextScreen.ScreenState = Screen.State.Closed;
          ScheduleDisposeScreen(_nextScreen);
        }
        if (_currentScreen != null)
          _currentScreen.ScreenState = Screen.State.Closing;
  
        _nextScreen = screen;
        CompleteScreenClosure();
      }
    }

    protected internal void DoSetSuperLayer(Screen screen)
    {
      if (screen != null)
        screen.Prepare();
      lock (_syncObj)
      {
        if (_currentSuperLayer != null)
          ScheduleDisposeScreen(_currentSuperLayer);
        _currentSuperLayer = screen;
      }
    }

    protected internal void DoReloadScreens()
    {
      ServiceRegistration.Get<ILogger>().Debug("ScreenManager: Reload");
      // Remember all open screens
      string backgroundName;
      string screenName;
      string superLayerName;
      List<DialogSaveDescriptor> dialogsReverse;
      lock (_syncObj)
      {
        backgroundName = _backgroundData.BackgroundScreen == null ? null : _backgroundData.BackgroundScreen.ResourceName;
        screenName = _currentScreen.ResourceName;
        superLayerName = _currentSuperLayer == null ? null : _currentSuperLayer.ResourceName;
        dialogsReverse = new List<DialogSaveDescriptor>(_dialogStack.Count);
        // Remember all dialogs and their close callbacks
        dialogsReverse.AddRange(_dialogStack.Select(dd => new DialogSaveDescriptor(dd.DialogScreen.ResourceName, dd.DialogInstanceId, dd.CloseCallback)));
      }

      // Close all
      DoCloseCurrentScreenAndDialogs(true, true, false);

      // Reload background
      if (backgroundName != null)
        _backgroundData.Load(backgroundName);

      // Reload screen
      Screen screen = GetScreen(screenName, ScreenType.ScreenOrDialog);
      if (screen == null)
        // Error message was shown in GetScreen()
        return;
      DoExchangeScreen(screen);

      // Reload dialogs
      foreach (DialogSaveDescriptor dialogDescriptor in dialogsReverse)
      {
        Screen dialog = GetScreen(dialogDescriptor.DialogName, ScreenType.ScreenOrDialog);
        dialog.ScreenInstanceId = dialogDescriptor.DialogId;
        DoShowDialog(new DialogData(dialog, dialogDescriptor.CloseCallback));
      }

      if (superLayerName != null)
      {
        // Reload screen
        Screen superLayer = GetScreen(screenName, ScreenType.SuperLayer);
        if (superLayer == null)
            // Error message was shown in GetScreen()
          return;
        DoSetSuperLayer(superLayer);
      }
    }

    protected IList<Screen> GetAllScreens()
    {
      return GetScreens(true, true, true, true);
    }

    protected IList<Screen> GetScreens(bool background, bool main, bool dialogs, bool superLayer)
    {
      lock (_syncObj)
      {
        List<Screen> result = new List<Screen>();
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
          LinkedListNode<DialogData> node = _dialogStack.Last;
          while (node != null) {
            result.Add(node.Value.DialogScreen);
            node = node.Previous;
          }
        }
        if (superLayer)
        {
          if (_currentSuperLayer != null)
            result.Add(_currentSuperLayer);
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
      UnsubscribeFromMessages();
      // Close all screens to make sure all SlimDX objects are correctly cleaned up
      DoCloseCurrentScreenAndDialogs(true, true, false);
      _skinManager.Dispose();
      Fonts.FontManager.Unload();
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
      // The next lock operation could cause deadlocks, see comment of this class
      lock (_syncObj)
      {
        SkinContext.FrameRenderingStartTime = DateTime.Now;
        
        // Check if we're waiting for screens to finish closing
        CompleteScreenClosure();
        CompleteDialogClosures();

        IList<Screen> disabledScreens = GetScreens(_backgroundDisabled, false, false, false);
        IList<Screen> enabledScreens = GetScreens(!_backgroundDisabled, true, true, true);
        foreach (Screen screen in disabledScreens)
          // Animation of screen is necessary to avoid an overrun of the async properties setter buffer
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
    /// <param name="newSkinName">Name of the skin to load or <c>null</c> to use the current skin.</param>
    /// <param name="newThemeName">Name of the theme to load or <c>null</c>, if the default theme should be used.</param>
    public void SwitchSkinAndTheme(string newSkinName, string newThemeName)
    {
      lock (_syncObj)
      {
        ServiceRegistration.Get<ILogger>().Info("ScreenManager: Loading skin '{0}' with theme '{1}'",
            newSkinName, newThemeName);

        string currentScreenName = _currentScreen == null ? null : _currentScreen.ResourceName;
        string currentSuperLayerName = _currentSuperLayer == null ? null : _currentSuperLayer.ResourceName;
        Screen backgroundScreen = _backgroundData.BackgroundScreen;
        string currentBackgroundName = backgroundScreen == null ? null : backgroundScreen.ResourceName;

        UninstallBackgroundManager();
        _backgroundData.Unload();

        DoCloseCurrentScreenAndDialogs(true, true, false);

        PlayersHelper.ReleaseGUIResources();

        ServiceRegistration.Get<ContentManager>().Clear();

        WaitForPendingOperations();

        PrepareSkinAndTheme(newSkinName, newThemeName);
        PlayersHelper.ReallocGUIResources();

        if (!InstallBackgroundManager())
          _backgroundData.Load(currentBackgroundName);

        Screen screen = GetScreen(currentScreenName, ScreenType.ScreenOrDialog);
        DoExchangeScreen(screen);

        if (currentSuperLayerName != null)
        {
          Screen superLayer = GetScreen(currentSuperLayerName, ScreenType.SuperLayer);
          DoSetSuperLayer(superLayer);
        }
      }
      SkinSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<SkinSettings>();
      settings.Skin = SkinName;
      settings.Theme = ThemeName;
      ServiceRegistration.Get<ISettingsManager>().Save(settings);
    }

    /// <summary>
    /// Loads the root UI element for the specified screen from the current skin or any of its parent skins,
    /// in the defined resource search order.
    /// </summary>
    /// <param name="screenName">Name of the screen to load.</param>
    /// <param name="relativeScreenPath">The path of the screen to load, relative to the skin's root path.</param>
    /// <param name="loader">Loader used for GUI models.</param>
    /// <returns>Root UI element for the specified screen or <c>null</c>, if the screen
    /// is not defined in the current skin resource chain.</returns>
    public static Screen LoadScreen(string screenName, string relativeScreenPath, IModelLoader loader)
    {
      SkinResources resourceBundle;
      string skinFilePath = SkinContext.SkinResources.GetResourceFilePath(relativeScreenPath, true, out resourceBundle);
      if (skinFilePath == null)
      {
        ServiceRegistration.Get<ILogger>().Error("SkinResources: No skinfile for screen '{0}'", relativeScreenPath);
        return null;
      }
      ServiceRegistration.Get<ILogger>().Debug("Loading screen from file path '{0}'...", skinFilePath);
      object obj = XamlLoader.Load(skinFilePath, loader, true);
      Screen screen = obj as Screen;
      if (screen == null)
      {
        DependencyObject.TryDispose(ref obj);
        return null;
      }
      screen.Initialize(screenName, resourceBundle.SkinWidth, resourceBundle.SkinHeight);
      return screen;
    }

    /// <summary>
    /// Gets a screen or background screen with the specified name.
    /// </summary>
    /// <remarks>
    /// If the desired screen could not be loaded (because it is not present or because an error occurs while
    /// loading the screen), an error notification is shown to the user.
    /// </remarks>
    /// <param name="screenName">Name of the screen to return.</param>
    /// <param name="screenType">Type of the screen to load. Depending on that type, the screen will be searched
    /// in the appropriate directory.</param>
    /// <returns>Root UI element of the desired screen or <c>null</c>, if an error occured while
    /// loading the screen.</returns>
    public static Screen GetScreen(string screenName, ScreenType screenType)
    {
      return GetScreen(screenName, new WorkflowManagerModelLoader(), screenType);
    }

    public static Screen GetScreen(string screenName, IModelLoader loader, ScreenType screenType)
    {
      try
      {
        string relativeDirectory;
        switch (screenType)
        {
          case ScreenType.ScreenOrDialog:
            relativeDirectory = SkinResources.SCREENS_DIRECTORY;
            break;
          case ScreenType.Background:
            relativeDirectory = SkinResources.BACKGROUNDS_DIRECTORY;
            break;
          case ScreenType.SuperLayer:
            relativeDirectory = SkinResources.SUPER_LAYERS_DIRECTORY;
            break;
          default:
            throw new NotImplementedException(string.Format("Screen type {0} is unknown", screenType));
        }
        string relativeScreenPath = relativeDirectory + Path.DirectorySeparatorChar + screenName + ".xaml";
        Screen result = LoadScreen(screenName, relativeScreenPath, loader);
        if (result == null)
        {
          ServiceRegistration.Get<ILogger>().Error("ScreenManager: Cannot load screen '{0}'", screenName);
          string errorText;
          switch (screenType)
          {
            case ScreenType.ScreenOrDialog:
              errorText = RES_SCREEN_MISSING_TEXT;
              break;
            case ScreenType.Background:
              errorText = RES_BACKGROUND_SCREEN_MISSING_TEXT;
              break;
            case ScreenType.SuperLayer:
              errorText = RES_SUPER_LAYER_MISSING_TEXT;
              break;
            default:
              throw new NotImplementedException(string.Format("Screen type {0} is unknown", screenType));
          }
          ServiceRegistration.Get<INotificationService>().EnqueueNotification(NotificationType.Error,
              RES_ERROR_LOADING_SKIN_RESOURCE_TEXT_RES,
              LocalizationHelper.CreateResourceString(errorText).Evaluate(screenName), true);
          return null;
        }
        return result;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error("ScreenManager: Error loading skin file for screen '{0}'", ex, screenName);
        try
        {
          string errorText;
          switch (screenType)
          {
            case ScreenType.ScreenOrDialog:
              errorText = RES_SCREEN_BROKEN_TEXT;
              break;
            case ScreenType.Background:
              errorText = RES_BACKGROUND_SCREEN_BROKEN_TEXT;
              break;
            case ScreenType.SuperLayer:
              errorText = RES_SUPER_LAYER_BROKEN_TEXT;
              break;
            default:
              throw new NotImplementedException(string.Format("Screen type {0} is unknown", screenType));
          }
          ServiceRegistration.Get<INotificationService>().EnqueueNotification(NotificationType.Error,
              RES_ERROR_LOADING_SKIN_RESOURCE_TEXT_RES,
              LocalizationHelper.CreateResourceString(errorText).Evaluate(screenName), true);
        }
        catch (Exception)
        {
          ServiceRegistration.Get<ILogger>().Error("ScreenManager: Error showing generic dialog for error message");
          return null;
        }
        return null;
      }
    }

    public DialogData CurrentDialogData
    {
      get
      {
        lock (_syncObj)
          return _dialogStack.Peek();
      }
    }

    public string CurrentDialogName
    {
      get
      {
        DialogData dd = CurrentDialogData;
        return dd == null ? null : dd.DialogScreen.ResourceName;
      }
    }

    public Screen FocusedScreen
    {
      get
      {
        lock (_syncObj)
          return _focusedScreen;
      }
    }

    public void ResetSize()
    {
      ForEachScreen(screen => screen.ResetSize());
    }

    public DialogData ShowDialogEx(string dialogName, DialogCloseCallbackDlgt dialogCloseCallback)
    {
      IncPendingOperations();
      ServiceRegistration.Get<ILogger>().Debug("ScreenManager: Preparing to show dialog '{0}'...", dialogName);
      Screen newDialog = GetScreen(dialogName, ScreenType.ScreenOrDialog);
      if (newDialog == null)
        return null;
      DialogData result = new DialogData(newDialog, dialogCloseCallback);
      ScreenManagerMessaging.SendMessageShowDialog(result);
      return result;
    }

    protected internal void ScreenChanging(string newScreenName)
    {
      Screen screen = _currentScreen;
      if (newScreenName == null || (screen != null && screen.ResourceName != newScreenName))
      {
        IncPendingOperations();
        ScreenManagerMessaging.SendMessageScreenClosing(screen);
      }
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
          return CurrentDialogName ?? (_currentScreen == null ? null : _currentScreen.ResourceName);
      }
    }

    public string ActiveBackgroundScreenName
    {
      get
      {
        // No locking necessary
        Screen backgroundScreen = _backgroundData.BackgroundScreen;
        return backgroundScreen == null ? null : backgroundScreen.ResourceName;
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

    public bool IsSuperLayerVisible
    {
      get
      {
        lock (_syncObj)
          return _currentSuperLayer != null;
      }
    }

    public IList<IDialogData> DialogStack
    {
      get
      {
        List<IDialogData> result = new List<IDialogData>();
        lock (_syncObj)
          // Albert: The copying procedure can be removed when we switch to .net 4.0
          result.AddRange(_dialogStack.Cast<IDialogData>());
        return result;
      }
    }

    public Guid? TopmostDialogInstanceId
    {
      get
      {
        lock (_syncObj)
        {
          if (_dialogStack.Count == 0)
            return null;
          return _dialogStack.Peek().DialogScreen.ScreenInstanceId;
        }
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
          if (_backgroundDisabled == value)
            return;
          ServiceRegistration.Get<ILogger>().Debug(value ?
              "ScreenManager: Disabling background screen rendering" :
              "ScreenManager: Enabling background screen rendering");
          _backgroundDisabled = value;
        }
      }
    }

    public void SwitchTheme(string newThemeName)
    {
      SwitchSkinAndTheme(null, newThemeName);
    }

    public void SwitchSkin(string newSkinName)
    {
      SwitchSkinAndTheme(newSkinName, null);
    }

    public void ReloadSkinAndTheme()
    {
      SwitchSkinAndTheme(_skin.Name, _theme == null ? null : _theme.Name);
    }

    public Guid? ShowScreen(string screenName)
    {
      return ShowScreen(screenName, true);
    }

    public Guid? ShowScreen(string screenName, bool backgroundEnabled)
    {
      ScreenChanging(screenName);

      ServiceRegistration.Get<ILogger>().Debug("ScreenManager: Preparing to show screen '{0}'...", screenName);
      Screen newScreen = GetScreen(screenName, ScreenType.ScreenOrDialog);
      if (newScreen == null)
          // Error message was shown in GetScreen()
        return null;

      newScreen.HasBackground = backgroundEnabled;
      IncPendingOperations();
      ScreenManagerMessaging.SendMessageShowScreen(newScreen, true);
      return newScreen.ScreenInstanceId;
    }

    public bool ExchangeScreen(string screenName)
    {
      return ExchangeScreen(screenName, true);
    }

    public bool ExchangeScreen(string screenName, bool backgroundEnabled)
    {
      ScreenChanging(screenName);

      ServiceRegistration.Get<ILogger>().Debug("ScreenManager: Preparing to show screen '{0}'...", screenName);
      Screen newScreen = GetScreen(screenName, ScreenType.ScreenOrDialog);
      if (newScreen == null)
          // Error message was shown in GetScreen()
        return false;

      newScreen.HasBackground = backgroundEnabled;
      IncPendingOperations();
      ScreenManagerMessaging.SendMessageShowScreen(newScreen, false);
      return true;
    }

    public Guid? ShowDialog(string dialogName)
    {
      return ShowDialog(dialogName, null);
    }

    public Guid? ShowDialog(string dialogName, DialogCloseCallbackDlgt dialogCloseCallback)
    {
      DialogData dd = ShowDialogEx(dialogName, dialogCloseCallback);
      return dd == null ? new Guid?() : dd.DialogScreen.ScreenInstanceId;
    }

    public void CloseDialog(Guid dialogInstanceId)
    {
      IncPendingOperations();
      ServiceRegistration.Get<ILogger>().Debug("ScreenManager: Preparing to close dialog '{0}'", dialogInstanceId);
      ScreenManagerMessaging.SendMessageCloseDialogs(dialogInstanceId, CloseDialogsMode.CloseSingleDialog);
    }

    public void CloseDialogs(Guid dialogInstanceId, bool inclusive)
    {
      IncPendingOperations();
      ServiceRegistration.Get<ILogger>().Debug("ScreenManager: Preparing to close dialog '{0}' and all dialogs on top of it", dialogInstanceId);
      ScreenManagerMessaging.SendMessageCloseDialogs(dialogInstanceId, inclusive ? CloseDialogsMode.CloseAllOnTopIncluding : CloseDialogsMode.CloseAllOnTopExcluding);
    }

    public void CloseTopmostDialog()
    {
      IncPendingOperations();
      Guid? dialogInstanceId = TopmostDialogInstanceId;
      if (dialogInstanceId.HasValue)
      {
        ServiceRegistration.Get<ILogger>().Debug("ScreenManager: Preparing to close dialog '{0}'", dialogInstanceId);
        ScreenManagerMessaging.SendMessageCloseDialogs(dialogInstanceId.Value, CloseDialogsMode.CloseSingleDialog);
      }
      else
        ServiceRegistration.Get<ILogger>().Debug("ScreenManager: No dialog found to close");
    }

    public bool SetBackgroundLayer(string backgroundName)
    {
      ServiceRegistration.Get<ILogger>().Debug("ScreenManager: Setting background screen '{0}'...", backgroundName);
      // No locking necessary
      if (backgroundName == null)
      {
        _backgroundData.Unload();
        return true;
      }
      return _backgroundData.Load(backgroundName);
    }

    public bool SetSuperLayer(string superLayerName)
    {
      IncPendingOperations();
      if (superLayerName == null)
      {
        ServiceRegistration.Get<ILogger>().Debug("ScreenManager: Preparing to hide super layer...");
        ScreenManagerMessaging.SendMessageSetSuperLayer(null);
        return true;
      }
      ServiceRegistration.Get<ILogger>().Debug("ScreenManager: Preparing to show super layer '{0}'...", superLayerName);
      Screen newScreen = GetScreen(superLayerName, ScreenType.SuperLayer);
      if (newScreen == null)
          // Error message was shown in GetScreen()
        return false;
      ScreenManagerMessaging.SendMessageSetSuperLayer(newScreen);
      return true;
    }

    public void Reload()
    {
      IncPendingOperations();
      ServiceRegistration.Get<ILogger>().Debug("ScreenManager: Preparing to reload screens");
      ScreenManagerMessaging.SendMessageReloadScreens();
    }

    public void StartBatchUpdate()
    {
      // TODO:
      // - Create class BatchUpdateCache and field _batchUpdateCache. Use field BUC._numBatchUpdates to support stacking.
      // - implement collection of all screen/dialog change events in methods above
      // - ensure that also dialog close events are executed in the correct order
      //   -> probably the BUC needs a complicated structure to hold the (rectified) sequence of screen updates,
      //      we probably need to hand the BUC structure to the async executor where we do the complete batch update at once
    }

    public void EndBatchUpdate()
    {
      // TODO
      // See comment in StartBatchUpdate()
    }

    #endregion
  }
}
