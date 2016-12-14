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
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures
{
  [DataContract]
  public class TraktSyncPaused
  {
    [DataMember(Name = "progress")]
    public float Progress { get; set; }

    [DataMember(Name = "type")]
    public string Type { get; set; }

    [DataMember(Name = "paused_at")]
    public string PausedAt { get; set; }
  }

  [DataContract]
  public class TraktSyncPausedMovie : TraktSyncPaused
  {
    [DataMember(Name = "movie")]
    public TraktMovie Movie { get; set; }
  }

  [DataContract]
  public class TraktSyncPausedEpisode : TraktSyncPaused
  {
    [DataMember(Name = "show")]
    public TraktShow Show { get; set; }

    [DataMember(Name = "episode")]
    public TraktEpisode Episode { get; set; }
  }
}
