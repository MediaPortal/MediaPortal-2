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
  internal class PackageMapping : EntityMappingProvider<Package>
  {
    public override void DefineModel(DbModelBuilder modelBuilder)
    {
      base.DefineModel(modelBuilder);

      Map.Property(e => e.Guid).IsRequired();
      Map.Property(e => e.PackageType).IsRequired();

      Map.Property(e => e.Name).IsRequired().HasMaxLength(100);
      Map.Property(e => e.Authors).IsRequired().HasMaxLength(200);
      Map.Property(e => e.License).IsRequired().HasMaxLength(100);
      Map.Property(e => e.Description).IsRequired().HasMaxLength(2000);

      Map.Property(e => e.Created).IsRequired();
      Map.Property(e => e.Modified).IsRequired();


      Map.HasRequired(e => e.Owner).WithMany(r => r.PackagesOwned).HasForeignKey(e => e.OwnerUserID);
      // NOTE: technically a release does not have many packages (as the configuration implies), but EF only creates the correct schema this way
      Map.HasOptional(e => e.CurrentRelease).WithMany().HasForeignKey(e => e.CurrentReleaseID).WillCascadeOnDelete(false);

      Map.HasMany(e => e.Reviews).WithRequired(r => r.Package).HasForeignKey(r => r.PackageID).WillCascadeOnDelete(false);
      Map.HasMany(e => e.Tags).WithMany(); // table/keys defined in TagMapping
      Map.HasMany(e => e.Releases).WithRequired(r => r.Package).HasForeignKey(r => r.PackageID).WillCascadeOnDelete(false);
    }
  }
}