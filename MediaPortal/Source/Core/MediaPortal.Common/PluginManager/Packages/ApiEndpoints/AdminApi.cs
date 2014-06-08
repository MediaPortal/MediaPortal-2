#region Copyright (C) 2007-2014 Team MediaPortal

/*
    Copyright (C) 2007-2014 Team MediaPortal
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

using MediaPortal.Common.PluginManager.Packages.DataContracts.UserAdmin;

namespace MediaPortal.Common.PluginManager.Packages.ApiEndpoints
{
  /// <summary>
  /// API endpints for administative users.
  /// </summary>
  public class AdminApi : ApiBase
  {
    private const string BASE_PATH = "admin";
    private static readonly ApiEndpoint CREATE_USER_API_ENDPOINT = ApiEndpoint.Post<CreateUserModel>(BASE_PATH, "create-user");
    private static readonly ApiEndpoint REVOKE_USER_API_ENDPOINT = ApiEndpoint.Post<RevokeUserModel>(BASE_PATH, "revoke-user");

    public AdminApi() : base(BASE_PATH)
    {
    }

    public static ApiEndpoint CreateUser
    {
      get { return CREATE_USER_API_ENDPOINT; }
    }

    public static ApiEndpoint RevokeUser
    {
      get { return REVOKE_USER_API_ENDPOINT; }
    }
  }
}