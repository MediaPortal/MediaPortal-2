#region Copyright (C) 2007-2020 Team MediaPortal

/*
    Copyright (C) 2007-2020 Team MediaPortal
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
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Plugins.SlimTv.Interfaces.Aspects;
using MediaPortal.Plugins.WifiRemote.Messages.MediaInfo;

namespace MediaPortal.Plugins.WifiRemote
{
  internal class RecordingInfo : IAdditionalMediaInfo
  {
    public string MediaType => "recording";
    public string Id => RecordingId.ToString();
    public int MpMediaType => (int)MpMediaTypes.Recording;
    public int MpProviderId => (int)MpProviders.MPTvServer;

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
    public RecordingInfo(MediaItem mediItem)
    {
      try
      {
        var mediaAspect = MediaItemAspect.GetAspect(mediItem.Aspects, MediaAspect.Metadata);
        var videoAspect = MediaItemAspect.GetAspect(mediItem.Aspects, VideoAspect.Metadata);
        var recordingAspect = MediaItemAspect.GetAspect(mediItem.Aspects, RecordingAspect.Metadata);
        if (recordingAspect != null)
        {
          ChannelId = recordingAspect.GetAttributeValue<string>(RecordingAspect.ATTR_CHANNEL).GetHashCode();
          ChannelName = recordingAspect.GetAttributeValue<string>(RecordingAspect.ATTR_CHANNEL);
          RecordingId = mediItem.MediaItemId.GetHashCode();
          ProgramName = mediaAspect?.GetAttributeValue<string>(MediaAspect.ATTR_TITLE);
          ProgramDescription = videoAspect?.GetAttributeValue<string>(VideoAspect.ATTR_STORYPLOT);
          ProgramBegin = recordingAspect.GetAttributeValue<DateTime>(RecordingAspect.ATTR_STARTTIME);
          ProgramEnd = recordingAspect.GetAttributeValue<DateTime>(RecordingAspect.ATTR_ENDTIME);
        }
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Error("WifiRemote: Error getting recording info", e);
      }
    }
  }
}
