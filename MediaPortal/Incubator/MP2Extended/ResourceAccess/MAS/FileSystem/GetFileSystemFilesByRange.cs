using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HttpServer;
using HttpServer.Sessions;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.MP2Extended.Exceptions;
using MediaPortal.Plugins.MP2Extended.Extensions;
using MediaPortal.Plugins.MP2Extended.MAS.FileSystem;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.FileSystem.BaseClasses;
using MediaPortal.Plugins.MP2Extended.Utils;
using Newtonsoft.Json;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.FileSystem
{
  internal class GetFileSystemFilesByRange : BaseFileBasic
  {
    [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, ReturnType = typeof(List<WebFileBasic>), Summary = "")]
    [ApiFunctionParam(Name = "id", Type = typeof(string), Nullable = false)]
    [ApiFunctionParam(Name = "start", Type = typeof(int), Nullable = false)]
    [ApiFunctionParam(Name = "end", Type = typeof(int), Nullable = false)]
    [ApiFunctionParam(Name = "sort", Type = typeof(WebSortField), Nullable = true)]
    [ApiFunctionParam(Name = "order", Type = typeof(WebSortOrder), Nullable = true)]
    public IList<WebFileBasic> Process(string id, int start, int end, WebSortField? sort, WebSortOrder? order)
    {
      List<WebFileBasic> output = new List<WebFileBasic>();
      string path = Base64.Decode(id);
      if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
      {
        output = new DirectoryInfo(path).GetFiles().Select(file => FileBasic(file)).ToList();
      }

      // sort
      if (sort != null && order != null)
      {
        output = output.AsQueryable().SortMediaItemList(sort, order).ToList();
      }

      // get range
      output = output.TakeRange(start, end).ToList();

      return output;
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}