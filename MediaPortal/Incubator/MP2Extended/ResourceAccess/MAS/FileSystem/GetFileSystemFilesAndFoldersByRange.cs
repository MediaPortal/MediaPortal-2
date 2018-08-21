#region Copyright (C) 2007-2017 Team MediaPortal

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
  internal class GetFileSystemFilesAndFoldersByRange : BaseFilesystemItem
  {
    [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, ReturnType = typeof(List<WebFilesystemItem>), Summary = "")]
    [ApiFunctionParam(Name = "id", Type = typeof(string), Nullable = false)]
    [ApiFunctionParam(Name = "start", Type = typeof(int), Nullable = false)]
    [ApiFunctionParam(Name = "end", Type = typeof(int), Nullable = false)]
    [ApiFunctionParam(Name = "sort", Type = typeof(WebSortField), Nullable = true)]
    [ApiFunctionParam(Name = "order", Type = typeof(WebSortOrder), Nullable = true)]
    public Task<IList<WebFilesystemItem>> ProcessAsync(IOwinContext context, string id, int start, int end, WebSortField? sort, WebSortOrder? order)
    {
      string path = Base64.Decode(id);

      // File listing
      List<WebFilesystemItem> files = new List<WebFilesystemItem>();
      if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
      {
        files = new DirectoryInfo(path).GetFiles().Select(file => FilesystemItem(file)).ToList();
      }

      // Folder listing
      List<WebFilesystemItem> folders = new List<WebFilesystemItem>();
      if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
      {
        folders = new DirectoryInfo(path).GetDirectories().Select(dir => FilesystemItem(dir)).ToList();
      }

      List<WebFilesystemItem> output = files.Concat(folders).ToList();

      // sort
      if (sort != null && order != null)
      {
        output = output.AsQueryable().SortMediaItemList(sort, order).ToList();
      }

      // get range
      output = output.TakeRange(start, end).ToList();

      return System.Threading.Tasks.Task.FromResult<IList<WebFilesystemItem>>(output);
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
