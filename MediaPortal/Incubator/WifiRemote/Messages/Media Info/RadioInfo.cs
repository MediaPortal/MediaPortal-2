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
using System.Linq;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Plugins.SlimTv.Interfaces;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;
using MediaPortal.Plugins.SlimTv.Interfaces.LiveTvMediaItem;
using MediaPortal.Plugins.WifiRemote.Messages.MediaInfo;

namespace MediaPortal.Plugins.WifiRemote
{
  internal class RadioInfo : IAdditionalMediaInfo
  {
    public string MediaType => "radio";
    public string Id => ChannelId.ToString();
    public int MpMediaType => (int)MpMediaTypes.Tv;
    public int MpProviderId => 0; //no radio providers yet

    /// <summary>
    /// ID of the current channel
    /// </summary>
    public int ChannelId { get; set; }
    /// <summary>
    /// Name of the current channel
    /// </summary>
    public string ChannelName { get; set; }
    /// <summary>
    /// Name of the current artits
    /// </summary>
    public string ArtistName { get; set; }
    /// <summary>
    /// Id of current program
    /// </summary>
    public int CurrentProgramId { get; set; }
    /// <summary>
    /// Name of current program
    /// </summary>
    public string CurrentProgramName { get; set; }
    /// <summary>
    /// Description of current program
    /// </summary>
    public string CurrentProgramDescription { get; set; }
    /// <summary>
    /// Start date of current program
    /// </summary>
    public DateTime CurrentProgramBegin { get; set; }
    /// <summary>
    /// End date of current program
    /// </summary>
    public DateTime CurrentProgramEnd { get; set; }
    /// <summary>
    /// Id of next program
    /// </summary>
    public int NextProgramId { get; set; }
    /// <summary>
    /// Name of next program
    /// </summary>
    public string NextProgramName { get; set; }
    /// <summary>
    /// Description of next program
    /// </summary>
    public string NextProgramDescription { get; set; }
    /// <summary>
    /// Start date of next program
    /// </summary>
    public DateTime NextProgramBegin { get; set; }
    /// <summary>
    /// End date of next program
    /// </summary>
    public DateTime NextProgramEnd { get; set; }
    /// <summary>
    /// <code>true</code> if the program is a web stream
    /// </summary>
    public bool IsWebStream { get; set; }
    /// <summary>
    /// Url of the program
    /// </summary>
    public string CurrentUrl { get; set; }

    // TODO: reimplement
    /// <summary>
    /// Constructor
    /// </summary>
    public RadioInfo(MediaItem mediaItem)
    {
      try
      {
        if (mediaItem is LiveTvMediaItem radioItem && radioItem.TimeshiftContexes.FirstOrDefault()?.Channel is IChannel channel)
        {
          ITvHandler tvHandler = ServiceRegistration.Get<ITvHandler>();
          var result = tvHandler.ProgramInfo.GetNowNextProgramAsync(channel).Result;

          ChannelId = channel.ChannelId;
          ChannelName = channel.Name;

          if (result.Success)
          {
            CurrentProgramId = result.Result[0].ProgramId;
            CurrentProgramName = result.Result[0].Title;
            CurrentProgramDescription = result.Result[0].Description;
            CurrentProgramBegin = result.Result[0].StartTime;
            CurrentProgramEnd = result.Result[0].EndTime;

            NextProgramId = result.Result[1].ProgramId;
            NextProgramName = result.Result[1].Title;
            NextProgramDescription = result.Result[1].Description;
            NextProgramBegin = result.Result[1].StartTime;
            NextProgramEnd = result.Result[1].EndTime;
          }
        }
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Error("WifiRemote: Error getting radio info", e);
      }
    }
  }
}
