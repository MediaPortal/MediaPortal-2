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

using CommandLine;
using MediaPortal.Common.PluginManager.Packages.DataContracts.Enumerations;
using MediaPortal.PackageManager.Options.Shared;

namespace MediaPortal.PackageManager.Options.Authors
{
  internal class PublishOptions : AuthOptions
  {
    [Option('s', "source", HelpText = "Path to the package file that should be published.", Required = true)]
    public string PackageFilePath { get; set; }

    [Option('t', "type", HelpText = "The target product for the package (either 'Client', 'Server' or 'Shared').", Required = true)]
    public PackageType PackageType { get; set; }

    [OptionArray('x', "tags", HelpText = "Tags associated with the package (see the wiki for a list of recognized tags).", Required = false)]
    public string[] CategoryTags { get; set; }

    //[OptionList('t', "tags", HelpText="Package tags (separate tags with colons; no spaces allowed)")]
    //public IList<string> Tags { get; set; }
  }
}