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
using MediaPortal.Plugins.SlimTv.Interfaces;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;

namespace MediaPortal.Plugins.SlimTv.Client.Helpers
{
  /// <summary>
  /// ProgramProperties acts as GUI wrapper for an IProgram instance to allow binding of Properties.
  /// </summary>
  public class ProgramProperties
  {
    private bool _settingProgram;

    public AbstractProperty ProgramIdProperty { get; set; }
    public AbstractProperty IsScheduledProperty { get; set; }
    public AbstractProperty IsSeriesScheduledProperty { get; set; }
    public AbstractProperty TitleProperty { get; set; }
    public AbstractProperty DescriptionProperty { get; set; }
    public AbstractProperty StartTimeProperty { get; set; }
    public AbstractProperty EndTimeProperty { get; set; }
    public AbstractProperty RemainingDurationProperty { get; set; }
    public AbstractProperty GenreProperty { get; set; }
    public AbstractProperty SeasonNumberProperty { get; set; }
    public AbstractProperty EpisodeNumberProperty { get; set; }
    public AbstractProperty EpisodeTitleProperty { get; set; }
    public AbstractProperty SeriesProperty { get; set; }
    public AbstractProperty ChannelNameProperty { get; set; }

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
      set { StartTimeProperty.SetValue(value); }
    }

    /// <summary>
    /// Gets or Sets the End time.
    /// </summary>
    public DateTime EndTime
    {
      get { return (DateTime)EndTimeProperty.GetValue(); }
      set { EndTimeProperty.SetValue(value); }
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
      get { return (bool)IsScheduledProperty.GetValue(); }
      set { IsScheduledProperty.SetValue(value); }
    }

    /// <summary>
    /// Gets or Sets an indicator if the program is scheduled or currently recording.
    /// </summary>
    public bool IsSeriesScheduled
    {
      get { return (bool)IsSeriesScheduledProperty.GetValue(); }
      set { IsSeriesScheduledProperty.SetValue(value); }
    }

    /// <summary>
    /// Gets or Sets an indicator if the program is scheduled or currently recording.
    /// </summary>
    public int ProgramId
    {
      get { return (int)ProgramIdProperty.GetValue(); }
      set { ProgramIdProperty.SetValue(value); }
    }

    /// <summary>
    /// Gets or Sets the SeasonNumber.
    /// </summary>
    public String SeasonNumber
    {
      get { return (String)SeasonNumberProperty.GetValue(); }
      set { SeasonNumberProperty.SetValue(value); }
    }

    /// <summary>
    /// Gets or Sets the EpisodeNumber.
    /// </summary>
    public String EpisodeNumber
    {
      get { return (String)EpisodeNumberProperty.GetValue(); }
      set { EpisodeNumberProperty.SetValue(value); }
    }

    /// <summary>
    /// Gets or Sets the EpisodeTitle.
    /// </summary>
    public String EpisodeTitle
    {
      get { return (String)EpisodeTitleProperty.GetValue(); }
      set { EpisodeTitleProperty.SetValue(value); }
    }

    /// <summary>
    /// Gets or Sets the formatted series number and title (i.e. "1.1 Pilot").
    /// </summary>
    public String Series
    {
      get { return (String)SeriesProperty.GetValue(); }
      set { SeriesProperty.SetValue(value); }
    }

    /// <summary>
    /// Gets or Sets the channel name.
    /// </summary>
    public String ChannelName
    {
      get { return (String)ChannelNameProperty.GetValue(); }
      set { ChannelNameProperty.SetValue(value); }
    }

    public ProgramProperties()
    {
      ProgramIdProperty = new WProperty(typeof(int), 0);
      IsScheduledProperty = new WProperty(typeof(bool), false);
      IsSeriesScheduledProperty = new WProperty(typeof(bool), false);
      TitleProperty = new WProperty(typeof(String), String.Empty);
      DescriptionProperty = new WProperty(typeof(String), String.Empty);
      GenreProperty = new WProperty(typeof(String), String.Empty);
      StartTimeProperty = new WProperty(typeof(DateTime), DateTime.MinValue);
      EndTimeProperty = new WProperty(typeof(DateTime), DateTime.MinValue);
      RemainingDurationProperty = new WProperty(typeof(int), 0);
      EpisodeNumberProperty = new WProperty(typeof(String), String.Empty);
      SeasonNumberProperty = new WProperty(typeof(String), String.Empty);
      EpisodeTitleProperty = new WProperty(typeof(String), String.Empty);
      SeriesProperty = new WProperty(typeof(String), String.Empty);
      ChannelNameProperty = new WProperty(typeof(String), String.Empty);
      Attach();
    }

    private void Attach()
    {
      StartTimeProperty.Attach(TimeChanged);
      EndTimeProperty.Attach(TimeChanged);
    }

    private void TimeChanged(AbstractProperty property, object oldvalue)
    {
      if (!_settingProgram)
        UpdateDuration();
    }

    public void UpdateState(RecordingStatus recordingStatus)
    {
      IsScheduled = recordingStatus != RecordingStatus.None; // Can be single or series
      IsSeriesScheduled = recordingStatus == RecordingStatus.SeriesScheduled;
    }

    public void SetProgram(IProgram program, IChannel channel = null)
    {
      IProgramRecordingStatus recordingStatus = program as IProgramRecordingStatus;
      if (recordingStatus != null)
      {
        UpdateState(recordingStatus.RecordingStatus);
      }
      try
      {
        if (channel != null)
          ChannelName = channel.Name;
        else if (program != null)
        {
          IChannelAndGroupInfo channelAndGroupInfo = ServiceRegistration.Get<ITvHandler>().ChannelAndGroupInfo;
          if (channelAndGroupInfo != null && channelAndGroupInfo.GetChannel(program.ChannelId, out channel))
            ChannelName = channel.Name;
        }

        _settingProgram = true;
        IProgramSeries series = program as IProgramSeries;
        if (series != null)
        {
          SeasonNumber = series.SeasonNumber;
          EpisodeNumber = series.EpisodeNumber;
          EpisodeTitle = series.EpisodeTitle;
          Series = BuildSeriesText(this);
        }
        else
        {
          SeasonNumber = string.Empty;
          EpisodeNumber = string.Empty;
          EpisodeTitle = string.Empty;
          Series = string.Empty;
        }
        if (program != null)
        {
          ProgramId = program.ProgramId;
          Title = program.Title;
          Description = program.Description;
          StartTime = program.StartTime;
          EndTime = program.EndTime;
          Genre = program.Genre;
        }
        else
        {
          ProgramId = 0;
          Title = string.Empty;
          Description = string.Empty;
          StartTime = DateTime.Now.GetDay();
          EndTime = StartTime.AddDays(1);
          Genre = string.Empty;
        }
        UpdateDuration();
      }
      finally
      {
        _settingProgram = false;
      }
    }

    private void UpdateDuration()
    {
      DateTime programStart = StartTime;
      DateTime programEnd = EndTime;
      RemainingDuration = Math.Max((int)(programEnd - programStart).TotalMinutes, 0);
    }

    public static string BuildSeriesText(ProgramProperties program)
    {
      if (string.IsNullOrEmpty(program.SeasonNumber) && string.IsNullOrEmpty(program.EpisodeNumber))
        return null;

      return string.Format("{0}{1}{2} {3}", program.SeasonNumber, string.IsNullOrEmpty(program.SeasonNumber) ? "" : ".", program.EpisodeNumber, program.EpisodeTitle).TrimEnd();
    }
  }
}
