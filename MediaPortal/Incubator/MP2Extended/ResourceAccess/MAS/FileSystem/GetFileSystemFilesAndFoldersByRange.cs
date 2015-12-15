using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HttpServer;
using HttpServer.Exceptions;
using HttpServer.Sessions;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.MP2Extended.MAS.FileSystem;
using MediaPortal.Plugins.MP2Extended.Extensions;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.FileSystem.BaseClasses;
using MediaPortal.Plugins.MP2Extended.Utils;
using Newtonsoft.Json;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.FileSystem
{
  internal class GetFileSystemFilesAndFoldersByRange : BaseFilesystemItem, IRequestMicroModuleHandler
  {
    [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, ReturnType = typeof(List<WebFilesystemItem>), Summary = "")]
    [ApiFunctionParam(Name = "id", Type = typeof(string), Nullable = false)]
    [ApiFunctionParam(Name = "start", Type = typeof(int), Nullable = false)]
    [ApiFunctionParam(Name = "end", Type = typeof(int), Nullable = false)]
    [ApiFunctionParam(Name = "sort", Type = typeof(WebSortField), Nullable = true)]
    [ApiFunctionParam(Name = "order", Type = typeof(WebSortOrder), Nullable = true)]
    public dynamic Process(IHttpRequest request, IHttpSession session)
    {
      HttpParam httpParam = request.Param;
      string id = httpParam["id"].Value;
      string start = httpParam["start"].Value;
      string end = httpParam["end"].Value;

      if (start == null || end == null)
        throw new BadRequestException("start or end parameter is missing");

      int startInt;
      if (!Int32.TryParse(start, out startInt))
      {
        throw new BadRequestException(String.Format("GetFileSystemFilesAndFoldersByRange: Couldn't convert start to int: {0}", start));
      }

      int endInt;
      if (!Int32.TryParse(end, out endInt))
      {
        throw new BadRequestException(String.Format("GetFileSystemFilesAndFoldersByRange: Couldn't convert end to int: {0}", end));
      }

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
      string sort = httpParam["sort"].Value;
      string order = httpParam["order"].Value;
      if (sort != null && order != null)
      {
        WebSortField webSortField = (WebSortField)JsonConvert.DeserializeObject(sort, typeof(WebSortField));
        WebSortOrder webSortOrder = (WebSortOrder)JsonConvert.DeserializeObject(order, typeof(WebSortOrder));

        output = output.AsQueryable().SortMediaItemList(webSortField, webSortOrder).ToList();
      }

      // get range
      output = output.TakeRange(startInt, endInt).ToList();

      return output;
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}