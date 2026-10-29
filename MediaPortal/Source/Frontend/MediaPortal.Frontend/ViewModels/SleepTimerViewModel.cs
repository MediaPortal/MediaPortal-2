/*
    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.
*/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using MediaPortal.Frontend.Services;

namespace MediaPortal.Frontend.ViewModels
{
  /// <summary>
  /// The sleep timer's dialog, question for question the way the reference laid it out
  /// (<c>dialogSleepTimer.xaml</c> over <c>SleepTimerModel</c>): "Time:" with a number, an up and a down
  /// step of five minutes and "min."; a line to take the number from the picture that is standing - the
  /// rest of this title plus one minute so the timer cannot beat the end credits; "Action:" on a button
  /// that cycles the machine's ways out; and one wide button underneath that starts the wait or stops it.
  /// </summary>
  /// <remarks>
  /// <para>
  /// The rules are the reference's arithmetic (<c>SleepTimerModel</c>): the minutes must stand between one
  /// and <see cref="Services.SleepTimerService.MaxMinutes"/> for the wide button to answer, typing or
  /// stepping is dead while a timer runs (only Stop is alive then), the wanted way out is remembered as
  /// the dialog's next default (<c>LastSleepTimerAction</c>), and the action button <em>cycles</em> - the
  /// reference never built a submenu, it turned the one button on to the next way out.
  /// </para>
  /// <para>
  /// The reference offered "After current media item" and "After current playlist" together; this back end
  /// knows the one title that is standing and no list of ones after it, so only the line it can answer is
  /// hung - a promise it would break is worse than the missing line.
  /// </para>
  /// </remarks>
  public sealed class SleepTimerViewModel : INotifyPropertyChanged
  {
    private readonly SleepTimerService _timer;
    private readonly List<ShutdownAction> _possible;
    private ShutdownAction _wanted;
    private string _timeText;
    private string _errorText = string.Empty;

    public SleepTimerViewModel(SleepTimerService timer, NowPlayingViewModel? playback = null,
        SystemStateSupport? support = null)
    {
      _timer = timer ?? throw new ArgumentNullException(nameof(timer));
      Playback = playback;

      SystemStateSupport machine = support ?? SystemStateSupport.ThisMachine;
      _possible = new List<ShutdownAction>();
      if (machine.Suspend)
        _possible.Add(ShutdownAction.Suspend);
      if (machine.Hibernate)
        _possible.Add(ShutdownAction.Hibernate);
      if (machine.SystemPower)
        _possible.Add(ShutdownAction.Shutdown);
      if (machine.SystemPower)
        _possible.Add(ShutdownAction.Restart);
      if (machine.Application)
        _possible.Add(ShutdownAction.CloseMP);
      if (machine.Application)
        _possible.Add(ShutdownAction.RestartMP);
      if (_possible.Count == 0)
        _possible.Add(ShutdownAction.Shutdown);

      _wanted = _possible.Contains(SleepTimerService.LastAction)
          ? SleepTimerService.LastAction
          : ShutdownAction.Shutdown;

      // The reference keeps whatever number was last in the field; a fresh dialog on a fresh application
      // answers half an hour, which is the wait the reference's own viewers found first.
      _timeText = "30";

      _timer.Changed += OnTimerChanged;
    }

    /// <summary>The dialog's heading, drawn by the headed frame.</summary>
    public string Heading
    {
      get { return "Sleep Timer"; }
    }

    private NowPlayingViewModel? Playback { get; }

    /// <summary>The minutes as typed or stepped. A number that cannot be read leaves the field and says so.</summary>
    public string TimeText
    {
      get { return _timeText; }
      set
      {
        if (_timeText == value)
          return;

        if (int.TryParse(value.Trim(), NumberStyles.Integer, CultureInfo.CurrentCulture, out int minutes)
            && minutes >= 0)
        {
          _timeText = minutes.ToString(CultureInfo.CurrentCulture);
          _errorText = string.Empty;
        }
        else
        {
          _timeText = value;
          _errorText = "Not a number of minutes.";
        }
        RaiseAll();
      }
    }

    /// <summary>Why the wide button will not answer, in the reference's red line, or empty.</summary>
    public string ErrorText
    {
      get { return _errorText; }
    }

    /// <summary>The minutes the wait would run, or nought when the field says something else.</summary>
    public int Minutes
    {
      get
      {
        return int.TryParse(_timeText.Trim(), NumberStyles.Integer, CultureInfo.CurrentCulture,
            out int minutes)
            ? minutes
            : 0;
      }
    }

    /// <summary>Whether a timer is standing right now - the state that kills every other control.</summary>
    public bool IsRunning
    {
      get { return _timer.IsActive; }
    }

