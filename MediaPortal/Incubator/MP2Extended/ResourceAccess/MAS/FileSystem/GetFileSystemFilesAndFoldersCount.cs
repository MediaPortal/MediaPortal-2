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
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, ReturnType = typeof(WebIntResult), Summary = "")]
  [ApiFunctionParam(Name = "id", Type = typeof(string), Nullable = false)]
  internal class GetFileSystemFilesAndFoldersCount : BaseFilesystemItem, IRequestMicroModuleHandler
  {
    public dynamic Process(IHttpRequest request, IHttpSession session)
    {
      HttpParam httpParam = request.Param;
      string id = httpParam["id"].Value;

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

      return new WebIntResult { Result = output.Count };
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}