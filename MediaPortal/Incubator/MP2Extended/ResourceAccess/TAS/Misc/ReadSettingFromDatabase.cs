using System.Collections.Generic;
using HttpServer;
using HttpServer.Exceptions;
using HttpServer.Sessions;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.MP2Extended.MAS.General;
using MediaPortal.Plugins.MP2Extended.TAS.Misc;
using MediaPortal.Plugins.SlimTv.Interfaces;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.TAS.Misc
{
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, ReturnType = typeof(WebStringResult), Summary = "This function is not really supported in MP2Ext.\r\nOnly 'preRecordInterval' and 'postRecordInterval' are supported by MP2Ext settings. These are !not! read from the TVE DB.")]
  [ApiFunctionParam(Name = "tagName", Type = typeof(string), Nullable = false)]
  internal class ReadSettingFromDatabase : IRequestMicroModuleHandler
  {
    public dynamic Process(IHttpRequest request, IHttpSession session)
    {
      HttpParam httpParam = request.Param;
      string tagName = httpParam["tagName"].Value;
      if (tagName == null)
        throw new BadRequestException("ReadSettingFromDatabase: tagName is null");

      string output = "0";
      switch (tagName)
      {
        case "preRecordInterval":
          output = MP2Extended.Settings.PreRecordInterval.ToString();
          break;
        case "postRecordInterval":
          output = MP2Extended.Settings.PostRecordInterval.ToString();
          break;
      }

      return new WebStringResult { Result = output };
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}