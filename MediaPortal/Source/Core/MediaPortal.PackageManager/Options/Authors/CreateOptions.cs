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
  internal class CreateOptions : BaseOptions
  {
    [Option('s', "source", Required = true, HelpText = "Source folder containing the files to package.")]
    public string SourceFolder { get; set; }

    [Option('t', "target", Required = false, HelpText = "Target folder in which to save the created package (default is current directory).", DefaultValue = null)]
    public string TargetFolder { get; set; }

    //[Option('q', "quick", Required = false, HelpText = "Skip validation of the source folder structure and content.", DefaultValue = false)]
    //public bool SkipValidation { get; set; }

    [Option('f', "force", Required = false, HelpText = "Overwrite the target file if it already exists.", DefaultValue = false)]
    public bool OverwriteExistingTarget { get; set; }

    #region Overrides of SharedOptions

    public override bool RequiresElevation
    {
      get { return false; }
    }

    #endregion
  }
}