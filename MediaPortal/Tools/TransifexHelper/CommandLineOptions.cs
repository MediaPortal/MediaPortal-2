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

using CommandLine;
using CommandLine.Text;

namespace TransifexHelper
{
  public class CommandLineOptions
  {
    [Option('t', "TargetDir", Required = true,
      HelpText = "Specifies the directory, where to search for Language\\string_en.xml.")]
    public string TargetDir { get; set; }

    [Option("verify", Required = false,
      HelpText = "Verify the tx project against the folder structure.")]
    public bool Verify { get; set; }

    [Option("ToCache", Required = false,
      HelpText = "Copy English template files from plugin sources to Transifex cache.")]
    public bool ToCache { get; set; }

    [Option("push", Required = false,
      HelpText = "Push English template files from Transifex cache to Transifex.net.")]
    public bool Push { get; set; }

    [Option("pull", Required = false,
      HelpText = "Pull non-English translation files from Transifex.net to Transifex cache.")]
    public bool Pull { get; set; }

    [Option("fix", Required = false,
      HelpText = "(Temporary) Fix xml encodings for &lt; &gt; tags.")]
    public bool Fix { get; set; }

    [Option("FromCache", Required = false,
      HelpText = "Copy non-English translation files from Transifex cache to language pack plugin.")]
    public bool FromCache { get; set; }

    [HelpOption]
    public string GetUsage()
    {
      return HelpText.AutoBuild(this, current => HelpText.DefaultParsingErrorsHandler(this, current));
    }
  }
}