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

using System.Data.Entity;
using MediaPortal.PackageServer.Domain.Entities;
using MediaPortal.PackageServer.Domain.Infrastructure.Mappings.Core;

namespace MediaPortal.PackageServer.Domain.Infrastructure.Mappings
{
  internal class UserMapping : EntityMappingProvider<User>
  {
    public override void DefineModel(DbModelBuilder modelBuilder)
    {
      base.DefineModel(modelBuilder);

      Map.Property(e => e.AuthType).IsRequired();
      Map.Property(e => e.Status).IsRequired();
      Map.Property(e => e.Role).IsRequired();

      Map.Property(e => e.Created).IsRequired();
      Map.Property(e => e.Modified).IsRequired();
      Map.Property(e => e.Deleted).IsOptional();
      Map.Property(e => e.LastSeen).IsRequired();

      Map.Property(e => e.SourceIdentity).IsOptional();

      Map.Property(e => e.Alias).IsOptional().HasMaxLength(50);
      Map.Property(e => e.PasswordHash).IsOptional().HasMaxLength(100).HasColumnType("varchar");

      //Map.Property( e => e.AccessToken ).IsOptional().HasMaxLength( 100 );
      //Map.Property( e => e.AccessTokenExpiry ).IsOptional();

      Map.Property(e => e.Name).IsOptional().HasMaxLength(200);
      Map.Property(e => e.Email).IsOptional().HasMaxLength(200);
      Map.Property(e => e.Culture).IsOptional().HasMaxLength(5).HasColumnType("varchar");

      Map.Property(e => e.RevokeReason).IsOptional().HasMaxLength(200);

      Map.HasOptional(e => e.CreatingUser).WithMany().HasForeignKey(e => e.CreatingUserID);

      Map.HasMany(e => e.PackagesOwned).WithRequired(r => r.Owner).HasForeignKey(r => r.OwnerUserID);
      Map.HasMany(e => e.Reviews).WithRequired(r => r.User).HasForeignKey(r => r.UserID);
    }
  }
}