using System.Collections.Generic;
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
using Newtonsoft.Json;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.FileSystem
{
  internal class GetFileSystemDrives : BaseDriveBasic, IRequestMicroModuleHandler
  {
    [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, ReturnType = typeof(List<WebDriveBasic>), Summary = "")]
    [ApiFunctionParam(Name = "sort", Type = typeof(WebSortField), Nullable = true)]
    [ApiFunctionParam(Name = "order", Type = typeof(WebSortOrder), Nullable = true)]
    public dynamic Process(IHttpRequest request, IHttpSession session)
    {
      List<WebDriveBasic> output = DriveBasic();

      // sort and filter
      HttpParam httpParam = request.Param;
      string sort = httpParam["sort"].Value;
      string order = httpParam["order"].Value;
      if (sort != null && order != null)
      {
        WebSortField webSortField = (WebSortField)JsonConvert.DeserializeObject(sort, typeof(WebSortField));
        WebSortOrder webSortOrder = (WebSortOrder)JsonConvert.DeserializeObject(order, typeof(WebSortOrder));

        output = output.AsQueryable().SortMediaItemList(webSortField, webSortOrder).ToList();
      }

      return output;
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}