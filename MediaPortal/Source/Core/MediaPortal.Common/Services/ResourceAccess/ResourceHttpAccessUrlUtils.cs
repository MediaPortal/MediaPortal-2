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
using System.Collections.Specialized;
using System.Web;
using MediaPortal.Common.ResourceAccess;

namespace MediaPortal.Common.Services.ResourceAccess
{
  /// <summary>
  /// Helper class to create and parse URLs which are used to query resources in MediaPortal 2 via HTTP.
  /// </summary>
  public class ResourceHttpAccessUrlUtils
  {
    /// <summary>
    /// Base HTTP path for resource access, e.g. "/GetResource".
    /// </summary>
    public const string RESOURCE_ACCESS_PATH = "/GetResource";

    /// <summary>
    /// Argument name for the resource path argument, e.g. "ResourcePath".
    /// </summary>
    public const string RESOURCE_PATH_ARGUMENT_NAME = "ResourcePath";

    public const string SYNTAX = RESOURCE_ACCESS_PATH + "?" + RESOURCE_PATH_ARGUMENT_NAME + "=[resource path]";

    public static string GetResourceURL(string baseURL, ResourcePath nativeResourcePath)
    {
      // Use UrlEncode to encode also # sign, UrlPathEncode doesn't do this.
      return string.Format("{0}?{1}={2}", baseURL, RESOURCE_PATH_ARGUMENT_NAME, HttpUtility.UrlEncode(nativeResourcePath.Serialize()));
    }

    public static bool ParseResourceURI(Uri resourceURI, out ResourcePath relativeResourcePath)
    {
      NameValueCollection query =  HttpUtility.ParseQueryString(resourceURI.Query);
      string resourcePathStr = query[RESOURCE_PATH_ARGUMENT_NAME];
      try
      {
        relativeResourcePath = ResourcePath.Deserialize(resourcePathStr);
        return true;
      }
      catch (ArgumentException)
      {
        relativeResourcePath = null;
        return false;
      }
    }
  }
}