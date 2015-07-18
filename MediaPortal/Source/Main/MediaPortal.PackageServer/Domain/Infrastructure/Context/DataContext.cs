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

using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;
using System.Reflection;
using MediaPortal.Common.General;
using MediaPortal.PackageServer.Domain.Entities;
using MediaPortal.PackageServer.Domain.Infrastructure.Conventions;
using MediaPortal.PackageServer.Domain.Infrastructure.Interfaces;

namespace MediaPortal.PackageServer.Domain.Infrastructure.Context
{
  public class DataContext : DbContext
  {
    public DbSet<Package> Packages { get; set; }
    public DbSet<Release> Releases { get; set; }
    public DbSet<Dependency> Dependencies { get; set; }
    public DbSet<Review> Reviews { get; set; }
    public DbSet<Tag> Tags { get; set; }
    public DbSet<User> Users { get; set; }

    public DataContext() : base("MediaPortal.PackageServer")
    {
    }

    public DataContext(string nameOrConnectionString) : base(nameOrConnectionString)
    {
    }

    protected override void OnModelCreating(DbModelBuilder modelBuilder)
    {
      base.OnModelCreating(modelBuilder);
      //modelBuilder.Conventions.Remove<IncludeMetadataConvention>();
      modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();
      //modelBuilder.Conventions.Add(new ForeignKeyNamingConvention());

      var maps = Assembly.GetExecutingAssembly().CreateInstances<IMappingProvider>();
      maps.ForEach(m => m.DefineModel(modelBuilder));
    }
  }
}