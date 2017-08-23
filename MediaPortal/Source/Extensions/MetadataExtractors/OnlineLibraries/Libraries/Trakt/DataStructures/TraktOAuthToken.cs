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
  public class TraktOAuthToken
  {
    [DataMember(Name = "access_token")]
    public string AccessToken { get; set; }

    [DataMember(Name = "token_type")]
    public string TokenType { get; set; }

    [DataMember(Name = "expires_in")]
    public int ExpiresIn { get; set; }

    [DataMember(Name = "refresh_token")]
    public string RefreshToken { get; set; }

    [DataMember(Name = "scope")]
    public string Scope { get; set; }
  }
}
