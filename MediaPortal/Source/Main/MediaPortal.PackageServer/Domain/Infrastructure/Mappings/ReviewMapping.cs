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
  internal class ReviewMapping : EntityMappingProvider<Review>
  {
    public override void DefineModel(DbModelBuilder modelBuilder)
    {
      base.DefineModel(modelBuilder);

      Map.Property(e => e.Created).IsRequired();
      Map.Property(e => e.Rating).IsRequired();
      Map.Property(e => e.LanguageCulture).IsRequired().HasMaxLength(5).HasColumnType("varchar");
      Map.Property(e => e.Title).IsRequired().HasMaxLength(100);
      Map.Property(e => e.Body).IsOptional().HasMaxLength(2000);

      Map.HasRequired(e => e.User).WithMany(r => r.Reviews).HasForeignKey(e => e.UserID).WillCascadeOnDelete(false);
      Map.HasRequired(e => e.Package).WithMany(r => r.Reviews).HasForeignKey(e => e.PackageID).WillCascadeOnDelete(false);
    }
  }
}