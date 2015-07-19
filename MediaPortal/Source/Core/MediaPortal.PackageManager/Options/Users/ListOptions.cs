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

using System.Collections.Generic;
using CommandLine;
using MediaPortal.Common.PluginManager.Packages.DataContracts.Enumerations;

namespace MediaPortal.PackageManager.Options.Users
{
  internal class ListOptions : SharedOptions
  {
    [Option('t', "type", HelpText = "Filters the package list to the given package type (Client or Server).")]
    public PackageType PackageType { get; set; }

    [Option('n', "name", HelpText = "Filters the package list using the given partial package name.", Required = false)]
    public string PackageName { get; set; }

    [Option("author", HelpText = "Filters the package list using the given partial author name.", Required = false)]
    public string AuthorText { get; set; }

    [Option("desc", HelpText = "Expands the search using package name to also search descriptions.", Required = false)]
    public bool SearchDescriptions { get; set; }

    [Option("all", HelpText = "Show all packages, regardless of compatibility.", Required = false)]
    public bool All { get; set; }

    [Option("tags", HelpText = "Filters the package list to contain only packages with matching tags.", Required = false)]
    public ICollection<string> CategoryTags { get; set; }
  }
}
