#region Copyright (C) 2007-2013 Team MediaPortal

/*
    Copyright (C) 2007-2013 Team MediaPortal
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
using MediaPortal.Common.General;
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

    public ProgramListItem(ProgramProperties program)
    {
      _programProperty = new WProperty(typeof(ProgramProperties), program);
      _isRunningProperty = new WProperty(typeof(bool), false);
      SetLabel(Consts.KEY_NAME, program.Title);
      SetLabel("Title", program.Title);
      SetLabel("StartTime", FormatHelper.FormatProgramTime(program.StartTime));
      SetLabel("EndTime", FormatHelper.FormatProgramTime(program.EndTime));
      Update();
    }

    /// <summary>
    /// Updates Program IsRunning status
    /// </summary>
    public void Update()
    {
      DateTime now = DateTime.Now;
      IsRunning = Program.StartTime <= now && Program.EndTime > now;
    }
  }

  public class PlaceholderListItem : ProgramListItem
  {
    public PlaceholderListItem(ProgramProperties program)
      : base(program)
    { }
  }
}