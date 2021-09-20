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

namespace MediaPortal.Plugins.MP2Extended.TAS.Misc
{
    public class WebCard
    {
        public bool CAM { get; set; }
        public int CamType { get; set; }
        public int DecryptLimit { get; set; }
        public string DevicePath { get; set; }
        public bool Enabled { get; set; }
        public bool GrabEPG { get; set; }
        public int Id { get; set; }
        public bool IsChanged { get; set; }
        public DateTime LastEpgGrab { get; set; }
        public string Name { get; set; }
        public int NetProvider { get; set; }
        public bool PreloadCard { get; set; }
        public int Priority { get; set; }
        public string RecordingFolder { get; set; }
        public int RecordingFormat { get; set; }
        public bool SupportSubChannels { get; set; }
        public string TimeShiftFolder { get; set; }
    }
}
