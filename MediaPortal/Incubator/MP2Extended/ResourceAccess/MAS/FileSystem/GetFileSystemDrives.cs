using System;
using System.Collections.Generic;
using System.Linq;
using HttpServer;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.MP2Extended.Extensions;
using MediaPortal.Plugins.MP2Extended.MAS.FileSystem;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.FileSystem.BaseClasses;
using Newtonsoft.Json;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.FileSystem
{
  internal class GetFileSystemDrives : BaseDriveBasic, IRequestMicroModuleHandler
  {
    protected Nullable<T> GetHttpParam<T>(HttpParam httpParam, string name) where T : struct
    {
      string value = httpParam[name].Value;
      if (value != null)
      {
        return (T)JsonConvert.DeserializeObject(value, typeof(T));
      }
      return null;
    }

    public dynamic Process(IHttpRequest request)
    {
      // sort and filter
      HttpParam httpParam = request.Param;
      WebSortField? sort = GetHttpParam<WebSortField>(httpParam, "sort");
      WebSortOrder? order = GetHttpParam<WebSortOrder>(httpParam, "order");

      return Process(sort, order);
    }

    //[ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, Summary = "")]
    //[ApiFunctionParam(Name = "sort", Type = typeof(WebSortField), Nullable = true)]
    //[ApiFunctionParam(Name = "order", Type = typeof(WebSortOrder), Nullable = true)]
    public List<WebDriveBasic> Process(WebSortField? sort, WebSortOrder? order)
    {
      List<WebDriveBasic> output = DriveBasic();
      if (sort != null && order != null)
      {
        output = output.AsQueryable().SortMediaItemList(sort.Value, order.Value).ToList();
      }
      return output;
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}