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

using System;
using System.Net;
using MediaPortal.Common.Logging;
using MediaPortal.Common.PluginManager.Packages.ApiEndpoints;
using MediaPortal.Common.PluginManager.Packages.DataContracts.UserAdmin;

namespace MediaPortal.PackageCore
{
  public class PackageAdmin : Requestor
  {
    public PackageAdmin(ILogger log) : base(log)
    { }

    public bool CreateUser(string userName, string password, string login, string secret, string name, string email, string culture)
    {
      var proxy = new RequestExecutionHelper(userName, password);
      var model = new CreateUserModel(login, secret, name, email, culture);
      var response = proxy.ExecuteRequest(PackageServerApi.Admin.CreateUser, model);

      var successMessage = string.Format("The user '{0}' has been created.{1}"
                                         + "Hint: if this was a mistake, you can use the 'revoke-user' command to disable the user account.",
        login, Environment.NewLine);
      return IsSuccess(response, successMessage, HttpStatusCode.OK, HttpStatusCode.Created);
    }

    public bool RevokeUser(string userName, string password, string login, string reason)
    {
      var proxy = new RequestExecutionHelper(userName, password);
      var model = new RevokeUserModel(login, reason);
      var response = proxy.ExecuteRequest(PackageServerApi.Admin.RevokeUser, model);

      var successMessage = string.Format("The user '{0}' has been revoked.", login);
      return IsSuccess(response, successMessage, HttpStatusCode.OK);
    }
  }
}