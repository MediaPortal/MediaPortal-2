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
using MediaPortal.Plugins.MP2Extended.TAS.Tv;
using MediaPortal.Plugins.SlimTv.Interfaces;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.TAS.EPG.BaseClasses
{
  class BaseProgramBasic
  {
    internal static WebProgramBasic ProgramBasic(IProgram program)
    {
      if (program == null)
        return new WebProgramBasic();

      IProgramRecordingStatus recordingStatus = program as IProgramRecordingStatus;

      WebProgramBasic webProgramBasic = new WebProgramBasic
      {
        Description = program.Description,
        ChannelId = program.ChannelId,
        StartTime = program.StartTime,
        EndTime = program.EndTime,
        Title = program.Title,
        Id = program.ProgramId,
        DurationInMinutes = Convert.ToInt32(program.EndTime.Subtract(program.StartTime).TotalMinutes),
        IsScheduled = recordingStatus?.RecordingStatus != RecordingStatus.None
      };

      return webProgramBasic;
    }
  }
}
