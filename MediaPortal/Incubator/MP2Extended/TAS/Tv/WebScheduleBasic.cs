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

namespace MediaPortal.Plugins.MP2Extended.TAS.Tv
{
    public class WebScheduleBasic
    {
        public int BitRateMode { get; set; }
        public DateTime Canceled { get; set; }
        public string Directory { get; set; }
        public bool DoesUseEpisodeManagement { get; set; }
        public DateTime EndTime { get; set; }
        public int ChannelId { get; set; }
        public int ParentScheduleId { get; set; }
        public int Id { get; set; }
        public bool IsChanged { get; set; }
        public bool IsManual { get; set; }
        public DateTime KeepDate { get; set; }
        public WebScheduleKeepMethod KeepMethod { get; set; }
        public int MaxAirings { get; set; }
        public int PostRecordInterval { get; set; }
        public int PreRecordInterval { get; set; }
        public int Priority { get; set; }
        public string Title { get; set; }
        public int Quality { get; set; }
        public int QualityType { get; set; }
        public int RecommendedCard { get; set; }
        public WebScheduleType ScheduleType { get; set; }
        public bool Series { get; set; }
        public DateTime StartTime { get; set; }
    }
}
