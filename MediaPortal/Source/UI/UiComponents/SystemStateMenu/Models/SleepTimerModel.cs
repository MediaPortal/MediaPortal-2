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
using System.Linq;
using MediaPortal.Common;
using MediaPortal.Common.Localization;
using MediaPortal.Common.Runtime;
using MediaPortal.Common.Settings;
using MediaPortal.Common.General;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.UI.Presentation.Models;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UI.Presentation.Players;
using MediaPortal.Plugins.SystemStateMenu.Settings;

namespace MediaPortal.Plugins.SystemStateMenu.Models
{
  /// <summary>
  /// Workflow model for the SleepTimer dialog.
  /// </summary>
  public class SleepTimerModel : IWorkflowModel
  {
    public const string SLEEP_TIMER_MODEL_ID_STR = "40FDD1C3-CFAB-4731-9636-96726301B648";

    #region Private fields

    private DateTime _startTime;
    private bool _needConfigRead = false;
    private bool _hasInputError = false;
    private List<SystemStateAction> PosibleShutdownModes { get; set; }
    private readonly System.Timers.Timer _timer;
    private readonly object _syncObject = new object();
    SystemStateAction? _actSystemState = null;
    SystemStateAction _wantedSystemState = SystemStateAction.Shutdown;
    private int _maxSleepTimeInMinutes = Consts.DEFAULT_MAX_SLEEPTIME;

    public SleepTimerModel()
    {
      _startTime = DateTime.Now;
      _initialMinutesProperty.Attach(InitialMinutesChanged);
      _timeTextProperty.Attach(TimeTextChanged);
      _timer = new System.Timers.Timer(100.0);
      _timer.Elapsed += _timer_Elapsed;
      _timer.Start();
    }

    #endregion

    #region Private members

    private void _timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
    {
      // update
      if (System.Threading.Monitor.TryEnter(_syncObject) == false)
        return;
      try
      {
        if (!_actSystemState.HasValue && _needConfigRead)
        {
          _needConfigRead = false;
          GetShutdownActionsFromSettings();
        }

        UpdateButtonEnabled();

        if (_actSystemState.HasValue)
        {
          // calculate Remaining Minutes
          TimeSpan ts = DateTime.Now - _startTime;
          int minutes = (int)ts.TotalMinutes;
          int rMin = InitialMinutes - minutes;

          if (RemainingMinutes != rMin)
            RemainingMinutes = rMin;

          ILocalization localization = ServiceRegistration.Get<ILocalization>();

          string actionString = localization.ToString(Consts.GetResourceIdentifierForMenuItem(_actSystemState.Value));
          string res = localization.ToString((RemainingMinutes <= 1) ? "[SleepTimer.ShutdownTextSingle]" : "[SleepTimer.ShutdownTextMulti]",
              actionString, RemainingMinutes);

          if (res != ShutdownText)
            ShutdownText = res;

          if (RemainingMinutes <= 0)
          {
            // finished, stop SleepTimer and do the action
            SystemStateAction toDo = _actSystemState.Value;
            Stop();
            DoAction(toDo);
          }
        }
        else
        {
          ShutdownText = string.Empty;
          RemainingMinutes = 0;
        }
      }
      finally
      {
        System.Threading.Monitor.Exit(_syncObject);
      }
    }

