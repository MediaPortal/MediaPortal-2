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

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures
{


  [DataContract]
  public class TraktOAuthLogin
  {
    [DataMember(Name = "code", EmitDefaultValue = false)]
    public string Code { get; set; }

    [DataMember(Name = "refresh_token", EmitDefaultValue = false)]
    public string RefreshToken { get; set; }

    [DataMember(Name = "client_id")]
    public string ClientId { get; set; }

    [DataMember(Name = "client_secret")]
    public string ClientSecret { get; set; }

    [DataMember(Name = "redirect_uri")]
    public string RedirectUri { get; set; }

    [DataMember(Name = "grant_type")]
    public string GrantType { get; set; }
  }
}
