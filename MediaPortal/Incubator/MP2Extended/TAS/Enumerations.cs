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

using System.Runtime.Serialization;

namespace MediaPortal.Plugins.MP2Extended.TAS
{
    [DataContract]
    public enum ChannelState
    {
        [EnumMember]
        Unknown = -1,
        [EnumMember]
        NotTunable = 0,
        [EnumMember]
        Tunable = 1,
        [EnumMember]
        Timeshifting = 2,
        [EnumMember]
        Recording = 3
    }

    [DataContract]
    public enum WebScheduleType
    {
        // UI: Once
        [EnumMember]
        Once = 0,

        // UI: Every day at this time
        [EnumMember]
        Daily = 1,

        // UI: Every week at this time
        [EnumMember]
        Weekly = 2,

        // UI: Every time on this channel (starttime ignored)
        [EnumMember]
        EveryTimeOnThisChannel = 3,

        // UI: Every time on every channel (starttime ignored)
        [EnumMember]
        EveryTimeOnEveryChannel = 4,

        // UI: Weekends
        [EnumMember]
        Weekends = 5,

        // UI: Weekdays 
        [EnumMember]
        WorkingDays = 6,

        // UI: Weekly on this channel (starttime ignored)
        [EnumMember]
        WeeklyEveryTimeOnThisChannel = 7
    }

    [DataContract]
    public enum WebScheduleKeepMethod
    {
        [EnumMember]
        UntilSpaceNeeded = 0,
        [EnumMember]
        UntilWatched = 1,
        [EnumMember]
        TillDate = 2,
        [EnumMember]
        Always = 3
    }

    // See TvEngine3/TVLibrary/TvLibrary.Interfaces/CardType.cs
    [DataContract]
    public enum WebCardType
    {
        [EnumMember]
        Analog = 0,
        [EnumMember]
        DvbS = 1,
        [EnumMember]
        DvbT = 2,
        [EnumMember]
        DvbC = 3,
        [EnumMember]
        Atsc = 4,
        [EnumMember]
        RadioWebStream = 5,
        [EnumMember]
        DvbIP = 6,
        [EnumMember]
        Unknown = 7
    }
}
