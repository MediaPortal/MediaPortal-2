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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaPortal.Common.PluginManager.Packages.DataContracts.UserAdmin
{
  public class CreateUserModel
  {
    public string Alias { get; set; }
    public string Password { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public string Culture { get; set; }

    public CreateUserModel()
    {
    }

    public CreateUserModel(string @alias, string password, string name)
    {
      Alias = alias;
      Password = password;
      Name = name;
    }

    public CreateUserModel(string @alias, string password, string name, string email, string culture)
    {
      Alias = alias;
      Password = password;
      Name = name;
      Email = email;
      Culture = culture;
    }
  }
}