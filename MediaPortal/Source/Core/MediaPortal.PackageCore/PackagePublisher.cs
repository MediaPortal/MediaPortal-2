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

using System;
using System.IO;
using System.Net;
using MediaPortal.Common.Logging;
using MediaPortal.Common.PluginManager.Packages.ApiEndpoints;
using MediaPortal.Common.PluginManager.Packages.DataContracts.Authors;
using MediaPortal.Common.PluginManager.Packages.DataContracts.Enumerations;

namespace MediaPortal.PackageCore
{
  public class PackagePublisher : Requestor
  {
    public PackagePublisher(ILogger log) : base(log)
    { }

    public bool Publish(string userName, string password, string host, string packageFilePath, PackageType packageType, string[] categoryTags)
    {
      var proxy = new RequestExecutionHelper(userName, password, host);
      var model = new PublishPackageModel(packageFilePath, packageType, categoryTags);
      var response = proxy.ExecuteRequest(PackageServerApi.Author.PublishPackage, model);

      var successMessage = string.Format("The package '{0}' has been published.{1}"
                                         + "Hint: if this was a mistake, you can use the 'recall' command to remove the package from the server.",
        Path.GetFileName(packageFilePath), Environment.NewLine);
      return IsSuccess(response, successMessage, HttpStatusCode.OK, HttpStatusCode.Created);
    }

    public bool Recall(string userName, string password, string host, string name, string version)
    {
      var proxy = new RequestExecutionHelper(userName, password, host);
      var model = new RecallPackageModel(name, version);
      var response = proxy.ExecuteRequest(PackageServerApi.Author.RecallPackage, model);

      var successMessage = string.Format("The release '{0}' of package '{1}' has been recalled and is no longer available.",
        version, name);
      return IsSuccess(response, successMessage, HttpStatusCode.OK);
    }
  }
}