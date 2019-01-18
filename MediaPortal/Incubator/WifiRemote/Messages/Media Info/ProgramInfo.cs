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
  public class ProgramInfo : IAdditionalMediaInfo
  {
    public string MediaType => "program";
    public string MpExtId => ProgramId.ToString();
    public int MpExtMediaType => (int)MpExtendedMediaTypes.Tv;
    public int MpExtProviderId => 0; //no tv providers yet

    /// <summary>
    /// Id of program
    /// </summary>
    public int ProgramId { get; set; }
    /// <summary>
    /// Name of program
    /// </summary>
    public string Name { get; set; }
    /// <summary>
    /// Description of program
    /// </summary>
    public string Description { get; set; }
    /// <summary>
    /// Start date of program
    /// </summary>
    public DateTime StartTime { get; set; }
    /// <summary>
    /// End date of program
    /// </summary>
    public DateTime EndTime { get; set; }
    /// <summary>
    /// Id of channel
    /// </summary>
    public int ChannelId { get; set; }

    public ProgramInfo(IProgram program)
    {
      ProgramId = program.ProgramId;
      Name = program.Title;
      ChannelId = program.ChannelId;
      Description = program.Description;
      StartTime = program.StartTime;
      EndTime = program.EndTime;
    }
  }
}
