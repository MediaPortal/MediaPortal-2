﻿#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
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

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.Exceptions;
using MediaPortal.Plugins.MP2Extended.MAS.OnlineVideos;
using MediaPortal.Plugins.MP2Extended.OnlineVideos;
using Microsoft.Owin;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.OnlineVideos
{
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, ReturnType = typeof(List<WebOnlineVideosSiteCategory>), Summary = "This function returns a list of Subcategories available in a selected Category.")]
  [ApiFunctionParam(Name = "id", Type = typeof(string), Nullable = false)]
  internal class GetOnlineVideosSubCategories
  {
    public static Task<IList<WebOnlineVideosSiteCategory>> ProcessAsync(IOwinContext context, string id)
    {
      if (id == null)
        throw new BadRequestException("GetOnlineVideosSubCategories: id is null");

      string siteName;
      string categoryRecursiveName;
      OnlineVideosIdGenerator.DecodeCategoryId(id, out siteName, out categoryRecursiveName);

      return Task.FromResult<IList<WebOnlineVideosSiteCategory>>(
        MP2Extended.OnlineVideosManager.GetSubCategories(siteName, categoryRecursiveName).Select(subCategory => new WebOnlineVideosSiteCategory
        {
          Id = OnlineVideosIdGenerator.BuildCategoryId(siteName, subCategory.RecursiveName()),
          Title = subCategory.Name,
          Description = subCategory.Description,
          HasSubCategories = subCategory.HasSubCategories
        }).ToList());
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