    private void UpdateButtonEnabled()
    {
      ILocalization localization = ServiceRegistration.Get<ILocalization>();
      if (_actSystemState.HasValue)
      {
        MediaItemEnabled = false;
        TextInputEnabled = false;
        AddEnabled = false;
        SubEnabled = false;
        ActivateEnabled = true;
        ButtonText = localization.ToString(Consts.GetResourceIdentifierForMenuItem(_actSystemState.Value));
        StartButtonText = "[SleepTimer.Stop]";
        IsSleepTimerActive = true;
        return;
      }

      bool playActive = false;
      try
      {
        IPlayerContextManager playerContextManager = ServiceRegistration.Get<IPlayerContextManager>();
        if (playerContextManager != null)
        {
          IPlayerContext playerContext = playerContextManager.CurrentPlayerContext;
          if (playerContext != null)
          {
            IPlayer player = playerContext.CurrentPlayer;
            IMediaPlaybackControl control = player as IMediaPlaybackControl;
            if (control != null)
              playActive = true;
          }
        }
      }
      catch (Exception)
      {
        // nur zur Sicherheit
      }

      if (InitialMinutes <= 0)
      {
        SubEnabled = false;
        AddEnabled = true;
      }
      if (InitialMinutes >= _maxSleepTimeInMinutes)
      {
        SubEnabled = true;
        AddEnabled = false;
      }
      if (InitialMinutes > 0 && InitialMinutes < _maxSleepTimeInMinutes)
      {
        SubEnabled = true;
        AddEnabled = true;
      }
      if (_hasInputError || InitialMinutes <= 0 || InitialMinutes > _maxSleepTimeInMinutes)
        ActivateEnabled = false;
      else
        ActivateEnabled = true;

      MediaItemEnabled = playActive;
      TextInputEnabled = true;

      ButtonText = localization.ToString(Consts.GetResourceIdentifierForMenuItem(_wantedSystemState));
      StartButtonText = "[SleepTimer.Start]";
      IsSleepTimerActive = false;
    }

    private void TimeTextChanged(AbstractProperty property, object oldValue)
    {
      // This message occurs, when the Time-Textbox is changed
      ILocalization localization = ServiceRegistration.Get<ILocalization>();

      // 1) test for integer
      int time;
      string textString = (string)property.GetValue();
      bool result = Int32.TryParse(textString, out time);
      if (result == false)
      {
        // not an Integer
        ErrorText = localization.ToString("[Configuration.ErrorIntegerValue]", textString);
        _hasInputError = true;
        return;
      }

      // 2) test for range
      if (time < 0 || time > _maxSleepTimeInMinutes)
      {
        // to low
        ErrorText = localization.ToString(
            (time < 0) ? "[Configuration.ErrorNumericLowerLimit]" : "[Configuration.ErrorNumericUpperLimit]",
            time, 0, _maxSleepTimeInMinutes);
        _hasInputError = true;
        return;
      }

      // 3) only rewrite, if needed
      ErrorText = string.Empty;
      _hasInputError = false;
      if (time != InitialMinutes)
        InitialMinutes = time;
    }

    private void InitialMinutesChanged(AbstractProperty property, object _oldValue)
    {
      int oldValue = 0;
      if (_oldValue != null)
        oldValue = (int)_oldValue;

      if (_actSystemState.HasValue)
      {
        property.SetValue(oldValue); // no new value while running
        return;
      }

      int newValue = (int)property.GetValue();

      newValue = Math.Max(0, Math.Min(_maxSleepTimeInMinutes, newValue));
      property.SetValue(newValue);
      TimeText = newValue.ToString();
      UpdateButtonEnabled();
    }

    private void SetLastSleepTimerAction(SystemStateAction action)
    {
      ISettingsManager settingsManager = ServiceRegistration.Get<ISettingsManager>();
      SystemStateDialogSettings settings = settingsManager.Load<SystemStateDialogSettings>();
      settings.LastSleepTimerAction = action;
      settingsManager.Save(settings);
    }

    private void GetShutdownActionsFromSettings()
    {
      SystemStateDialogSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<SystemStateDialogSettings>();
      PosibleShutdownModes = new List<SystemStateAction>();
      List<SystemStateItem> lst = settings.ShutdownItemList;
      for (int i = 0; i < lst.Count; i++)
      {
        if (lst[i].Enabled == false)
          continue;

        switch (lst[i].Action)
        {
          case SystemStateAction.Shutdown:
          case SystemStateAction.Suspend:
          case SystemStateAction.Hibernate:
            PosibleShutdownModes.Add(lst[i].Action);
            break;
        }
      }

      // at least, one element is needed
      if (PosibleShutdownModes.Count == 0)
        PosibleShutdownModes.Add(SystemStateAction.Shutdown);

      // read the max sleeptime
      if (settings.MaxSleepTimeout.HasValue)
        _maxSleepTimeInMinutes = settings.MaxSleepTimeout.Value;

      // read the last action
      if (settings.LastSleepTimerAction.HasValue)
        _wantedSystemState = settings.LastSleepTimerAction.Value;

      UpdateButtonEnabled();
    }

    #endregion

