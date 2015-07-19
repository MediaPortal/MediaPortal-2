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

namespace MediaPortal.PackageManager.Options.Admin
{
  internal class CreateUserOptions : AuthOptions
  {
    [Option('l', "login", Required = true, HelpText = "The login alias of the user to create.")]
    public string Login { get; set; }

    [Option('s', "secret", Required = true, HelpText = "The password of the user to create.")]
    public string Secret { get; set; }

    [Option('n', "name", Required = true, HelpText = "The name of the user to create.")]
    public string Name { get; set; }

    [Option('e', "email", Required = false, HelpText = "The email address of the user to create (optional).")]
    public string Email { get; set; }

    [Option('c', "culture", Required = false, HelpText = "The language culture (e.g. en-US) of the user to create (optional).")]
    public string Culture { get; set; }
  }
}