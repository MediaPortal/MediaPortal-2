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
using System.IO;
using System.Linq;
using System.Threading;
using MediaPortal.Common.Messaging;
using MediaPortal.Common.PluginManager;
using MediaPortal.Common.Localization;
using MediaPortal.UI.Presentation.Screens;
using MediaPortal.UI.Presentation.SkinResources;
using MediaPortal.UI.Presentation.UiNotifications;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.Settings;
using MediaPortal.UI.SkinEngine.DirectX;
using MediaPortal.UI.SkinEngine.MpfElements;
using MediaPortal.UI.SkinEngine.Settings;
using MediaPortal.UI.SkinEngine.SkinManagement;
using MediaPortal.UI.SkinEngine.Utils;

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

  public class ScreenManager : IScreenManager, IDisposable
  {
    #region Consts

    public const string HOME_SCREEN = "home";

    public const string RES_ERROR_LOADING_SKIN_TITLE = "[ScreenManager.NotificationErrorLoadingSkinTitle]";
    public const string RES_ERROR_LOADING_SKIN_FALLING_BACK_TO_CURRENT_TEXT = "[ScreenManager.NotificationErrorLoadingSkinFallingBackToCurrentText]";
    public const string RES_ERROR_LOADING_SKIN_FALLING_BACK_TO_DEFAULT_TEXT = "[ScreenManager.NotificationErrorLoadingSkinFallingBackToDefaultText]";
    public const string RES_ERROR_LOADING_SKIN_RESOURCE_TEXT = "[ScreenManager.ErrorLoadingSkinResource]";
    public const string RES_SCREEN_MISSING_TEXT = "[ScreenManager.ScreenMissing]";
    public const string RES_SCREEN_BROKEN_TEXT = "[ScreenManager.ScreenBroken]";
    public const string RES_BACKGROUND_SCREEN_MISSING_TEXT = "[ScreenManager.BackgroundScreenMissing]";
    public const string RES_SUPER_LAYER_MISSING_TEXT = "[ScreenManager.SuperLayerScreenMissing]";
    public const string RES_BACKGROUND_SCREEN_BROKEN_TEXT = "[ScreenManager.BackgroundScreenBroken]";
    public const string RES_SUPER_LAYER_BROKEN_TEXT = "[ScreenManager.SuperLayerScreenBroken]";

    public const string MODELS_REGISTRATION_LOCATION = "/Models";

    protected static readonly TimeSpan TIMESPAN_INFINITE = TimeSpan.FromMilliseconds(-1);

    #endregion

    #region Classes, structs, delegates & enums

    public class ScreenManagerMemento
    {
      public string CurrentScreenName;
      public string CurrentSuperLayerName;
      public string CurrentBackgroundName;
    }

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
        Screen background = _parent.GetScreen(backgroundName, this, ScreenType.Background);
        if (background == null)
          return false;
        background.Prepare();
        background.TriggerScreenShowingEvent();
        lock (_parent.SyncObj)
          _backgroundScreen = background;
        return true;
      }

      public void Unload()
      {
        ICollection<Guid> oldModels;
        lock (_parent.SyncObj)
        {
          if (_backgroundScreen == null)
            return;
          Screen oldBackground = _backgroundScreen;
          _backgroundScreen = null;
          oldBackground.ScreenState = Screen.State.Closing;
          oldBackground.TriggerScreenClosingEvent();
          // For backgrounds, we don't wait for DoneClosing. Backgrounds should close at once. Else, we would need to
          // make the background handling asynchronous.
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
        lock (_parent.SyncObj)
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
        lock (_parent.SyncObj)
          if (_models.TryGetValue(modelId, out result))
            return result;
        result = ServiceRegistration.Get<IPluginManager>().RequestPluginItem<object>(
            MODELS_REGISTRATION_LOCATION, modelId.ToString(), this);
        if (result == null)
          throw new ArgumentException(string.Format("ScreenManager: Model with id '{0}' is not available", modelId));
        lock (_parent.SyncObj)
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

      public override string ToString()
      {
        return _dialogName + "(" + _dialogId + ")";
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

    /// <summary>
    /// Synchronization object for field access.
    /// </summary>
    /// <remarks>
    /// In the rendering subsystem, sometimes we break the MP2 multithreading guidelines; We DO hold multiple locks sometimes.
    /// This lock should be used to synchronize field access but not during the rendering of screens.
    /// </remarks>
    protected readonly object _syncObj = new object();
    protected ManualResetEvent _terminatedEvent = new ManualResetEvent(false);
    protected AutoResetEvent _garbageScreensAvailable = new AutoResetEvent(false);
    protected AutoResetEvent _pendingOperationsDecreasedEvent = new AutoResetEvent(false);
    protected ManualResetEvent _garbageCollectionFinished = new ManualResetEvent(true);
    protected ManualResetEvent _renderFinished = new ManualResetEvent(true);

    // Initialized in constructor
    protected readonly SkinManager _skinManager;
    protected readonly BackgroundData _backgroundData;

    protected readonly DialogStackList _dialogStack = new DialogStackList();
    protected Screen _currentScreen = null; // "Normal" screen
    protected Screen _nextScreen = null; // Holds the next screen while the current screen finishes closing
    protected Screen _currentSuperLayer = null; // Layer on top of screen and all dialogs - busy indicator and additional popups
    protected volatile int _numPendingAsyncOperations = 0;
    protected Screen _focusedScreen = null;

    protected bool _backgroundDisabled = false;

    protected Skin _skin = null;
    protected Theme _theme = null;

    protected AsynchronousMessageQueue _messageQueue = null;
    protected Thread _garbageCollectorThread = null;
    protected Queue<Screen> _garbageScreens = new Queue<Screen>(10);

    #endregion

    public ScreenManager()
    {
      _skinManager = new SkinManager();
      _backgroundData = new BackgroundData(this);
    }

    public void Dispose()
    {
      _terminatedEvent.Set();
      _garbageCollectionFinished.WaitOne(2000);
      _terminatedEvent.Close();
      _garbageScreensAvailable.Close();
      _pendingOperationsDecreasedEvent.Close();
      _garbageCollectionFinished.Close();
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
            DoShowScreen_NoLock(screen, closeDialogs);
            DecPendingOperations();
            break;
          case ScreenManagerMessaging.MessageType.SetSuperLayer:
            screen = (Screen) message.MessageData[ScreenManagerMessaging.SCREEN];
            DoSetSuperLayer_NoLock(screen);
            DecPendingOperations();
            break;
          case ScreenManagerMessaging.MessageType.ShowDialog:
            DialogData dialogData = (DialogData) message.MessageData[ScreenManagerMessaging.DIALOG_DATA];
            DoShowDialog_NoLock(dialogData);
            DecPendingOperations();
            break;
          case ScreenManagerMessaging.MessageType.CloseDialogs:
            Guid dialogInstanceId = (Guid) message.MessageData[ScreenManagerMessaging.DIALOG_INSTANCE_ID];
            CloseDialogsMode mode = (CloseDialogsMode) message.MessageData[ScreenManagerMessaging.CLOSE_DIALOGS_MODE];
            DoCloseDialogs_NoLock(dialogInstanceId, mode, true, true);
            DecPendingOperations();
            break;
          case ScreenManagerMessaging.MessageType.ReloadScreens:
            DoReloadScreens_NoLock();
            DecPendingOperations();
            break;
          case ScreenManagerMessaging.MessageType.SwitchSkinAndTheme:
            string newSkinName = (string) message.MessageData[ScreenManagerMessaging.SKIN_NAME];
            string newThemeName = (string) message.MessageData[ScreenManagerMessaging.THEME_NAME];
            DoSwitchSkinAndTheme_NoLock(newSkinName, newThemeName);
            DecPendingOperations();
            break;
          case ScreenManagerMessaging.MessageType.SetBackgroundDisabled:
            bool disabled = (bool) message.MessageData[ScreenManagerMessaging.IS_DISABLED];
            DoSetBackgroundDisabled_NoLock(disabled);
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
        WaitHandle.WaitAny(new WaitHandle[] {_terminatedEvent, _garbageScreensAvailable}); // Only run if garbage screens are available
        WaitHandle.WaitAny(new WaitHandle[] {_terminatedEvent, _renderFinished}); // If currently rendering, wait once before we can be sure that no screen is rendered any more
        
        // The render thread doesn't need to wait for the garbage collector thread, so it's enough to reset the garbage collector finished event after waiting
        // for the render thread (if the render thread would need to be blocked by the garbage collector, we would have to reset the garbage collector finished
        // event before waiting for the render thread finished event above).
        _garbageCollectionFinished.Reset();
        Screen screen;
        bool active = true;
        do
        {
          lock (_syncObj)
            screen = _garbageScreens.Count == 0 ? null : _garbageScreens.Dequeue();
          if (screen == null)
            active = false;
          else
          {
            screen.Close();
            screen.Dispose();
          }
        } while (active);
        _garbageCollectionFinished.Set();
        if (_terminatedEvent.WaitOne(0))
          break;
      }
    }

    protected void WaitForGarbageCollection()
    {
      _garbageCollectionFinished.WaitOne(TIMESPAN_INFINITE);
    }

    protected void FinishGarbageCollection()
    {
      lock (_syncObj)
        _garbageCollectorThread.Priority = ThreadPriority.AboveNormal;
      _terminatedEvent.Set();
      ServiceRegistration.Get<ILogger>().Debug("ScreenManager: Waiting for screen garbage collection...");
      _garbageCollectorThread.Join();
      ServiceRegistration.Get<ILogger>().Debug("ScreenManager: Screen garbage collection finished");
    }

    protected void ScheduleDisposeScreen(Screen screen)
    {
      lock (_syncObj)
        _garbageScreens.Enqueue(screen);
      _garbageScreensAvailable.Set();
    }

    /// <summary>
    /// Increments the number of async operation which are pending and being executed via our async message loop.
    /// </summary>
    /// <remarks>
    /// This method has to be called before ANY call to methods of class <see cref="ScreenManagerMessaging"/>.
    /// </remarks>
    protected internal void IncPendingOperations()
    {
      Interlocked.Increment(ref _numPendingAsyncOperations);
    }

    /// <summary>
    /// Decrements the number of async operation which are pending and being executed via our async message loop.
    /// </summary>
    /// <remarks>
    /// This method has to be called after EACH handling of a <see cref="ScreenManagerMessaging"/> message.
    /// </remarks>
    protected internal void DecPendingOperations()
    {
      Interlocked.Decrement(ref _numPendingAsyncOperations);
      _pendingOperationsDecreasedEvent.Set();
    }

    /// <summary>
   /// Waits until all screens pending to be hidden are hidden and all screens pending to be shown are shown.
   /// </summary>
    protected internal void WaitForPendingOperations()
    {
      while (_numPendingAsyncOperations > 0)
        // Wait until all outstanding async operations have been executed
        _pendingOperationsDecreasedEvent.WaitOne(TIMESPAN_INFINITE);
    }

    public void ExecuteWithTempReleasedResources(Action action)
    {
      GraphicsDevice.RenderAndResourceAccessLock.EnterWriteLock(); // Avoid rendering during DX initialization
      ScreenManagerMemento memento;
      try
      {
        memento = TempClean_OnlyRenderLock();
        action();
      }
      finally
      {
        GraphicsDevice.RenderAndResourceAccessLock.ExitWriteLock();
      }
      Recreate_NoLock(memento);
    }

    protected ScreenManagerMemento TempClean_OnlyRenderLock()
    {
      ScreenManagerMemento memento;
      lock (_syncObj)
      {
        if (_currentScreen == null)
          // Not yet initialized
          return null;
        Screen backgroundScreen = _backgroundData.BackgroundScreen;
        memento = new ScreenManagerMemento
          {
              CurrentScreenName = _currentScreen.ResourceName,
              CurrentSuperLayerName = _currentSuperLayer == null ? null : _currentSuperLayer.ResourceName,
              CurrentBackgroundName = backgroundScreen == null ? null : backgroundScreen.ResourceName
          };

        UninstallBackgroundManager();
        _backgroundData.Unload();
      }

      // Potential deadlock situation... But we cannot release our lock since noone is allowed to access the skin resources during skin/theme update.
      DoCloseCurrentScreenAndDialogs_NoLock(true, true, false);
      WaitForGarbageCollection();
      return memento;
    }

    protected void Recreate_NoLock(ScreenManagerMemento memento)
    {
      if (memento == null)
        return;
      if (!InstallBackgroundManager())
        _backgroundData.Load(memento.CurrentBackgroundName);
      Screen screen = GetScreen(memento.CurrentScreenName, ScreenType.ScreenOrDialog);
      DoExchangeScreen_NoLock(screen);
      if (memento.CurrentSuperLayerName != null)
      {
        Screen superLayer = GetScreen(memento.CurrentSuperLayerName, ScreenType.SuperLayer);
        DoSetSuperLayer_NoLock(superLayer);
      }
    }

    public void DoSwitchSkinAndTheme_NoLock(string newSkinName, string newThemeName)
    {
      ServiceRegistration.Get<ILogger>().Info("ScreenManager: Switching skin and theme");

      ScreenManagerMemento memento;

      // Suspend both rendering and resource access to avoid the render thread rendering screens which are being closed here and
      // to block other threads accessing our skin resources (via indirect GetScreen calls)
      GraphicsDevice.RenderAndResourceAccessLock.EnterWriteLock();
      try
      {
        memento = TempClean_OnlyRenderLock();

        lock (_syncObj)
        {
          UIResourcesHelper.ReleaseUIResources();

          PrepareSkinAndTheme(newSkinName, newThemeName);

          UIResourcesHelper.ReallocUIResources();
        }
      }
      finally
      {
        // Resume resource access before we reload everything below
        GraphicsDevice.RenderAndResourceAccessLock.ExitWriteLock();
      }
      Recreate_NoLock(memento);
      SkinSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<SkinSettings>();
      settings.Skin = SkinName;
      settings.Theme = ThemeName;
      ServiceRegistration.Get<ISettingsManager>().Save(settings);
    }

    protected void DoSetBackgroundDisabled_NoLock(bool disabled)
    {
      ServiceRegistration.Get<ILogger>().Debug(disabled ?
          "ScreenManager: Disabling background screen rendering" :
          "ScreenManager: Enabling background screen rendering");
      lock (_syncObj)
        _backgroundDisabled = disabled;
    }

    /// <summary>
    /// Prepares the skin and theme, this will load the skin and theme instances and set it as the current skin and theme
    /// in the <see cref="SkinContext"/>.
    /// After calling this method, the <see cref="SkinContext.SkinResources"/> contents can be requested.
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
          // Fall back to current skin/theme
          ServiceRegistration.Get<ILogger>().Error("ScreenManager: Error loading skin '{0}', theme '{1}', falling back to previous skin/theme",
              ex, skinName, themeName);
          ServiceRegistration.Get<INotificationService>().EnqueueNotification(NotificationType.Error, RES_ERROR_LOADING_SKIN_TITLE,
              RES_ERROR_LOADING_SKIN_FALLING_BACK_TO_CURRENT_TEXT, true);
          skin = _skin;
          theme = _theme;
        }

        // Try to apply new skin/theme
        try
        {
          SkinResources skinResources = theme == null ? skin : (SkinResources) theme;
          ServiceRegistration.Get<ILogger>().Info("ScreenManager: Applying skin '{0}', theme '{1}'",
              skin == null ? string.Empty : skin.Name, theme == null ? string.Empty : theme.Name);

          _skinManager.InstallSkinResources(skinResources);

          _skin = skin;
          _theme = theme;
        }
        catch (Exception ex)
        {
          // Didn't work - try fallback skin/theme
          ServiceRegistration.Get<ILogger>().Error("ScreenManager: Error applying skin '{0}', theme '{1}'",
              ex, skin == null ? "<undefined>" : skin.Name, theme == null ? "<undefined>" : theme.Name);
          Skin fallbackSkin = _skin ?? defaultSkin; // Either the previous skin (_skin) or the default skin
          Theme fallbackTheme = fallbackSkin.DefaultTheme;
          if (fallbackSkin == skin && fallbackTheme == theme)
          {
            ServiceRegistration.Get<ILogger>().Critical("ScreenManager: There is no valid skin to show");
            // No notification necessary here - we don't have a skin to show
            throw;
          }
          ServiceRegistration.Get<INotificationService>().EnqueueNotification(NotificationType.Error, RES_ERROR_LOADING_SKIN_TITLE,
              RES_ERROR_LOADING_SKIN_FALLING_BACK_TO_DEFAULT_TEXT, true);
          return PrepareSkinAndTheme(fallbackSkin.Name, fallbackTheme == null ? null : fallbackTheme.Name);
        }
      }
      return true;
    }

    protected internal void DoShowScreen_NoLock(Screen screen, bool closeDialogs)
    {
      ServiceRegistration.Get<ILogger>().Debug("ScreenManager: Showing screen '{0}'", screen.ResourceName);
      DoStartClosingScreen_NoLock(_currentScreen);
      if (closeDialogs)
        DoCloseDialogs_NoLock(true, false);
      DoExchangeScreen_NoLock(screen);
    }

    public void SetInputFocus_NoLock()
    {
      Screen focusScreen = GetScreens(false, true, true, false).LastOrDefault();
      Screen unfocusScreen;
      lock (_syncObj)
        if (focusScreen == _focusedScreen)
          return;
        else
        {
          unfocusScreen = _focusedScreen;
          _focusedScreen = focusScreen;
        }
      // Don't hold the ScreenManager's lock while calling the next methods - they call event handlers
      if (unfocusScreen != null)
        unfocusScreen.DetachInput();
      if (focusScreen != null)
        focusScreen.AttachInput();
    }

    public void UnfocusCurrentScreen_NoLock()
    {
      UnfocusScreen_NoLock(_focusedScreen);
    }

    public void UnfocusScreen_NoLock(Screen screen)
    {
      lock (_syncObj)
      {
        if (screen != _focusedScreen)
          return;
        _focusedScreen = null;
      }
      if (screen != null)
        // Don't hold the ScreenManager's lock while calling this - it calls event handlers
        screen.DetachInput();
    }

    protected internal void DoShowDialog_NoLock(DialogData dialogData)
    {
      ServiceRegistration.Get<ILogger>().Debug("ScreenManager: Showing dialog '{0}'", dialogData.DialogName);
      dialogData.DialogScreen.Prepare();
      dialogData.DialogScreen.TriggerScreenShowingEvent();

      lock (_syncObj)
      {
        DialogData dd = dialogData;
        _dialogStack.Push(dd);
      }
      // Don't hold the lock while focusing the screen
      SetInputFocus_NoLock();
    }

    protected internal void DoCloseDialogs_NoLock(bool fireCloseDelegates, bool dialogPersistence)
    {
      DoCloseDialogs_NoLock(null, CloseDialogsMode.CloseAllOnTopIncluding, fireCloseDelegates, dialogPersistence);
    }

    protected internal void DoCloseDialogs_NoLock(Guid? dialogInstanceId, CloseDialogsMode mode, bool fireCloseDelegates, bool dialogPersistence)
    {
      if (dialogInstanceId.HasValue)
        if (mode == CloseDialogsMode.CloseSingleDialog)
          ServiceRegistration.Get<ILogger>().Debug("ScreenManager: Closing dialog with dialog id '{0}'", dialogInstanceId.Value);
        else
          ServiceRegistration.Get<ILogger>().Debug("ScreenManager: Closing all dialogs dialogs until dialog with dialog id '{0}' ({1})", dialogInstanceId.Value, mode == CloseDialogsMode.CloseAllOnTopIncluding ? "including" : "excluding");
      ICollection<DialogData> oldDialogData = new List<DialogData>();
      LinkedListNode<DialogData> bottomDialogNode;
      lock(_syncObj)
      {
        bottomDialogNode = dialogInstanceId.HasValue ? FindDialogNode(dialogInstanceId.Value) : _dialogStack.Last;
        if (bottomDialogNode == null)
        {
          if (dialogInstanceId.HasValue)
            ServiceRegistration.Get<ILogger>().Warn("ScreenManager.DoCloseDialogs: Dialog to close with dialog instance id '{0}' was not found on the dialog stack", dialogInstanceId.Value);
          return;
        }

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
        DoCloseDialog_NoLock(dd, fireCloseDelegates, dialogPersistence);

      CompleteDialogClosures_NoLock();
    }

    protected internal void DoCloseDialog_NoLock(DialogData dd, bool fireCloseDelegates, bool dialogPersistence)
    {
      ServiceRegistration.Get<ILogger>().Debug("ScreenManager: Closing dialog '{0}'", dd.DialogName);
      Screen oldDialog = dd.DialogScreen;
      if (dialogPersistence)
      {
        oldDialog.ScreenState = Screen.State.Closing;
        oldDialog.TriggerScreenClosingEvent();
      }
      else
      {
        ScheduleDisposeScreen(oldDialog);
      }
        
      if (fireCloseDelegates && dd.CloseCallback != null)
        dd.CloseCallback(dd.DialogScreen.ResourceName, dd.DialogInstanceId);
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

    protected internal void CompleteDialogClosures_NoLock()
    {
      lock (_syncObj)
      {
        LinkedListNode<DialogData> node = _dialogStack.Last;
        while (node != null)
        {
          Screen screen = node.Value.DialogScreen;
          if (screen.ScreenState == Screen.State.Closing && screen.DoneClosing)
          {
            LinkedListNode<DialogData> prevNode = node.Previous;
            ScheduleDisposeScreen(screen);
            _dialogStack.Remove(node);
            node = prevNode;
            continue;
          }
          node = node.Previous;
        }
      }
      SetInputFocus_NoLock();
    }

    protected internal void DoCloseScreen_NoLock(Screen screen)
    {
      ServiceRegistration.Get<ILogger>().Debug("ScreenManager: Closing screen '{0}'", screen.ResourceName);
      UnfocusScreen_NoLock(screen);
      ScheduleDisposeScreen(screen);
    }

    protected internal void DoCloseCurrentScreenAndDialogs_NoLock(bool closeBackgroundLayer, bool closeSuperLayer, bool fireCloseDelegates)
    {
      if (closeBackgroundLayer)
        _backgroundData.Unload();
      if (closeSuperLayer)
        DoSetSuperLayer_NoLock(null);
      DoCloseDialogs_NoLock(fireCloseDelegates, false);
      Screen currentScreen;
      lock (_syncObj)
      {
        currentScreen = _currentScreen;
        _currentScreen = null;
        if (currentScreen == null)
          return;
      }
      DoCloseScreen_NoLock(currentScreen);
    }

    protected internal void DoStartClosingScreen_NoLock(Screen screen)
    {
      Screen currentScreen;
      lock (_syncObj)
      {
        if (_currentScreen == null || _currentScreen.ScreenState == Screen.State.Closing || 
            screen == null || screen != _currentScreen)
          return;
        currentScreen = _currentScreen;
      }
      DoCloseDialogs_NoLock(true, true);
      currentScreen.ScreenState = Screen.State.Closing;
      currentScreen.TriggerScreenClosingEvent_Sync();
      UnfocusScreen_NoLock(screen);
    }

    protected internal void DoExchangeScreen_NoLock(Screen screen)
    {
      lock (_syncObj)
      {
        if (_nextScreen != null)
        { // If next screen is already set, dispose it. This might happen if during a screen change, another screen change is scheduled.
          ScheduleDisposeScreen(_nextScreen);
        }
        if (_currentScreen != null)
          _currentScreen.ScreenState = Screen.State.Closing;

        _nextScreen = screen;
      }
      screen.Prepare();
    }

    protected internal void CompleteScreenClosure_NoLock()
    {
      Screen nextScreen;
      Screen currentScreen;
      lock (_syncObj)
      {
        // Has the current screen finished closing?
        if (_currentScreen != null && (_currentScreen.ScreenState != Screen.State.Closing || !_currentScreen.DoneClosing))
          return;

        nextScreen = _nextScreen;
        currentScreen = _currentScreen;
        _currentScreen = nextScreen;
        _nextScreen = null;
        BackgroundDisabled = _currentScreen != null && !_currentScreen.HasBackground;
      }
      if (nextScreen != null)
        // Outside the lock - we're firing events
        nextScreen.TriggerScreenShowingEvent();
      if (currentScreen != null)
        DoCloseScreen_NoLock(currentScreen);
      SetInputFocus_NoLock();
    }

    protected internal void DoSetSuperLayer_NoLock(Screen screen)
    {
      if (screen == null)
        ServiceRegistration.Get<ILogger>().Debug("ScreenManager: Hiding superlayer");
      else
      {
        ServiceRegistration.Get<ILogger>().Debug("ScreenManager: Showing superlayer '{0}'", screen.ResourceName);
        screen.Prepare();
      }
      lock (_syncObj)
      {
        if (_currentSuperLayer != null)
        {
          _currentSuperLayer.ScreenState = Screen.State.Closing;
          // Super layers must close at once, we don't wait for DoneClosing. Else, we would need to make
          // the superlayer handling asynchronous.
          ScheduleDisposeScreen(_currentSuperLayer);
        }
        _currentSuperLayer = screen;
      }
      if (screen == null)
        SetInputFocus_NoLock();
      else
        UnfocusCurrentScreen_NoLock();
    }

    protected internal void DoReloadScreens_NoLock()
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
        screenName = _currentScreen == null ? null : _currentScreen.ResourceName;
        superLayerName = _currentSuperLayer == null ? null : _currentSuperLayer.ResourceName;
        // Remember all dialogs and their close callbacks
        dialogsReverse = new List<DialogSaveDescriptor>(
            _dialogStack.Select(dd => new DialogSaveDescriptor(dd.DialogScreen.ResourceName, dd.DialogInstanceId, dd.CloseCallback)));
        dialogsReverse.Reverse();
      }

      // Close all
      DoCloseCurrentScreenAndDialogs_NoLock(true, true, false);

      // Reload background
      if (backgroundName != null)
        _backgroundData.Load(backgroundName);

      // Reload screen
      Screen screen = GetScreen(screenName, ScreenType.ScreenOrDialog);
      if (screen == null)
        // Error message was shown in GetScreen()
        return;
      DoExchangeScreen_NoLock(screen);

      // Reload dialogs
      foreach (DialogSaveDescriptor dialogDescriptor in dialogsReverse)
      {
        Screen dialog = GetScreen(dialogDescriptor.DialogName, ScreenType.ScreenOrDialog);
        dialog.ScreenInstanceId = dialogDescriptor.DialogId;
        DoShowDialog_NoLock(new DialogData(dialog, dialogDescriptor.CloseCallback));
      }

      if (superLayerName != null)
      {
        // Reload screen
        Screen superLayer = GetScreen(screenName, ScreenType.SuperLayer);
        if (superLayer == null)
            // Error message was shown in GetScreen()
          return;
        DoSetSuperLayer_NoLock(superLayer);
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
    /// Initializes all resources, loads skin and theme, starts the screen manager's threads.
    /// </summary>
    public void Startup()
    {
      ServiceRegistration.Get<ILogger>().Debug("ScreenManager: Startup");
      SkinSettings screenSettings = ServiceRegistration.Get<ISettingsManager>().Load<SkinSettings>();
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
      ServiceRegistration.Get<ILogger>().Debug("ScreenManager: Started");
    }

    /// <summary>
    /// Disposes all resources which were allocated by the screen manager.
    /// </summary>
    // Don't hold the ScreenManager's lock while calling this method; At least one _NoLock method is called inside here
    public void Shutdown()
    {
      ServiceRegistration.Get<ILogger>().Debug("ScreenManager: Shutting down");
      UnsubscribeFromMessages();
      // Close all screens to make sure all SharpDX objects are correctly cleaned up
      DoCloseCurrentScreenAndDialogs_NoLock(true, true, false);
      FinishGarbageCollection();

      _skinManager.Dispose();
      Fonts.FontManager.Unload();
    }

    public ISkinResourceManager SkinResourceManager
    {
      get { return _skinManager; }
    }

    public object SyncObj
    {
      get { return _syncObj; }
    }

    /// <summary>
    /// Renders the current screens (background, main and dialogs).
    /// </summary>
    // Don't hold the ScreenManager's lock while calling this method; At least one _NoLock method is called inside here
    public void Render()
    {
      SkinContext.FrameRenderingStartTime = DateTime.Now;

      // Check if we're waiting for screens to finish closing
      CompleteScreenClosure_NoLock();
      CompleteDialogClosures_NoLock();

      _renderFinished.Reset();
      try
      {
        IList<Screen> disabledScreens;
        IList<Screen> enabledScreens;
        lock (_syncObj)
        {
          disabledScreens = GetScreens(_backgroundDisabled, false, false, false);
          enabledScreens = GetScreens(!_backgroundDisabled, true, true, true);
        }
        foreach (Screen screen in disabledScreens)
        {
          screen.IsVisible = false;
          // Animation of disabled screens is necessary to avoid an overrun of the async properties setter buffer
          screen.SetValues();
        }
        foreach (Screen screen in enabledScreens)
        {
          screen.IsVisible = true;
          screen.SetValues();
          screen.Animate();
          screen.Render();
        }
      }
      finally
      {
        _renderFinished.Set();
      }
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
      Screen result = null;
      ISkinResourceBundle resourceBundle = SkinContext.SkinResources;
      while (result == null)
      {
        string skinFilePath = resourceBundle.GetResourceFilePath(relativeScreenPath, true, out resourceBundle);
        if (skinFilePath == null)
        {
          ServiceRegistration.Get<ILogger>().Error("ScreenManager: No skinfile for screen '{0}'", relativeScreenPath);
          return null;
        }
        ServiceRegistration.Get<ILogger>().Debug("ScreenManager: Loading screen from file path '{0}'...", skinFilePath);
        try
        {
          object obj = XamlLoader.Load(skinFilePath, resourceBundle, loader);
          result = obj as Screen;
          if (result == null)
          {
            if (obj != null)
              ServiceRegistration.Get<ILogger>().Warn("ScreenManager: XAML file '{0}' is expected to be a screen but the top-level element is a '{1}'. Try using a top-level 'Screen' element.", screenName, obj.GetType().Name);
            DependencyObject.TryDispose(ref obj);
          }
        }
        catch (Exception e)
        {
          ISkinResourceBundle inheritedBundle = resourceBundle.InheritedSkinResources;
          if (inheritedBundle == null)
          {
            ServiceRegistration.Get<ILogger>().Error("ScreenManager: Error loading screen file '{0}', no fallback screen available", e, skinFilePath);
            throw;
          }
          ServiceRegistration.Get<ILogger>().Error(
              "ScreenManager: Error loading screen '{0}' in resource bundle '{1}', falling back to resource bundle '{2}'",
              e, screenName, resourceBundle.Name, inheritedBundle);
          resourceBundle = inheritedBundle;
        }
      }
      result.Initialize(screenName);
      return result;
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
    public Screen GetScreen(string screenName, ScreenType screenType)
    {
      return GetScreen(screenName, new WorkflowManagerModelLoader(), screenType);
    }

    public Screen GetScreen(string screenName, IModelLoader loader, ScreenType screenType)
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

        Screen result;
        GraphicsDevice.RenderAndResourceAccessLock.EnterReadLock();
        try
        {
          result = LoadScreen(screenName, relativeScreenPath, loader);
        }
        finally
        {
          GraphicsDevice.RenderAndResourceAccessLock.ExitReadLock();
        }

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
              RES_ERROR_LOADING_SKIN_RESOURCE_TEXT, LocalizationHelper.CreateResourceString(errorText).Evaluate(screenName), true);
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
              RES_ERROR_LOADING_SKIN_RESOURCE_TEXT,
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

    public DialogData ShowDialogEx(string dialogName, DialogCloseCallbackDlgt dialogCloseCallback)
    {
      ServiceRegistration.Get<ILogger>().Debug("ScreenManager: Preparing to show dialog '{0}'...", dialogName);
      Screen newDialog = GetScreen(dialogName, ScreenType.ScreenOrDialog);
      if (newDialog == null)
        return null;
      DialogData result = new DialogData(newDialog, dialogCloseCallback);
      IncPendingOperations();
      ScreenManagerMessaging.SendMessageShowDialog(result);
      return result;
    }

    #region IScreenManager implementation

    public ISkinResourceBundle CurrentSkinResourceBundle
    {
      get { return _theme ?? (ISkinResourceBundle) _skin; }
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
          result.AddRange(_dialogStack);
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
        if (_backgroundDisabled == value)
          return;

        IncPendingOperations();
        ScreenManagerMessaging.SendSetBackgroundDisableMessage(value);
      }
    }

    public void SwitchSkinAndTheme(string newSkinName, string newThemeName)
    {
      ServiceRegistration.Get<ILogger>().Info("ScreenManager: Preparing to load skin '{0}' with theme '{1}'...", newSkinName, newThemeName);

      IncPendingOperations();
      ScreenManagerMessaging.SendMessageSwitchSkinAndTheme(newSkinName, newThemeName);
    }

    public void ReloadSkinAndTheme()
    {
      SwitchSkinAndTheme(_skin.Name, _theme == null ? null : _theme.Name);
    }

    public Guid? CheckScreen(string screenName)
    {
      lock (_syncObj)
      {
        Screen screen = _nextScreen ?? _currentScreen;
        if (screen != null && screen.ResourceName == screenName)
          return _currentScreen.ScreenInstanceId;
      }
      return ShowScreen(screenName, true);
    }

    public Guid? CheckScreen(string screenName, bool backgroundEnabled)
    {
      lock (_syncObj)
      {
        Screen screen = _nextScreen ?? _currentScreen;
        if (screen != null && screen.ResourceName == screenName)
        {
          BackgroundDisabled = !backgroundEnabled;
          return screen.ScreenInstanceId;
        }
      }
      return ShowScreen(screenName, backgroundEnabled);
    }

    public Guid? ShowScreen(string screenName)
    {
      return ShowScreen(screenName, true);
    }

    public Guid? ShowScreen(string screenName, bool backgroundEnabled)
    {
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
      ServiceRegistration.Get<ILogger>().Debug("ScreenManager: Preparing to close dialog '{0}'...", dialogInstanceId);
      IncPendingOperations();
      ScreenManagerMessaging.SendMessageCloseDialogs(dialogInstanceId, CloseDialogsMode.CloseSingleDialog);
    }

    public void CloseDialogs(Guid dialogInstanceId, bool inclusive)
    {
      if (inclusive)
        ServiceRegistration.Get<ILogger>().Debug("ScreenManager: Preparing to close dialog '{0}' and all dialogs on top of it...", dialogInstanceId);
      else
        ServiceRegistration.Get<ILogger>().Debug("ScreenManager: Preparing to close all dialogs on top of dialog '{0}'...", dialogInstanceId);
      IncPendingOperations();
      ScreenManagerMessaging.SendMessageCloseDialogs(dialogInstanceId, inclusive ? CloseDialogsMode.CloseAllOnTopIncluding : CloseDialogsMode.CloseAllOnTopExcluding);
    }

    public void CloseTopmostDialog()
    {
      Guid? dialogInstanceId = TopmostDialogInstanceId;
      if (dialogInstanceId.HasValue)
      {
        ServiceRegistration.Get<ILogger>().Debug("ScreenManager: Preparing to close topmost dialog '{0}'...", dialogInstanceId);
        IncPendingOperations();
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
      if (superLayerName == null)
      {
        ServiceRegistration.Get<ILogger>().Debug("ScreenManager: Preparing to hide super layer...");
        IncPendingOperations();
        ScreenManagerMessaging.SendMessageSetSuperLayer(null);
        return true;
      }
      ServiceRegistration.Get<ILogger>().Debug("ScreenManager: Preparing to show super layer '{0}'...", superLayerName);
      Screen newScreen = GetScreen(superLayerName, ScreenType.SuperLayer);
      if (newScreen == null)
          // Error message was shown in GetScreen()
        return false;
      IncPendingOperations();
      ScreenManagerMessaging.SendMessageSetSuperLayer(newScreen);
      return true;
    }

    public void Reload()
    {
      ServiceRegistration.Get<ILogger>().Debug("ScreenManager: Preparing to reload screens...");
      IncPendingOperations();
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