    #region Public properties (can be used by the GUI)

    protected AbstractProperty _shutdownTextProperty = new WProperty(typeof(string), string.Empty);
    protected AbstractProperty _timeTextProperty = new WProperty(typeof(string), string.Empty);
    protected AbstractProperty _errorTextProperty = new WProperty(typeof(string), string.Empty);
    protected AbstractProperty _buttonTextProperty = new WProperty(typeof(string), "");
    protected AbstractProperty _startButtonTextProperty = new WProperty(typeof(string), "[SleepTimer.Start]");
    protected AbstractProperty _remainingMinutesProperty = new WProperty(typeof(int), 0);
    protected AbstractProperty _initialMinutesProperty = new WProperty(typeof(int), 0);
    protected AbstractProperty _addEnabledProperty = new WProperty(typeof(bool), true);
    protected AbstractProperty _subEnabledProperty = new WProperty(typeof(bool), false);
    protected AbstractProperty _activateEnabledProperty = new WProperty(typeof(bool), false);
    protected AbstractProperty _mediaItemEnabledProperty = new WProperty(typeof(bool), true);
    protected AbstractProperty _textInputEnabledProperty = new WProperty(typeof(bool), true);
    protected AbstractProperty _isSleepTimerActiveProperty = new WProperty(typeof(bool), false);

    public AbstractProperty ShutdownTextProperty
    {
      get { return _shutdownTextProperty; }
    }
    public string ShutdownText
    {
      get { return (string)_shutdownTextProperty.GetValue(); }
      set { _shutdownTextProperty.SetValue(value); }
    }

    public AbstractProperty TimeTextProperty
    {
      get { return _timeTextProperty; }
    }
    public string TimeText
    {
      get { return (string)_timeTextProperty.GetValue(); }
      set { _timeTextProperty.SetValue(value); }
    }

    public AbstractProperty ErrorTextProperty
    {
      get { return _errorTextProperty; }
    }
    public string ErrorText
    {
      get { return (string)_errorTextProperty.GetValue(); }
      set { _errorTextProperty.SetValue(value); }
    }

    public AbstractProperty ButtonTextProperty
    {
      get { return _buttonTextProperty; }
    }
    public string ButtonText
    {
      get { return (string)_buttonTextProperty.GetValue(); }
      set { _buttonTextProperty.SetValue(value); }
    }

    public AbstractProperty StartButtonTextProperty
    {
      get { return _startButtonTextProperty; }
    }
    public string StartButtonText
    {
      get { return (string)_startButtonTextProperty.GetValue(); }
      set { _startButtonTextProperty.SetValue(value); }
    }

    public AbstractProperty RemainingMinutesProperty
    {
      get { return _remainingMinutesProperty; }
    }
    public int RemainingMinutes
    {
      get { return (int)_remainingMinutesProperty.GetValue(); }
      set { _remainingMinutesProperty.SetValue(value); }
    }

    public AbstractProperty InitialMinutesProperty
    {
      get { return _initialMinutesProperty; }
    }
    public int InitialMinutes
    {
      get { return (int)_initialMinutesProperty.GetValue(); }
      set { _initialMinutesProperty.SetValue(value); }
    }


    public AbstractProperty AddEnabledProperty
    {
      get { return _addEnabledProperty; }
    }
    public bool AddEnabled
    {
      get { return (bool)_addEnabledProperty.GetValue(); }
      set { _addEnabledProperty.SetValue(value); }
    }

    public AbstractProperty SubEnabledProperty
    {
      get { return _subEnabledProperty; }
    }
    public bool SubEnabled
    {
      get { return (bool)_subEnabledProperty.GetValue(); }
      set { _subEnabledProperty.SetValue(value); }
    }

    public AbstractProperty ActivateEnabledProperty
    {
      get { return _activateEnabledProperty; }
    }
    public bool ActivateEnabled
    {
      get { return (bool)_activateEnabledProperty.GetValue(); }
      set { _activateEnabledProperty.SetValue(value); }
    }

    public AbstractProperty MediaItemEnabledProperty
    {
      get { return _mediaItemEnabledProperty; }
    }
    public bool MediaItemEnabled
    {
      get { return (bool)_mediaItemEnabledProperty.GetValue(); }
      set { _mediaItemEnabledProperty.SetValue(value); }
    }

