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
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using MediaPortal.Frontend.Services;

namespace MediaPortal.Frontend.ViewModels
{
  /// <summary>
  /// One line of the shutdown menu: the word the reference put on it, the face it hung beside the word,
  /// and the way out it stands for. The reference spelled its picture out of the row itself
  /// (<c>PowerMenu\{Action}.png</c>); this face carries the rows as shapes instead, so the naming says
  /// which shape of <c>images\PowerMenu</c> answers which line - the two restarts wearing the one fresh
  /// face, since a viewer is not asked to tell a machine's restart from the application's by its glyph.
  /// </summary>
  public sealed class ShutdownChoice
  {
    public ShutdownChoice(ShutdownAction action, string name, string icon)
    {
      Action = action;
      Name = name;
      Icon = icon;
    }

    /// <summary>Which way out this line is.</summary>
    public ShutdownAction Action { get; }

    /// <summary>The word on the line, in the reference's spelling.</summary>
    public string Name { get; }

    /// <summary>The worn skin's face for the line, named the skin's way.</summary>
    public string Icon { get; }
  }

  /// <summary>
  /// The way out of the machine, as the reference laid it out in <c>dialogSystemState</c>: a dialog headed
  /// "Shutdown menu" whose rows were the machine's ways out, in the order its settings kept them - sleep,
  /// hibernate, shutdown, the sleep timer, restart, then the application's own two ways. A line on the
  /// list IS the answer: the reference had no OK button, because a dialog that asked a viewer to press a
  /// line and then press something else would be two presses where the reference needed one.
  /// </summary>
  /// <remarks>
  /// <para>
  /// The machine's own ways are hung wherever the operating system hands its power to a program - on anything
  /// but Windows only the application's two ways and the timer stand. The reference's settings hid minimize
  /// and log-off by default, so this list never carries them.
  /// </para>
  /// <para>
  /// The sleep timer's line answers the way the reference's did: while no timer runs it opens the timer
  /// dialog; while one runs it IS the stop - and the row then reads "Stop sleep timer", with the standing
  /// order spelled out under the list ("Shutdown in 12 minutes") exactly like the reference wrote it there.
  /// </para>
  /// </remarks>
  public sealed class CloseViewModel : INotifyPropertyChanged
  {
    private readonly SleepTimerService? _timer;
    private readonly Action<ShutdownAction> _run;
    private readonly SystemStateSupport _support;
    private readonly Action? _openSleepTimer;

    public CloseViewModel(SleepTimerService? timer = null,
        Action<ShutdownAction>? run = null,
        SystemStateSupport? support = null,
        Action? openSleepTimer = null)
    {
      _timer = timer;
      _run = run ?? delegate { };
      _support = support ?? SystemStateSupport.ThisMachine;
      _openSleepTimer = openSleepTimer;

      if (_timer != null)
        _timer.Changed += Rebuild;

      Rebuild();
    }

    /// <summary>The dialog's heading, which the headed frame draws the reference's way.</summary>
    public string Heading
    {
      get { return "Shutdown menu"; }
    }

    /// <summary>The ways out, in the reference's order, filtered by what this machine can do.</summary>
    public ObservableCollection<ShutdownChoice> Choices { get; } =
        new ObservableCollection<ShutdownChoice>();

    /// <summary>
    /// The standing order under the list, or nothing when nothing stands: the reference wrote its sleep
    /// timer's state as a small line under the menu, visible only while the timer ran.
    /// </summary>
    public string Notice
    {
      get { return _timer?.NoticeText ?? string.Empty; }
    }

    /// <summary>Whether the notice line stands at all.</summary>
    public bool HasNotice
    {
      get { return _timer != null && _timer.IsActive; }
    }

    /// <summary>
    /// Pressing a line. Everything goes through the caller's <c>run</c> - the application answers what
    /// leaving and coming back really mean - except the sleep timer's own line, which is the one line the
    /// dialog can answer twice: it opens the timer's dialog while nothing runs, and stops the running
    /// timer when one does (the reference's <c>DoAction</c> split the row exactly this way).
    /// </summary>
    public void Choose(ShutdownChoice choice)
    {
      if (choice.Action != ShutdownAction.SleepTimer)
      {
        _run(choice.Action);
        return;
      }

      SleepTimerService? timer = _timer;
      if (timer != null && timer.IsActive)
      {
        timer.Stop();
        return;
      }
      _openSleepTimer?.Invoke();
    }

    /// <summary>
    /// Laying the rows out again: the reference rebuilt its list every time the dialog opened and every
    /// time the sleep timer changed, so the row flips between "Setup sleep timer" and "Stop sleep timer"
    /// while the dialog stands.
    /// </summary>
    private void Rebuild()
    {
      List<ShutdownChoice> rows = new List<ShutdownChoice>();
      if (_support.Suspend)
        rows.Add(Row(ShutdownAction.Suspend));
      if (_support.Hibernate)
        rows.Add(Row(ShutdownAction.Hibernate));
      if (_support.SystemPower)
        rows.Add(Row(ShutdownAction.Shutdown));
      // The timer's own row is only honest where at least one way out can actually happen: a wait that
      // ends in nothing is no wait. On a phone - no machine power, no leaving - the row stays off.
      if (_support.SystemPower || _support.Application)
        rows.Add(Row(ShutdownAction.SleepTimer));
      if (_support.SystemPower)
        rows.Add(Row(ShutdownAction.Restart));
      if (_support.Application)
        rows.Add(Row(ShutdownAction.CloseMP));
      if (_support.Application)
        rows.Add(Row(ShutdownAction.RestartMP));

      Choices.Clear();
      foreach (ShutdownChoice row in rows)
        Choices.Add(row);

      OnPropertyChanged(nameof(Notice));
      OnPropertyChanged(nameof(HasNotice));
    }

    private ShutdownChoice Row(ShutdownAction action)
    {
      bool running = _timer != null && _timer.IsActive;
      return new ShutdownChoice(action,
          SleepTimerService.NameOf(action, running),
          "PowerMenu/" + FaceOf(action));
    }

    /// <summary>
    /// Which face of <c>images\PowerMenu</c> the line wears. Most lines wear their own; the sleep timer's
    /// line wears the clock whether or not a wait stands, and both ways back - the machine's restart and
    /// the application's - wear the one fresh face, because the two words already say whose restart it is.
    /// </summary>
    private static string FaceOf(ShutdownAction action)
    {
      return action switch
      {
        ShutdownAction.SleepTimer => "timer",
        ShutdownAction.Restart => "refresh",
        ShutdownAction.RestartMP => "refresh",
        ShutdownAction.CloseMP => "close",
        _ => action.ToString().ToLowerInvariant()
      };
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
  }
}
