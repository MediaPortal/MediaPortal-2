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

namespace MediaPortal.Plugins.SlimTv.UPnP
{
  public class Consts
  {
    public const string SLIMTV_SERVICE_TYPE = "schemas-team-mediaportal-com:service:SlimTv";
    public const int SLIMTV_SERVICE_TYPE_VERSION = 1;
    public const string SLIMTV_SERVICE_ID = "urn:team-mediaportal-com:serviceId:SlimTv";

    public const string ACTION_START_TIMESHIFT = "StartTimeshift";
    public const string ACTION_STOP_TIMESHIFT = "StopTimeshift";
    public const string ACTION_DEINIT = "DeInit";
    public const string ACTION_GET_CHANNELGROUPS = "GetChannelGroups";
    public const string ACTION_GET_CHANNELS = "GetChannels";
    public const string ACTION_GET_PROGRAMS = "GetPrograms";
    public const string ACTION_GET_PROGRAMS_GROUP = "GetProgramsGroup";
    public const string ACTION_GET_NOW_NEXT_PROGRAM = "GetNowNextProgram";
    public const string ACTION_CREATE_SCHEDULE = "CreateSchedule";
    public const string ACTION_CREATE_SCHEDULE_BY_TIME = "CreateScheduleByTime";
    public const string ACTION_REMOVE_SCHEDULE = "RemoveSchedule";
    public const string ACTION_GET_REC_STATUS = "GetRecordingStatus";
  }
}
