//#region Copyright (C) 2007-2014 Team MediaPortal
///*
//    Copyright (C) 2007-2014 Team MediaPortal
//    http://www.team-mediaportal.com

//    This file is part of MediaPortal 2

//    MediaPortal 2 is free software: you can redistribute it and/or modify
//    it under the terms of the GNU General Public License as published by
//    the Free Software Foundation, either version 3 of the License, or
//    (at your option) any later version.

//    MediaPortal 2 is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//    GNU General Public License for more details.

//    You should have received a copy of the GNU General Public License
//    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
//*/
//#endregion

//using System.Data.Entity;
//using MediaPortal.PackageServer.Domain.Entities;
//using MediaPortal.PackageServer.Domain.Infrastructure.Mappings.Core;

//namespace MediaPortal.PackageServer.Domain.Infrastructure.Mappings
//{
//  internal class CategoryMapping : EntityMappingProvider<Category>
//  {
//    public override void DefineModel( DbModelBuilder modelBuilder )
//    {
//      base.DefineModel( modelBuilder );

//      Map.Property( e => e.Title ).IsRequired().HasMaxLength( 200 );
//      Map.Property( e => e.Description ).IsOptional().HasMaxLength( 1000 );
//      Map.Property( e => e.DisplayOrder ).IsRequired();

//      Map.HasOptional( e => e.ParentCategory ).WithMany( r => r.ChildCategories ).HasForeignKey( e => e.ParentID ).WillCascadeOnDelete( false );
//      Map.HasMany( e => e.Tags ).WithMany().Map( r => r.ToTable( "CategoryTags" ).MapLeftKey( "CategoryID" ).MapRightKey( "TagID" ) );
//      Map.HasMany( e => e.Packages ).WithMany( r => r.Categories ).Map( r => r.ToTable( "PackageCategories" ).MapLeftKey( "CategoryID" ).MapRightKey( "PackageID" ) );
//    }
//  }
//}

