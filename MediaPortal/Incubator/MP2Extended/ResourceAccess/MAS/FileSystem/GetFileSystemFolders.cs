#region Copyright (C) 2007-2020 Team MediaPortal

/*
    Copyright (C) 2007-2020 Team MediaPortal
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
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.MP2Extended.Extensions;
using MediaPortal.Plugins.MP2Extended.MAS.FileSystem;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.FileSystem.BaseClasses;
using MediaPortal.Plugins.MP2Extended.Utils;
using Microsoft.Owin;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.FileSystem
{
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, ReturnType = typeof(List<WebFolderBasic>), Summary = "")]
  [ApiFunctionParam(Name = "id", Type = typeof(string), Nullable = false)]
  [ApiFunctionParam(Name = "sort", Type = typeof(WebSortField), Nullable = true)]
  [ApiFunctionParam(Name = "order", Type = typeof(WebSortOrder), Nullable = true)]
  internal class GetFileSystemFolders : BaseFolderBasic
  {
    public static Task<IList<WebFolderBasic>> ProcessAsync(IOwinContext context, string id, WebSortField? sort, WebSortOrder? order)
    {
      string path = Base64.Decode(id);

      // Folder listing
      List<WebFolderBasic> output = new List<WebFolderBasic>();
      if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
      {
        output = new DirectoryInfo(path).GetDirectories().Select(dir => FolderBasic(dir)).ToList();
      }

      // sort
      if (sort != null && order != null)
      {
       output = output.AsQueryable().SortMediaItemList(sort, order).ToList();
      }

      return System.Threading.Tasks.Task.FromResult<IList<WebFolderBasic>>(output);
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
