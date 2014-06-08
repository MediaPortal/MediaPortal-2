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

namespace MediaPortal.Common.PluginManager.Packages.ApiEndpoints
{
  /// <summary>
  /// Route definitions for package server endpoints to facilitate discovery and ease of access.
  /// </summary>
  public static class PackageServerApi
  {
    private static readonly AdminApi ADMIN_API = new AdminApi();
    private static readonly AuthorApi AUTHOR_API = new AuthorApi();
    private static readonly PackagesApi PACKAGES_API = new PackagesApi();

    public static AdminApi Admin
    {
      get { return ADMIN_API; }
    }

    public static AuthorApi Author
    {
      get { return AUTHOR_API; }
    }

    public static PackagesApi Packages
    {
      get { return PACKAGES_API; }
    }
  }
}