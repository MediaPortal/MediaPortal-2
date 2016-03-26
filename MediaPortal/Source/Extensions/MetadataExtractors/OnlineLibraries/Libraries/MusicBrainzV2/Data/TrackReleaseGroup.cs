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

using System.Collections.Generic;
using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.MusicBrainzV2.Data
{
  [DataContract]
  public class TrackReleaseGroup
  {
    [DataMember(Name = "id")]
    public string Id { get; set; }

    [DataMember(Name = "primary-type")]
    public string PrimaryType { get; set; }

    [DataMember(Name = "secondary-types")]
    public IList<string> SecondaryTypes { get; set; }

    public override string ToString()
    {
      return string.Format("Id: {0}, PrimaryType: {1}", Id, PrimaryType);
    }
  }
}