    public AbstractProperty TextInputEnabledProperty
    {
      get { return _textInputEnabledProperty; }
    }
    public bool TextInputEnabled
    {
      get { return (bool)_textInputEnabledProperty.GetValue(); }
      set { _textInputEnabledProperty.SetValue(value); }
    }

    public AbstractProperty IsSleepTimerActiveProperty
    {
      get { return _isSleepTimerActiveProperty; }
    }
    public bool IsSleepTimerActive
    {
      get { return (bool)_isSleepTimerActiveProperty.GetValue(); }
      set { _isSleepTimerActiveProperty.SetValue(value); }
    }
    #endregion

    public static void DoAction(SystemStateAction action)
    {
      switch (action)
      {
        case SystemStateAction.Suspend:
          ServiceRegistration.Get<ISystemStateService>().Suspend();
          return;

        case SystemStateAction.Hibernate:
          ServiceRegistration.Get<ISystemStateService>().Hibernate();
          return;

        case SystemStateAction.Shutdown:
          ServiceRegistration.Get<ISystemStateService>().Shutdown();
          return;
      }
    }

    #region Public methods (can be used by the GUI)

    /// <summary>
    /// Provides a callable method for the skin to activate the sleeptimer.
    /// </summary>
    public void Activate()
    {
      if (!_actSystemState.HasValue)
      {
        // sleeptimer is deactivated, activate it (if posible)
        if (InitialMinutes > 0)
        {
          _actSystemState = _wantedSystemState;
          _startTime = DateTime.Now;
        }
      }
      else
      {
        _actSystemState = null;
      }
      // update Buttontext & Buttonstates
      UpdateButtonEnabled();
    }

    /// <summary>
    /// Provides a callable method for the skin to change the action of the sleeptimer.
    /// </summary> 
    public void Select()
    {
      if (_actSystemState.HasValue)
        return;

      int index = 0;
      for (int i = 0; i < PosibleShutdownModes.Count; i++)
      {
        if (PosibleShutdownModes[i] == _wantedSystemState)
        {
          index = i;
          break;
        }
      }

      index = (index + 1) % PosibleShutdownModes.Count;
      _wantedSystemState = PosibleShutdownModes[index];
      SetLastSleepTimerAction(_wantedSystemState);
      // update Buttontext & Buttonstates
      UpdateButtonEnabled();
    }

    /// <summary>
    /// Provides a callable method for the skin to stop the sleeptimer.
    /// </summary>
    public void Stop()
    {
      if (_actSystemState.HasValue)
      {
        _actSystemState = null;
      }
      // update Buttontext & Buttonstates
      UpdateButtonEnabled();
    }

    /// <summary>
    /// Provides a callable method for the skin to set the time of the sleeptimer
    /// to the length of the current mediaitem
    /// </summary>
    public void FromMediaItem()
    {
      try
      {
        IPlayerContextManager playerContextManager = ServiceRegistration.Get<IPlayerContextManager>();
        if (playerContextManager == null)
          return;
        IPlayerContext playerContext = playerContextManager.CurrentPlayerContext;
        if (playerContext == null)
          return;
        IPlayer player = playerContext.CurrentPlayer;
        IMediaPlaybackControl control = player as IMediaPlaybackControl;
        if (control == null)
          return;

        TimeSpan duration = control.Duration.Subtract(control.CurrentTime);
        int minutes = (int)duration.TotalMinutes;
        // Add 1 extra Minute to ensure, that the player finished
        minutes += 1;
        InitialMinutes = minutes;
      }
      catch (Exception)
      {
        // nur zur Sicherheit
      }
    }

