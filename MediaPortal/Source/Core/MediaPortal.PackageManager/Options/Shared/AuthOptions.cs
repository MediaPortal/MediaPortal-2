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

namespace MediaPortal.PackageManager.Options.Shared
{
  internal class AuthOptions
  {
    [Option('h', "host", HelpText="URI to the MediaPortal package server.", Required = true)]
    public string Host { get; set; }

    [Option('u', "user", HelpText="User name for authentication with the MediaPortal package server.", Required = true)]
    public string UserName { get; set; }

    [Option('p', "password", HelpText="Password used to authenticate with the MediaPortal package server (if not specified, you will be prompted).", Required = false)]
    public string Password { get; set; }
  }
}