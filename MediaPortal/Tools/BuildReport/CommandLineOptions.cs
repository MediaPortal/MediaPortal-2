#region Copyright (C) 2007-2008 Team MediaPortal

/*
    Copyright (C) 2007-2008 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal II

    MediaPortal II is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal II is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using CommandLine;

namespace MediaPortal.Tools.BuildReport
{
  [Serializable]
  public class CommandLineOptions
  {
    [Option("i", "input", Required = true,
        HelpText = "Input filename")]
    public string Input = null;

    [Option("o", "output", Required = true,
        HelpText = "Output filename")]
    public string Output = null;

    [Option("s", "solution", Required = false,
        HelpText = "Solution name")]
    public string Solution = null;

    [Option("t", "title", Required = true,
        HelpText = "Report title")]
    public string Title = null;

    [Option("vs2005", null, Required = false,
        HelpText = "Use Visual Studio 2005 solution file")]
    public bool VS2005 = false;
  }
}
