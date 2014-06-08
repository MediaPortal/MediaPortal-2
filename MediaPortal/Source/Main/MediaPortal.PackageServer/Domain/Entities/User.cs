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
using MediaPortal.PackageServer.Domain.Entities.Enumerations;
using MediaPortal.PackageServer.Domain.Entities.Interfaces;

namespace MediaPortal.PackageServer.Domain.Entities
{
  public class User : IEntity
  {
    public long ID { get; set; }
    public long? CreatingUserID { get; set; } // keep track of who created whom

    // enum to let us identify where accounts originate
    public AuthType AuthType { get; set; }
    // enum to let us identify unused and revoked accounts
    public AccountStatus Status { get; set; }
    // enum to let us authorize requests from authenticated users
    public Role Role { get; set; }

    public DateTime Created { get; set; }
    public DateTime Modified { get; set; }
    public DateTime? Deleted { get; set; }
    public DateTime LastSeen { get; set; }

    // accounts without authentication (semi-anonymous; these cannot own packages)
    public Guid? SourceIdentity { get; set; }

    // accounts owned by server and authenticated against the local database
    public string Alias { get; set; }
    public string PasswordHash { get; set; }

    // these were for authenticating forum accounts, not currently supported
    //public string AccessToken { get; set; }
    //public DateTime? AccessTokenExpiry { get; set; }

    public string Name { get; set; }
    public string Email { get; set; }
    // preferred language (snatched from browser, if available)
    public string Culture { get; set; }

    // the reason given for revoking a user account
    public string RevokeReason { get; set; }

    public virtual User CreatingUser { get; set; } // the User who created this User
    public virtual ICollection<Package> PackagesOwned { get; set; }
    public virtual ICollection<Review> Reviews { get; set; }

    public User()
    {
      PackagesOwned = new List<Package>();
      Reviews = new List<Review>();
    }
  }
}