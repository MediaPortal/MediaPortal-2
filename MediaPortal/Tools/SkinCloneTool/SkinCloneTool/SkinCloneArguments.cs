#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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

namespace SkinCloneTool
{
  public class SkinCloneArguments
  {
    [Option('s', "Source Skin", Required = true, HelpText = "Specifies the original skin name. It needs to match the plugin folder name.")]
    public string SourceSkin { get; set; }

    [Option('t', "Target Skin", Required = true, HelpText = "Specifies the target skin name. This name will be used as folder, namespace and display name.")]
    public string TargetSkin { get; set; }

    [Option('f', "Plugin folder", Required = false, HelpText = @"Specifies the path to skin folder.", DefaultValue = @"..\..\..\..\..\Source\UI\Skins")]
    public string PluginFolder { get; set; }

    [Option('o', "Overwrite existing folder", Required = false, HelpText = @"Deletes target folder if it exists.", DefaultValue = false)]
    public bool Overwrite { get; set; }
  }
}
