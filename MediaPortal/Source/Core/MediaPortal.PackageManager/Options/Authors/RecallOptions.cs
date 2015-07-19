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

using CommandLine;
using MediaPortal.PackageManager.Options.Shared;

namespace MediaPortal.PackageManager.Options.Authors
{
  internal class RecallOptions : AuthOptions
  {
    [Option('n', "name", HelpText = "Name of the plugin (as defined in the plugin descriptor file) for the package to recall.", Required = true, MutuallyExclusiveSet = "ident")]
    public string Name { get; set; }

    //[Option('g', "guid", HelpText="GUID of the plugin (as defined in the plugin descriptor file) for the package to recall.", Required = true, MutuallyExclusiveSet = "ident")]
    //public string Guid { get; set; }

    [Option('v', "version", HelpText = "Exact version of the package release to recall.", Required = true, MutuallyExclusiveSet = "release")]
    public string Version { get; set; }

    [Option('r', "release", HelpText = "ID of the package release to recall.", Required = true, MutuallyExclusiveSet = "release")]
    public long? ReleaseID { get; set; }
  }
}