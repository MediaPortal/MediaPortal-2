#region Copyright (C) 2007-2012 Team MediaPortal

/*
    Copyright (C) 2007-2012 Team MediaPortal
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

namespace TransifexHelper
{
  public class CommandLineOptions
  {
    [Option("t", "TargetDir", Required = true,
        HelpText = "Specifies the directory where to search for Language\\string_en.xml.")]
    public string TargetDir = null;

    [Option(null, "verify", Required = false,
        HelpText = "Verify the tx project against the folder structure.")]
    public bool Verify = false;

    [Option(null, "push", Required = false,
        HelpText = "Push the english template files to Transifex.")]
    public bool Push = false;

    [Option(null, "MP2toAndroid", Required = false,
        HelpText = "Transform only, from MP2 to Android.")]
    public bool MP2toAndroid = false;

    [Option(null, "pull", Required = false,
        HelpText = "Pull the translations from Transifex.")]
    public bool Pull = false;

    [Option(null, "AndroidToMP2", Required = false,
        HelpText = "Transform only, from Android to MP2.")]
    public bool AndroidToMP2 = false;

    [Option(null, "AllTranslations", Required = false,
        HelpText = "Transform all translations to cache, not only strings_en.xml.")]
    public bool AllTranslations = false;
  }
}