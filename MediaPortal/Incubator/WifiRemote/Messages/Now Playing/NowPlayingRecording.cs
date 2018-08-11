#region Copyright (C) 2007-2015 Team MediaPortal

/*
    Copyright (C) 2007-2015 Team MediaPortal
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
using MediaPortal.Plugins.WifiRemote.Messages.Now_Playing;

namespace MediaPortal.Plugins.WifiRemote
{
  public class NowPlayingRecording : IAdditionalNowPlayingInfo
  {
    private string mediaType = "recording";
    private bool recordingFound = false;

    public string MediaType
    {
      get { return mediaType; }
    }

    public string MpExtId
    {
      get { return RecordingId.ToString(); }
    }

    public int MpExtMediaType
    {
      get { return (int)MpExtendedMediaTypes.Recording; }
    }

    public int MpExtProviderId
    {
      get { return 0; } //no tv providers yet
    }

    /// <summary>
    /// ID of the channel
    /// </summary>
    public int ChannelId { get; set; }

    /// <summary>
    /// Id of recording
    /// </summary>
    public int RecordingId { get; set; }

    /// <summary>
    /// Name of channel
    /// </summary>
    public String ChannelName { get; set; }

    /// <summary>
    /// Name of program
    /// </summary>
    public string ProgramName { get; set; }

    /// <summary>
    /// Description of program
    /// </summary>
    public string ProgramDescription { get; set; }

    /// <summary>
    /// Start date of program
    /// </summary>
    public DateTime ProgramBegin { get; set; }

    /// <summary>
    /// End date of program
    /// </summary>
    public DateTime ProgramEnd { get; set; }

    // TODO: reimplement
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="filename">The currently playing recording</param>
    public NowPlayingRecording(string filename)
    {
      /*TvDatabase.Recording recording = TvDatabase.Recording.Retrieve(filename);
      if (recording != null)
      {
        recordingFound = true;
        ChannelId = recording.IdChannel;
        RecordingId = recording.IdRecording;
        ProgramName = recording.Title;
        ProgramDescription = recording.Description;
        ProgramBegin = recording.StartTime;
        ProgramEnd = recording.EndTime;

        TvDatabase.Channel channel = TvDatabase.Channel.Retrieve(ChannelId);
        if (channel != null)
        {
          ChannelName = channel.DisplayName;
        }
      }*/
    }

    public bool IsRecording()
    {
      return recordingFound;
    }
  }
}
