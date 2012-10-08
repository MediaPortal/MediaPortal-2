#region Copyright (C) 2007-2012 Team MediaPortal

/*
    Copyright (C) 2007-2012 Team MediaPortal
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
using MediaPortal.Plugins.SlimTv.Client.Models;
using MediaPortal.Plugins.SlimTv.Interfaces;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;

namespace MediaPortal.Plugins.SlimTv.Client.Helpers
{
  /// <summary>
  /// ProgramProperties acts as GUI wrapper for an IProgram instance to allow binding of Properties.
  /// </summary>
  public class ProgramProperties
  {
    private DateTime _viewPortMinTime;
    private DateTime _viewPortMaxTime;
    private static readonly double ProgramWidthFactor = 6.5;

    public AbstractProperty ProgramIdProperty { get; set; }
    public AbstractProperty IsScheduledProperty { get; set; }
    public AbstractProperty TitleProperty { get; set; }
    public AbstractProperty DescriptionProperty { get; set; }
    public AbstractProperty StartTimeProperty { get; set; }
    public AbstractProperty EndTimeProperty { get; set; }
    public AbstractProperty RemainingDurationProperty { get; set; }
    public AbstractProperty ProgramWidthProperty { get; set; }
    public AbstractProperty GenreProperty { get; set; }
    
    /// <summary>
    /// Gets or Sets the Title.
    /// </summary>
    public String Title
    {
      get { return (String)TitleProperty.GetValue(); }
      set { TitleProperty.SetValue(value); }
    }

    /// <summary>
    /// Gets or Sets the Long Description.
    /// </summary>
    public String Description
    {
      get { return (String)DescriptionProperty.GetValue(); }
      set { DescriptionProperty.SetValue(value); }
    }

    /// <summary>
    /// Gets or Sets the Genre.
    /// </summary>
    public String Genre
    {
      get { return (String)GenreProperty.GetValue(); }
      set { GenreProperty.SetValue(value); }
    }
    
    /// <summary>
    /// Gets or Sets the Start time.
    /// </summary>
    public DateTime StartTime
    {
      get { return (DateTime)StartTimeProperty.GetValue(); }
      set { StartTimeProperty.SetValue(value); UpdateDuration(); }
    }

    /// <summary>
    /// Gets or Sets the End time.
    /// </summary>
    public DateTime EndTime
    {
      get { return (DateTime)EndTimeProperty.GetValue(); }
      set { EndTimeProperty.SetValue(value); UpdateDuration(); }
    }

    /// <summary>
    /// Gets or Sets the remaining duration. The value gets calculated from the difference of "EndTime - StartTime".
    /// If Start is less DateTime.Now, "EndTime - DateTime.Now" is used instead.
    /// </summary>
    public int RemainingDuration
    {
      get { return (int)RemainingDurationProperty.GetValue(); }
      set { RemainingDurationProperty.SetValue(value); }
    }

    /// <summary>
    /// Gets or Sets an indicator if the program is scheduled or currently recording.
    /// </summary>
    public bool IsScheduled
    {
      get { return (bool) IsScheduledProperty.GetValue(); }
      set { IsScheduledProperty.SetValue(value); }
    }

    /// <summary>
    /// Gets or Sets an indicator if the program is scheduled or currently recording.
    /// </summary>
    public int ProgramId
    {
      get { return (int) ProgramIdProperty.GetValue(); }
      set { ProgramIdProperty.SetValue(value); }
    }

    public double ProgramWidth
    {
      get { return (double) ProgramWidthProperty.GetValue(); }
      set { ProgramWidthProperty.SetValue(value); }
    }

    static ProgramProperties()
    {
      ResourceHelper.ReadResourceDouble("MultiGuideProgramTimeFactor", ref ProgramWidthFactor);
    }

    public ProgramProperties() : this(DateTime.Now, DateTime.Now.AddHours(SlimTvMultiChannelGuideModel.VisibleHours))
    {
    }

    public ProgramProperties(DateTime viewPortMinTime, DateTime viewPortMaxTime)
    {
      _viewPortMinTime = viewPortMinTime;
      _viewPortMaxTime = viewPortMaxTime;
      ProgramIdProperty = new WProperty(typeof(int), 0);
      IsScheduledProperty = new WProperty(typeof(bool), false);
      TitleProperty = new WProperty(typeof(String), String.Empty);
      DescriptionProperty = new WProperty(typeof(String), String.Empty);
      GenreProperty = new WProperty(typeof(String), String.Empty);
      StartTimeProperty = new WProperty(typeof(DateTime), DateTime.MinValue);
      EndTimeProperty = new WProperty(typeof(DateTime), DateTime.MinValue);
      RemainingDurationProperty = new WProperty(typeof(int), 0);
      ProgramWidthProperty = new WProperty(typeof(double), 0d);
    }

    public void SetProgram(IProgram program)
    {
      IProgramRecordingStatus recordingStatus = program as IProgramRecordingStatus;
      if (recordingStatus != null)
        IsScheduled = recordingStatus.RecordingStatus != RecordingStatus.None;

      if (program != null)
      {
        ProgramId = program.ProgramId;
        Title = program.Title;
        Description = program.Description;
        StartTime = program.StartTime;
        EndTime = program.EndTime;
        Genre = program.Genre;
        UpdateDuration();
      }
      else
      {
        ProgramId = 0;
        Title = string.Empty;
        Description = string.Empty;
        StartTime = FormatHelper.GetDay(DateTime.Now);
        EndTime = StartTime.AddDays(1);
        Genre = string.Empty;
      }
    }

    public void UpdateDuration(DateTime viewPortMinTime, DateTime viewPortMaxTime)
    {
      _viewPortMinTime = viewPortMinTime;
      _viewPortMaxTime = viewPortMaxTime;
      UpdateDuration();
    }

    private void UpdateDuration()
    {
      DateTime programStart = StartTime;
      if (programStart < _viewPortMinTime)
        programStart = _viewPortMinTime;

      DateTime programEnd = EndTime;
      if (programEnd > _viewPortMaxTime)
        programEnd = _viewPortMaxTime;

      RemainingDuration = Math.Max((int)(programEnd - programStart).TotalMinutes, 0);
      ProgramWidth = ProgramWidthFactor * RemainingDuration;
    }
  }
}