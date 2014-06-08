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

namespace MediaPortal.PackageServer.Models
{
  // Models returned by AccountController actions.

  public class ExternalLoginViewModel
  {
    public string Name { get; set; }

    public string Url { get; set; }

    public string State { get; set; }
  }

  public class ManageInfoViewModel
  {
    public string LocalLoginProvider { get; set; }

    public string UserName { get; set; }

    public IEnumerable<UserLoginInfoViewModel> Logins { get; set; }

    public IEnumerable<ExternalLoginViewModel> ExternalLoginProviders { get; set; }
  }

  public class UserInfoViewModel
  {
    public string UserName { get; set; }

    public bool HasRegistered { get; set; }

    public string LoginProvider { get; set; }
  }

  public class UserLoginInfoViewModel
  {
    public string LoginProvider { get; set; }

    public string ProviderKey { get; set; }
  }
}