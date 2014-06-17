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

using System.Web.Optimization;
using MediaPortal.PackageServer.Initialization.Core;

namespace MediaPortal.PackageServer.Initialization
{
  public class BundleConfig : IConfigurationTask
  {
    public void Configure()
    {
      RegisterBundles(BundleTable.Bundles);
    }

    // For more information on bundling, visit http://go.microsoft.com/fwlink/?LinkId=301862
    private static void RegisterBundles(BundleCollection bundles)
    {
      bundles.Add(new ScriptBundle("~/bundles/core").Include(
        "~/Scripts/jquery-{version}.js",
        "~/Scripts/dust-full.js"));

      bundles.Add(new ScriptBundle("~/bundles/site").Include(
        "~/Scripts/proto.js"));

      // Use the development version of Modernizr to develop with and learn from. Then, when you're
      // ready for production, use the build tool at http://modernizr.com to pick only the tests you need.
      bundles.Add(new ScriptBundle("~/bundles/modernizr").Include(
        "~/Scripts/modernizr-*"));

      //bundles.Add(new ScriptBundle("~/bundles/bootstrap").Include(
      //  "~/Scripts/bootstrap.js",
      //  "~/Scripts/respond.js"));

      bundles.Add(new StyleBundle("~/Content/css").Include(
        "~/Content/css/reset.css",
        "~/Content/css/proto.css"));
    }
  }
}
