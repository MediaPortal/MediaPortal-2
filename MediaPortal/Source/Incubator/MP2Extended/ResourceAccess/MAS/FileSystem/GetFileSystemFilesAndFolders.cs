using System.Collections.Generic;
using System.IO;
using System.Linq;
using HttpServer;
using HttpServer.Sessions;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.MP2Extended.Extensions;
using MediaPortal.Plugins.MP2Extended.MAS.FileSystem;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.FileSystem.BaseClasses;
using MediaPortal.Plugins.MP2Extended.Utils;
using Newtonsoft.Json;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.FileSystem
{
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, ReturnType = typeof(List<WebFilesystemItem>), Summary = "")]
  [ApiFunctionParam(Name = "id", Type = typeof(string), Nullable = false)]
  [ApiFunctionParam(Name = "sort", Type = typeof(WebSortField), Nullable = true)]
  [ApiFunctionParam(Name = "order", Type = typeof(WebSortOrder), Nullable = true)]
  internal class GetFileSystemFilesAndFolders : BaseFilesystemItem
  {
    public IList<WebFilesystemItem> Process(string id, WebSortField? sort, WebSortOrder? order)
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

      return output;
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}