    /// <summary>
    /// Provides a callable method for the skin to set the time of the sleeptimer
    /// to the length of the current playlist
    /// </summary>
    public void FromPlaylist()
    {
      try
      {
        IPlayerContextManager playerContextManager = ServiceRegistration.Get<IPlayerContextManager>();
        if (playerContextManager == null)
          return;
        IPlayerContext playerContext = playerContextManager.CurrentPlayerContext;
        if (playerContext == null)
          return;

        IPlaylist playlist = playerContext.Playlist;
        if (playlist == null)
          return;

        // playlistTime
        TimeSpan playlistDuration = new TimeSpan();
        MediaItem item;
        for (int i = 0; (item = playlist[i]) != null; i++)
        {
          IList<MediaItemAspect> aspects;
          if (item.Aspects.TryGetValue(AudioAspect.ASPECT_ID, out aspects))
          {
            var aspect = aspects.First();
            long? dur = aspect == null ? null : (long?)aspect[AudioAspect.ATTR_DURATION];
            TimeSpan miDur = dur.HasValue ? TimeSpan.FromSeconds(dur.Value) : TimeSpan.FromSeconds(0);
            playlistDuration = playlistDuration.Add(miDur);
          }
          else if (item.Aspects.TryGetValue(VideoStreamAspect.ASPECT_ID, out aspects))
          {
            var aspect = aspects.First();
            int? part = (int?)aspect[VideoStreamAspect.ATTR_VIDEO_PART];
            int? partSet = (int?)aspect[VideoStreamAspect.ATTR_VIDEO_PART_SET];
            long? dur = null;
            if (!part.HasValue || part < 0)
            {
              dur = (long?)aspect[VideoStreamAspect.ATTR_DURATION];
            }
            else if(partSet.HasValue)
            {
              dur = aspects.Where(a => (int?)a[VideoStreamAspect.ATTR_VIDEO_PART_SET] == partSet && 
              aspect[VideoStreamAspect.ATTR_DURATION] != null).Sum(a => (long)a[VideoStreamAspect.ATTR_DURATION]);
            }
            TimeSpan miDur = dur.HasValue ? TimeSpan.FromSeconds(dur.Value) : TimeSpan.FromSeconds(0);
            playlistDuration = playlistDuration.Add(miDur);
          }
        }

        // currentTime
        IPlayer player = playerContext.CurrentPlayer;
        IMediaPlaybackControl control = player as IMediaPlaybackControl;
        if (control == null)
          return;

        TimeSpan duration = playlistDuration.Subtract(control.CurrentTime);
        int minutes = (int)duration.TotalMinutes;
        // Add 1 extra Minute to ensure, that the player finished
        minutes += 1;
        InitialMinutes = minutes;
      }
      catch (Exception)
      {
        // nur zur Sicherheit
      }
    }

    /// <summary>
    /// Provides a callable method for the skin to add 5 minutes to the sleeptimer.
    /// </summary>
    public void AddTime()
    {
      if (_actSystemState.HasValue)
        return;

      int tmp = ((InitialMinutes / 5) + 1) * 5;
      InitialMinutes = tmp;
    }

    /// <summary>
    /// Provides a callable method for the skin to sub 5 minutes to the sleeptimer.
    /// </summary>
    public void SubTime()
    {
      if (_actSystemState.HasValue)
        return;

      int tmp = (((InitialMinutes + 4) / 5) - 1) * 5;
      InitialMinutes = tmp;
    }

    #endregion

    #region IWorkflowModel implementation

    public Guid ModelId
    {
      get { return new Guid(SLEEP_TIMER_MODEL_ID_STR); }
    }

    public bool CanEnterState(NavigationContext oldContext, NavigationContext newContext)
    {
      return true;
    }

    public void EnterModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      // Load settings, but only if sleeptimer is not started yet
      if (!_actSystemState.HasValue)
        GetShutdownActionsFromSettings();
      else
        _needConfigRead = true;
    }

    public void ExitModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      _needConfigRead = false;
    }

    public void ChangeModelContext(NavigationContext oldContext, NavigationContext newContext, bool push)
    {
      // TODO
    }

    public void Deactivate(NavigationContext oldContext, NavigationContext newContext)
    {
      _timer.Stop();
    }

    public void Reactivate(NavigationContext oldContext, NavigationContext newContext)
    {
      // after suspension, disable Sleeptimer
      Stop();
    }

    public void UpdateMenuActions(NavigationContext context, IDictionary<Guid, WorkflowAction> actions)
    {
      // Nothing to do here
    }

    public ScreenUpdateMode UpdateScreen(NavigationContext context, ref string screen)
    {
      return ScreenUpdateMode.AutoWorkflowManager;
    }

    #endregion
  }
}
