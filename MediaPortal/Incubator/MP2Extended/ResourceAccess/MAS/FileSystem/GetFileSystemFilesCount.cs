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
  internal class GetFileSystemFilesCount : BaseFileBasic, IRequestMicroModuleHandler
  {
    [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, ReturnType = typeof(WebIntResult), Summary = "")]
    [ApiFunctionParam(Name = "id", Type = typeof(string), Nullable = false)]
    public dynamic Process(IHttpRequest request, IHttpSession session)
    {
      HttpParam httpParam = request.Param;
      string id = httpParam["id"].Value;

      List<WebFileBasic> output = new List<WebFileBasic>();
      string path = Base64.Decode(id);
      if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
      {
        output = new DirectoryInfo(path).GetFiles().Select(file => FileBasic(file)).ToList();
      }

      return new WebIntResult { Result = output.Count };
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}