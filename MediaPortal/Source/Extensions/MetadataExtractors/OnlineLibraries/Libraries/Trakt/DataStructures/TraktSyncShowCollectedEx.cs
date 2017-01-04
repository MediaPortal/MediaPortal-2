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

using System.Collections.Generic;
using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures
{
  [DataContract]
  public class TraktSyncShowCollectedEx : TraktShow
  {
    [DataMember(Name = "seasons")]
    public List<Season> Seasons { get; set; }

    [DataContract]
    public class Season
    {
      [DataMember(Name = "number")]
      public int Number { get; set; }

      [DataMember(Name = "episodes")]
      public List<Episode> Episodes { get; set; }

      [DataContract]
      public class Episode
      {
        [DataMember(Name = "number")]
        public int Number { get; set; }

        [DataMember(Name = "collected_at")]
        public string CollectedAt { get; set; }

        [DataMember(Name = "media_type")]
        public string MediaType { get; set; }

        [DataMember(Name = "resolution")]
        public string Resolution { get; set; }

        [DataMember(Name = "audio")]
        public string AudioCodec { get; set; }

        [DataMember(Name = "audio_channels")]
        public string AudioChannels { get; set; }

        [DataMember(Name = "3d")]
        public bool Is3D { get; set; }
      }
    }
  }
}