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
using MediaPortal.Plugins.MP2Extended.Common;

namespace MediaPortal.Plugins.MP2Extended.WSS.General
{
    public class WebStreamingSession
    {
        public string Profile { get; set; }
        public string Identifier { get; set; }

        public WebMediaType SourceType { get; set; }
        public string SourceId { get; set; }
        public string DisplayName { get; set; }

        public string ClientDescription { get; set; }
        // Do not actually parse this as an IP address, because it isn't: it might have some comments in it too
        public string ClientIPAddress { get; set; }
        public DateTime StartTime { get; set; }

        /// <summary>
        /// The place where the user started the playback. 
        /// </summary>
        public long StartPosition { get; set; }

        /// <summary>
        /// The current place of the player.
        /// </summary>
        public long PlayerPosition { get; set; }

        /// <summary>
        /// Percentage of the file the user is at
        /// </summary>
        public int PercentageProgress { get; set; }

        public WebTranscodingInfo TranscodingInfo { get; set; }
    }
}
