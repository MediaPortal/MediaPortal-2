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
using MediaPortal.Plugins.SlimTvClient.Interfaces.Items;
using MPExtended.Services.TVAccessService.Interfaces;

namespace MediaPortal.Plugins.SlimTvClient.Providers.Items
{
  public class Program : IProgramRecordingStatus
  {
    public Program()
    {}

    public Program(WebProgramDetailed webProgram, int serverIndex)
    {
      ServerIndex = serverIndex;
      Description = webProgram.Description;
      StartTime = webProgram.StartTime;
      EndTime = webProgram.EndTime;
      Genre = webProgram.Genre;
      Title = webProgram.Title;
      ChannelId = webProgram.ChannelId;
      ProgramId = webProgram.Id;
      RecordingStatus = GetRecordingStatus(webProgram);
    }

    public static RecordingStatus GetRecordingStatus(WebProgramBasic programDetailed)
    {
      RecordingStatus recordingStatus = RecordingStatus.None;
      if (programDetailed.IsScheduled)
        recordingStatus |= RecordingStatus.Scheduled;
      return recordingStatus;
    }

    public static RecordingStatus GetRecordingStatus(WebProgramDetailed programDetailed)
    {
      RecordingStatus recordingStatus = RecordingStatus.None;
      if (programDetailed.IsRecording || programDetailed.IsRecordingOnce || programDetailed.IsRecordingSeries)
        recordingStatus |= RecordingStatus.Recording;
      if (programDetailed.IsScheduled || programDetailed.IsRecordingOncePending)
        recordingStatus |= RecordingStatus.Scheduled;
      if (programDetailed.IsRecordingSeriesPending)
        recordingStatus |= RecordingStatus.SeriesScheduled;
      return recordingStatus;
    }

    #region IProgram Member

    public int ServerIndex { get; set; }
    public int ProgramId { get; set; }
    public int ChannelId { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public string Genre { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public RecordingStatus RecordingStatus { get; set; }

    #endregion
  }
}