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
using System.Data.Entity.ModelConfiguration;
using MediaPortal.PackageServer.Domain.Infrastructure.Interfaces;

namespace MediaPortal.PackageServer.Domain.Infrastructure.Mappings.Core
{
  internal abstract class AbstractMappingProvider<T> : IMappingProvider where T : class
  {
    public EntityTypeConfiguration<T> Map { get; private set; }

    public virtual void DefineModel(DbModelBuilder modelBuilder)
    {
      DefineModel(modelBuilder, null, null);
    }

    protected virtual void DefineModel(DbModelBuilder modelBuilder, string table, string discriminatorColumn, string discriminatorValue = null)
    {
      Map = modelBuilder.Entity<T>();

      Map.Map(e =>
      {
        e.ToTable(table ?? typeof(T).Name);
        if (!string.IsNullOrEmpty(discriminatorColumn))
        {
          e.Requires(discriminatorColumn).HasValue(discriminatorValue ?? typeof(T).Name);
        }
      });
    }
  }
}