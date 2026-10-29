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
using System.Timers;

namespace MediaPortal.Frontend.Services
{
  /// <summary>
  /// The sleep timer, kept the way the reference kept it (<c>SleepTimerModel</c>): somebody says how many
  /// minutes and which way out, the timer watches the clock, and when the minutes have run out the way out
  /// happens on its own. While it runs, the shutdown menu says so - the reference spelled it out under its
  /// list, "Shutdown in 12 minutes", and turned its own row into "Stop sleep timer" - and the dialog it was
  /// started from shows the same standing order until the viewer stops it or it fires.
  /// </summary>
  /// <remarks>
  /// <para>
  /// The timer is one object for the whole application, like the reference's workflow model: it keeps
  /// running while the viewer walks through every screen, and every screen that asks (the shutdown menu,
  /// its own dialog) reads the same answer. Closing the dialog does not stop it - the reference let the
  /// viewer walk away from a running timer and only <em>Stop</em> stopped it.
  /// </para>
  /// <para>
  /// The clock and the tick's route to the screens are the two seams that make it answerable without a
  /// television: <see cref="Clock"/> stands in for the wall clock in tests, and <see cref="TickMarshaller"/>
  /// carries a tick onto the thread that draws (the application sets it; a test leaves it alone and ticks
  /// inline).
  /// </para>
  /// </remarks>
  public sealed class SleepTimerService
  {
    /// <summary>The reference's ceiling for a sleep timer (<c>Consts.DEFAULT_MAX_SLEEPTIME</c>).</summary>
    public const int MaxMinutes = 300;

    private readonly object _sync = new object();
    private readonly Action<ShutdownAction> _runAction;
    private Timer? _timer;
    private DateTime _startTime;

    public SleepTimerService(Action<ShutdownAction>? runAction = null)
    {
      _runAction = runAction ?? delegate { };
    }

    /// <summary>What "now" is. The application never changes it; a test does.</summary>
    public Func<DateTime> Clock { get; set; } = delegate { return DateTime.Now; };

    /// <summary>
    /// How a tick reaches the screens - the application posts it onto the thread that draws, and a test
    /// leaves it running inline so the whole timer answers without a display.
    /// </summary>
    public Action<Action> TickMarshaller { get; set; } = delegate (Action action) { action(); };

    /// <summary>Raised whenever the standing order changes - started, stopped, or a minute went by.</summary>
    public event Action? Changed;

    /// <summary>Whether a timer is running right now.</summary>
    public bool IsActive { get; private set; }

    /// <summary>The minutes the viewer asked for when the timer was started.</summary>
    public int InitialMinutes { get; private set; }

    /// <summary>The minutes left, counting down; nought once the order has fired.</summary>
    public int RemainingMinutes { get; private set; }

    /// <summary>The way out that will happen when the minutes run out.</summary>
    public ShutdownAction Action { get; private set; }

    /// <summary>The name of the standing order's way out, for the notice line.</summary>
    public string ActionName
    {
      get { return NameOf(Action); }
    }

    /// <summary>
    /// The line the reference wrote under its shutdown list while a timer ran: "Shutdown in 12 minutes",
    /// or "Shutdown in less than one minute" once only the last minute is left. Empty when nothing runs -
    /// a menu that has nothing to wait for says nothing about waiting.
    /// </summary>
    public string NoticeText
    {
      get
      {
        if (!IsActive)
          return string.Empty;
        return RemainingMinutes <= 1
            ? ActionName + " in less than one minute"
            : ActionName + " in " + RemainingMinutes + " minutes";
      }
    }

    /// <summary>
    /// Starting the wait, exactly like the reference's <c>Start</c>: the minutes are fixed at the moment of
    /// starting, and the last choice of a way out sticks as the dialog's next default. Starting a timer
    /// that already runs is answered by the running one - the reference never ran two.
    /// </summary>
    public void Start(int minutes, ShutdownAction action)
    {
      if (minutes <= 0 || minutes > MaxMinutes)
        throw new ArgumentOutOfRangeException(nameof(minutes),
            "a sleep timer waits between one minute and " + MaxMinutes);

      lock (_sync)
      {
        if (IsActive)
          return;

        InitialMinutes = minutes;
        RemainingMinutes = minutes;
        Action = action;
        LastAction = action;
        _startTime = Clock();
        IsActive = true;

        _timer = new Timer(1000.0);
        _timer.Elapsed += delegate { Refresh(); };
        _timer.Start();
      }
      Raise();
    }

    /// <summary>
    /// Breaking the order. Answers nothing when none stands - a viewer who presses Stop twice, or a menu
    /// that fires its row after the timer already stopped itself, changes nothing rather than changing it
    /// back.
    /// </summary>
    public void Stop()
    {
      lock (_sync)
      {
        if (!IsActive)
          return;

        IsActive = false;
        RemainingMinutes = 0;
        Timer? timer = _timer;
        _timer = null;
        if (timer != null)
        {
          timer.Stop();
          timer.Dispose();
        }
      }
      Raise();
    }

    /// <summary>
    /// The way out the timer dialog will offer first next time: the reference remembered the last choice
    /// (<c>LastSleepTimerAction</c> in its settings) and this one remembers it for as long as the
    /// application lives.
    /// </summary>
    public static ShutdownAction LastAction { get; set; } = ShutdownAction.Shutdown;

    /// <summary>
    /// Looking at the clock once: recompute the minutes left, tell the screens, and - when the wait is
    /// over - step out from under it and do the way out. The reference ran the action only after stopping
    /// itself, so a timer never fires twice for one order.
    /// </summary>
    public void Refresh()
    {
      ShutdownAction? due = null;
      lock (_sync)
      {
        if (!IsActive)
          return;

        int minutes = (int)(Clock() - _startTime).TotalMinutes;
        int remaining = InitialMinutes - minutes;
        bool changed = remaining != RemainingMinutes;
        RemainingMinutes = remaining;

        if (remaining <= 0)
        {
          due = Action;
          IsActive = false;
          RemainingMinutes = 0;
          Timer? timer = _timer;
          _timer = null;
          changed = true;
          if (timer != null)
          {
            timer.Stop();
            timer.Dispose();
          }
        }

        if (!changed)
          return;
      }

      TickMarshaller(delegate
      {
        Changed?.Invoke();
        if (due.HasValue)
          _runAction(due.Value);
      });
    }

    private void Raise()
    {
      TickMarshaller(delegate { Changed?.Invoke(); });
    }

    /// <summary>
    /// The word for a way out, spelled the way the reference spelled it (<c>strings_en.xml</c> of the
    /// SystemStateMenu plugin), so a line on this menu and a line on the reference's say the same thing.
    /// </summary>
    public static string NameOf(ShutdownAction action, bool sleepTimerRunning = false)
    {
      switch (action)
      {
        case ShutdownAction.Suspend: return "Suspend";
        case ShutdownAction.Hibernate: return "Hibernate";
        case ShutdownAction.Shutdown: return "Shutdown";
        case ShutdownAction.Restart: return "Restart Windows";
        case ShutdownAction.CloseMP: return "Close MediaPortal";
        case ShutdownAction.RestartMP: return "Restart MediaPortal";
        case ShutdownAction.SleepTimer:
          return sleepTimerRunning ? "Stop sleep timer" : "Setup sleep timer";
        default:
          return string.Empty;
      }
    }
  }
}