    /// <summary>The number field and both steppers: dead while a wait runs, alive otherwise.</summary>
    public bool EditingEnabled
    {
      get { return !IsRunning; }
    }

    /// <summary>The up stepper: dead at the ceiling, alive under it.</summary>
    public bool AddEnabled
    {
      get { return !IsRunning && Minutes < SleepTimerService.MaxMinutes; }
    }

    /// <summary>The down stepper: dead at nought, alive above it.</summary>
    public bool SubEnabled
    {
      get { return !IsRunning && Minutes > 0; }
    }

    /// <summary>"After current media item": hung only while a title with a known end stands there.</summary>
    public bool MediaItemEnabled
    {
      get
      {
        return !IsRunning && Playback != null && Playback.HasMedia
            && Playback.LengthMilliseconds > Playback.PositionMilliseconds;
      }
    }

    /// <summary>The wide button: Start needs a real number, Stop answers whenever a wait runs.</summary>
    public bool ActivateEnabled
    {
      get
      {
        if (IsRunning)
          return true;
        return _errorText.Length == 0 && Minutes > 0 && Minutes <= SleepTimerService.MaxMinutes;
      }
    }

    /// <summary>The way out the wait would end in - the name on the cycling button.</summary>
    public string ActionName
    {
      get
      {
        if (_timer.IsActive)
          return _timer.ActionName;
        return SleepTimerService.NameOf(_wanted);
      }
    }

    /// <summary>"Start" or "Stop", the reference's one wide button reading its own state.</summary>
    public string StartButtonText
    {
      get { return _timer.IsActive ? "Stop" : "Start"; }
    }

    /// <summary>The action button stays alive only while the wait can still be talked to.</summary>
    public bool ActionSelectEnabled
    {
      get { return !IsRunning; }
    }

    /// <summary>Five minutes up, the reference's step, stopped short of the ceiling.</summary>
    public void AddTime()
    {
      int minutes = ((Minutes / 5) + 1) * 5;
      SetMinutes(Math.Min(minutes, SleepTimerService.MaxMinutes));
    }

    /// <summary>Five minutes down, the reference's step, never below nought.</summary>
    public void SubTime()
    {
      int minutes = (((Minutes + 4) / 5) - 1) * 5;
      SetMinutes(Math.Max(minutes, 0));
    }

    /// <summary>
    /// "After current media item": the rest of the standing title, plus one minute so the wait cannot
    /// beat the end of the picture - the reference's own arithmetic and its own reason.
    /// </summary>
    public void FromMediaItem()
    {
      NowPlayingViewModel? playback = Playback;
      if (playback == null)
        return;

      long remainingMs = playback.LengthMilliseconds - playback.PositionMilliseconds;
      if (remainingMs <= 0)
        return;

      int minutes = (int)TimeSpan.FromMilliseconds(remainingMs).TotalMinutes + 1;
      SetMinutes(Math.Min(minutes, SleepTimerService.MaxMinutes));
    }

    /// <summary>
    /// The cycling action button: the next way out the machine has, remembered as the dialog's next
    /// default exactly like the reference's <c>SetLastSleepTimerAction</c>.
    /// </summary>
    public void SelectAction()
    {
      if (IsRunning)
        return;

      int index = _possible.IndexOf(_wanted);
      if (index < 0)
        index = 0;
      _wanted = _possible[(index + 1) % _possible.Count];
      SleepTimerService.LastAction = _wanted;
      RaiseAll();
    }

    /// <summary>
    /// The wide button. While nothing runs it starts the wait with the number and the way out on screen;
    /// while one runs it stops the wait. Starting is what the reference's <c>Activate</c> was: the number
    /// freezes at that moment and the dialog stays open to say "Stop".
    /// </summary>
    public void Activate()
    {
      if (_timer.IsActive)
      {
        _timer.Stop();
        return;
      }

      if (!ActivateEnabled)
        return;

      _timer.Start(Minutes, _wanted);
    }

    private void SetMinutes(int minutes)
    {
      _errorText = string.Empty;
      _timeText = minutes.ToString(CultureInfo.CurrentCulture);
      RaiseAll();
    }

    private void OnTimerChanged()
    {
      RaiseAll();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void RaiseAll()
    {
      foreach (string name in new[]
      {
          nameof(TimeText), nameof(ErrorText), nameof(Minutes), nameof(IsRunning),
          nameof(EditingEnabled), nameof(AddEnabled), nameof(SubEnabled), nameof(MediaItemEnabled),
          nameof(ActivateEnabled), nameof(ActionName), nameof(StartButtonText),
          nameof(ActionSelectEnabled)
      })
      {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
      }
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
  }
}
