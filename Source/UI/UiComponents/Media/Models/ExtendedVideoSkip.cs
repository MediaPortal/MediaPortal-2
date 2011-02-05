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
using System.Timers;
using MediaPortal.Core;
using MediaPortal.Core.General;
using MediaPortal.Core.Localization;
using MediaPortal.Core.Settings;
using MediaPortal.UI.Presentation.Players;
using MediaPortal.UiComponents.Media.Settings;

namespace MediaPortal.UiComponents.Media.Models
{
  public class ExtendedVideoSkip
  {
    protected int _skipStepIndex = 0;
    protected int _skipStepDirection = 1;
    protected bool _skipStepValid = true;
    protected List<int> _skipSteps = new List<int>();
    protected Timer _skipStepTimer;
    protected AbstractProperty _skipStepProperty;

    public const string MODEL_ID_STR = "8573DBD8-A257-426a-9875-9DB155D32D47";

    public Guid ModelId
    {
      get { return new Guid(MODEL_ID_STR); }
    }
    
    public ExtendedVideoSkip()
    {
      _skipStepProperty = new SProperty(typeof(string), string.Empty);
      InitSkipSteps();
    }

    #region GUI Properties

    /// <summary>
    /// Gets a string which contains the current skip step (i.e. "+ 30 sec").
    /// </summary>
    public string SkipStep
    {
      get { return (string)_skipStepProperty.GetValue(); }
      internal set { _skipStepProperty.SetValue(value); }
    }

    public AbstractProperty SkipStepProperty
    {
      get { return _skipStepProperty; }
    }

    /// <summary>
    /// Called from the skin if the user invokes the "SkipStepForward" action. It will switch between the available skip steps
    /// and execute the skip after the skip timer is elapsed.
    /// </summary>
    public void SkipStepForward()
    {
      DoSkipStep(1);
    }

    /// <summary>
    /// Called from the skin if the user invokes the "SkipStepBackwar" action. It will switch between the available skip steps
    /// and execute the skip after the skip timer is elapsed.
    /// </summary>
    public void SkipStepBackward()
    {
      DoSkipStep(-1);
    }

    /// <summary>
    /// Called from the skin if the user invokes the "InstantSkip" action. This will start the InstantSkip in the
    /// underlaying player.
    /// </summary>
    public void InstantSkipForward()
    {
      IPlayerContext pc = GetPlayerContext();
      if (pc == null)
        return;

      MediaModelSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<MediaModelSettings>();
      pc.InstantSkip((int)settings.InstantSkipPercent);
    }

    /// <summary>
    /// Called from the skin if the user invokes the "InstantSkip" action. This will start the InstantSkip in the
    /// underlaying player.
    /// </summary>
    public void InstantSkipBackward()
    {
      IPlayerContext pc = GetPlayerContext();
      if (pc == null)
        return;

      MediaModelSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<MediaModelSettings>();
      pc.InstantSkip(-(int)settings.InstantSkipPercent);
    }

    #endregion

    #region Skip Step handling

    protected IPlayerContext GetPlayerContext()
    {
      IPlayerContextManager playerContextManager = ServiceRegistration.Get<IPlayerContextManager>();
      return playerContextManager.GetPlayerContext(PlayerChoice.CurrentPlayer);
    }

    private void InitSkipSteps()
    {
      // TODO: settings
      String stepList = "15,30,60,180,300,600,900,1800,3600,7200";
      _skipSteps.Clear();
      foreach (string step in stepList.Split(new char[] { ',' }))
      {
        int stepValue;
        if (int.TryParse(step, out stepValue))
          _skipSteps.Add(stepValue);
      }
      if (!_skipSteps.Contains(0))
        _skipSteps.Add(0);
      _skipSteps.Sort();
    }

    private void DoSkipStep(int skipDirection)
    {
      IPlayerContext pc = GetPlayerContext();
      if (pc == null)
        return;

      ReSetSkipTimer();

      // first we find the new skip index, then we check if the player is able to skip.
      int newSkipStepIndex = _skipStepIndex;

      // we are in the matching range and want to step "forward" (greater step into this direction).
      if (_skipStepDirection == skipDirection)
      {
        if (_skipStepIndex < _skipSteps.Count - 1)
          newSkipStepIndex++;
      }
      else
      {
        // the current index is in the opposite direction, so we take one step back.
        if (_skipStepIndex > 0)
          newSkipStepIndex--;
        else
        {
          _skipStepDirection *= -1; // swap sign and direction
          newSkipStepIndex = 1;
        }
      }

      if (pc.CanSkipRelative(TimeSpan.FromSeconds(_skipStepDirection * _skipSteps[newSkipStepIndex])))
      {
        // skip target is inside valid range
        _skipStepIndex = newSkipStepIndex;
        SkipStep = FormatStepUnit(_skipStepDirection * _skipSteps[_skipStepIndex]);
        _skipStepValid = true;
      }
      else
      {
        _skipStepValid = false;
        SkipStep = _skipStepDirection == -1 ? "[Media.Start]" : "[Media.End]";
      }
    }

    private void ReSetSkipTimer()
    {
      // TODO: settings for timer interval
      if (_skipStepTimer == null)
      {
        _skipStepTimer = new Timer(1500) { Enabled = true, AutoReset = false };
        _skipStepTimer.Elapsed += SkipStepTimerElapsed;
      }
      else
      {
        // in case of new user action, reset the timer.
        _skipStepTimer.Stop();
        _skipStepTimer.Start();
      }
    }

    private void SkipStepTimerElapsed(object sender, ElapsedEventArgs e)
    {
      IPlayerContext pc = GetPlayerContext();
      if (pc != null)
      {
        if (_skipStepValid)
          pc.SkipRelative(TimeSpan.FromSeconds(_skipStepDirection * _skipSteps[_skipStepIndex]));
        else
        {
          if (_skipStepDirection == -1)
            pc.SkipToStart();
          else
            pc.SkipToEnd();
        }
        _skipStepIndex = 0;
        SkipStep = string.Empty;
      }
    }

    /// <summary>
    /// This function returns the localized time units for "Step" (seconds) in human readable format.
    /// </summary>
    /// <param name="step"></param>
    /// <returns></returns>
    public static string FormatStepUnit(int step)
    {
      if (step == 0)
        return string.Empty;

      ILocalization loc = ServiceRegistration.Get<ILocalization>();
      string sign = step < 0 ? "-" : "+";
      int absStep = Math.Abs(step);
      if (absStep >= 3600)
      {
        // check for 'full' hours
        if ((Convert.ToSingle(absStep) / 3600) > 1 && (Convert.ToSingle(absStep) / 3600) != 2 &&
            (Convert.ToSingle(absStep) / 3600) != 3)
          return string.Format("{0} {1} {2}", sign, Convert.ToString(absStep / 60), loc.ToString("[Media.Minutes]")); // "min"

        return string.Format("{0} {1} {2}", sign, Convert.ToString(absStep / 3600), loc.ToString("[Media.Hours]")); // "hrs"
      }

      if (absStep >= 60)
        return string.Format("{0} {1} {2}", sign, Convert.ToString(absStep / 60), loc.ToString("[Media.Minutes]")); // "min"

      return string.Format("{0} {1} {2}", sign, Convert.ToString(absStep), loc.ToString("[Media.Seconds]")); // "sec"
    }

    #endregion
  }
}
