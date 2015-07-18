//#region Copyright (C) 2007-2015 Team MediaPortal
///*
//    Copyright (C) 2007-2015 Team MediaPortal
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

//using System;
//using MediaPortal.PackageServer.Domain.Entities;
//using MediaPortal.PackageServer.Domain.Infrastructure.Context;

//namespace MediaPortal.PackageServer.Domain.Infrastructure.ContentProviders
//{
//  internal class CategoryContentProvider : AbstractContentProvider
//  {
//    public CategoryContentProvider() : base( 25 )
//    {
//    }

//    public override void CreateContent( DataContext context )
//    {
//      // TODO define categories
//      AddCategory( context, "Skins", "Blah", 1 );
//      context.SaveChanges();
//    }

//    private Category AddCategory( DataContext context, string title, string description, int displayOrder )
//    {
//      var category = new Category
//      {
//        Title = title,
//        Description = description,
//        DisplayOrder = displayOrder
//      };
//      context.Categories.Add( category );
//      return category;
//    }
//  }
//}

