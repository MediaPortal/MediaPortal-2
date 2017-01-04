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
using MediaPortal.Common;
using MediaPortal.Common.General;
using MediaPortal.Common.Settings;
using MediaPortal.Plugins.SlimTv.Client.Settings;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UiComponents.Media.General;

namespace MediaPortal.Plugins.SlimTv.Client.Helpers
{
  /// <summary>
  /// Holds a GUI item which represents a Program item.
  /// </summary>
  public class ProgramListItem : ListItem
  {
    protected AbstractProperty _programProperty = null;
    protected AbstractProperty _isRunningProperty = null;
    protected AbstractProperty _progressProperty = null;

    /// <summary>
    /// Exposes the program.
    /// </summary>
    public ProgramProperties Program
    {
      get { return (ProgramProperties) _programProperty.GetValue(); }
      set { _programProperty.SetValue(value); }
    }
    /// <summary>
    /// Exposes the program.
    /// </summary>
    public AbstractProperty ProgramProperty
    {
      get { return _programProperty; }
    }

    /// <summary>
    /// Exposes a flag if the program is currently running.
    /// </summary>
    public bool IsRunning
    {
      get { return (bool) _isRunningProperty.GetValue(); }
      set { _isRunningProperty.SetValue(value); }
    }

    /// <summary>
    /// Exposes a flag if the program is currently running.
    /// </summary>
    public AbstractProperty IsRunningProperty
    {
      get { return _isRunningProperty; }
    }

    /// <summary>
    /// Exposes percent value of programm's progress. It will be a value between <c>0</c> and <c>100</c>.
    /// If program is not running, it will be always <c>0</c>.
    /// </summary>
    public double Progress
    {
      get { return (double) _progressProperty.GetValue(); }
      set { _progressProperty.SetValue(value); }
    }

    /// <summary>
    /// Exposes percent value of programm's progress. It will be a value between <c>0</c> and <c>100</c>.
    /// If program is not running, it will be always <c>0</c>.
    /// </summary>
    public AbstractProperty ProgressProperty
    {
      get { return _progressProperty; }
    }


    public ProgramListItem(ProgramProperties program)
    {
      _programProperty = new WProperty(typeof(ProgramProperties), program);
      _isRunningProperty = new WProperty(typeof(bool), false);
      _progressProperty = new WProperty(typeof(double), 0d);
      SetLabel(Consts.KEY_NAME, program.Title);
      SetLabel("Title", program.Title);
      SetLabel("StartTime", program.StartTime.FormatProgramTime());
      SetLabel("EndTime", program.EndTime.FormatProgramTime());
      SetLabel("Series", BuildSeriesText(program));
      Update();
    }

    private string BuildSeriesText(ProgramProperties program)
    {
      if (!ServiceRegistration.Get<ISettingsManager>().Load<SlimTvClientSettings>().ShowSeriesInfo)
        return null;
      var text = ProgramProperties.BuildSeriesText(program);
      return string.IsNullOrEmpty(text) ? null : string.Format("({0})", text);
    }

    /// <summary>
    /// Updates Program IsRunning status
    /// </summary>
    public void Update()
    {
      DateTime now = DateTime.Now;
      IsRunning = Program.StartTime <= now && Program.EndTime > now;
      Progress = (now - Program.StartTime).TotalSeconds / (Program.EndTime - Program.StartTime).TotalSeconds * 100;
    }
  }

  public class PlaceholderListItem : ProgramListItem
  {
    public PlaceholderListItem(ProgramProperties program)
      : base(program)
    { }
  }
}
