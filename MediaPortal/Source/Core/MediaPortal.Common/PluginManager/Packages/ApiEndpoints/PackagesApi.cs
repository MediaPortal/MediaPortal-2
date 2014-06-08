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

using System.Collections.Generic;
using MediaPortal.Common.PluginManager.Packages.DataContracts;
using MediaPortal.Common.PluginManager.Packages.DataContracts.Packages;

namespace MediaPortal.Common.PluginManager.Packages.ApiEndpoints
{
  /// <summary>
  /// API endpints for users and system components.
  /// </summary>
  public class PackagesApi : ApiBase
  {
    private const string BASE_PATH = "packages";
    private static readonly ApiEndpoint LIST_PACKAGES_API_ENDPOINT = ApiEndpoint.Post<PackageQuery>(BASE_PATH, "list");

    private static readonly ApiEndpoint PACKAGE_DETAILS_API_ENDPOINT = ApiEndpoint.Get<PackageInfo>(BASE_PATH, "{id:long:min(1)}/details");
    private static readonly ApiEndpoint PACKAGE_RELEASES_API_ENDPOINT = ApiEndpoint.Get<IList<ReleaseInfo>>(BASE_PATH, "{id:long:min(1)}/releases");

    private static readonly ApiEndpoint PACKAGE_REVIEWS_API_ENDPOINT = ApiEndpoint.Get<IList<ReviewInfo>>(BASE_PATH, "{id:long:min(1)}/reviews");
    private static readonly ApiEndpoint ADD_REVIEW_API_ENDPOINT = ApiEndpoint.Post<AddPackageReviewModel>(BASE_PATH, "{id:long:min(1)}/reviews/add");

    private static readonly ApiEndpoint FIND_RELEASE_API_ENDPOINT = ApiEndpoint.Post<PackageReleaseQuery, ReleaseInfo>(BASE_PATH, "find-release");
    private static readonly ApiEndpoint UPDATE_CHECK_API_ENDPOINT = ApiEndpoint.Post<PackageUpdateQuery, IList<ReleaseInfo>>(BASE_PATH, "update-check");

    public PackagesApi() : base(BASE_PATH)
    {
    }

    public static ApiEndpoint List
    {
      get { return LIST_PACKAGES_API_ENDPOINT; }
    }

    public static ApiEndpoint Details
    {
      get { return PACKAGE_DETAILS_API_ENDPOINT; }
    }

    public static ApiEndpoint Releases
    {
      get { return PACKAGE_RELEASES_API_ENDPOINT; }
    }

    public static ApiEndpoint Reviews
    {
      get { return PACKAGE_REVIEWS_API_ENDPOINT; }
    }

    public static ApiEndpoint AddReview
    {
      get { return ADD_REVIEW_API_ENDPOINT; }
    }

    public static ApiEndpoint FindRelease
    {
      get { return FIND_RELEASE_API_ENDPOINT; }
    }

    public static ApiEndpoint UpdateCheck
    {
      get { return UPDATE_CHECK_API_ENDPOINT; }
    }
  }
}