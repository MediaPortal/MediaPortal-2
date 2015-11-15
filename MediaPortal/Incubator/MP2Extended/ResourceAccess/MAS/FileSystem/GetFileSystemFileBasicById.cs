using System.IO;
using HttpServer;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.FileSystem.BaseClasses;
using MediaPortal.Plugins.MP2Extended.Utils;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.FileSystem
{
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, ReturnType = typeof(WebFileBasic), Summary = "")]
  [ApiFunctionParam(Name = "id", Type = typeof(string), Nullable = false)]
  internal class GetFileSystemFileBasicById : BaseFileBasic, IRequestMicroModuleHandler
  {
    public dynamic Process(IHttpRequest request)
    {
      HttpParam httpParam = request.Param;
      string id = httpParam["id"].Value;

      string path = Base64.Decode(id);
      if (!File.Exists(path))
        return null;

      return FileBasic(new FileInfo(path.StartsWith("file://") ? path.Substring(7) : path));
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}