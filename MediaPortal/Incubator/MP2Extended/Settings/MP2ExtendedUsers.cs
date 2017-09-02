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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaPortal.Common.Settings;
using MediaPortal.Plugins.MP2Extended.Authentication;

namespace MediaPortal.Plugins.MP2Extended.Settings
{
  public class MP2ExtendedUsers
  {
    // This is our super sectret key to store the passwords inside the xml file. Not very secure but at least no plain text.
    public const string KEY = "HigP0Bf4KJQOzgxTKhPUD98TXY6QUfXo8t5nUQ89FA9X1sh9Jk3nC6AVt53t";

    public MP2ExtendedUsers()
    {
      // Creates the MP2Ext default user
      Users = new List<MP2ExtendedUser>
      {
        new MP2ExtendedUser
        {
          Name = "admin",
          Type = UserTypes.Admin,
          Password = "admin"
        }
      };
    }

    [Setting(SettingScope.Global)]
    public List<MP2ExtendedUser> Users { get; set; }
  }
}
