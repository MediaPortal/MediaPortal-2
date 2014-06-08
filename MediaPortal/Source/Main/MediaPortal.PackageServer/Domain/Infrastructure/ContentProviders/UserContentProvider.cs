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
using MediaPortal.PackageServer.Domain.Entities;
using MediaPortal.PackageServer.Domain.Entities.Enumerations;
using MediaPortal.PackageServer.Domain.Infrastructure.Context;

namespace MediaPortal.PackageServer.Domain.Infrastructure.ContentProviders
{
  internal class UserContentProvider : AbstractContentProvider
  {
    public UserContentProvider() : base(22)
    {
    }

    public override void CreateContent(DataContext context)
    {
      CreateUser(context, "admin", "kJ1+vZV+XTvYTDOfqNcsOi/VmaAwwGqf13XT5VPZ6hOZJCZQnxeLYv6uuRsMC9PD", "PackageServer Administrator", null);
    }

    protected User CreateUser(DataContext context, string alias, string passwordHash, string name, string email)
    {
      var user = new User
      {
        AuthType = AuthType.Integral,
        Status = AccountStatus.Active,
        Role = Role.Admin,
        SourceIdentity = null,
        Alias = alias,
        PasswordHash = passwordHash,
        Name = name,
        Email = email,
        Culture = null,
        RevokeReason = null,
        Created = DateTime.Now,
        Modified = DateTime.Now,
        Deleted = null,
        LastSeen = DateTime.Now
      };
      context.Users.Add(user);
      return user;
    }
  }
}