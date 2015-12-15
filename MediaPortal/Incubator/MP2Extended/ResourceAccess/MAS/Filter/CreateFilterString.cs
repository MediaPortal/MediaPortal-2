using System;
using System.Linq;
using HttpServer;
using HttpServer.Exceptions;
using HttpServer.Sessions;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.MAS.General;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.Filter
{
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, ReturnType = typeof(WebStringResult),
    Summary = "Create a filter string from a given set of parameters. The result of this method can be used as the \"filter\"\r\nparameter in other MPExtended APIs.\r\n\r\nA filter consists of a field name (alphabetic, case-sensitive), followed by an operator (only special characters),\r\nfollowed by the value. Multiple filters are separated with a comma. \r\n\r\nTo define multiple filters, call this method multiple times and join them together. ")]
  [ApiFunctionParam(Name = "field", Type = typeof(string), Nullable = false)]
  [ApiFunctionParam(Name = "op", Type = typeof(string), Nullable = false)]
  [ApiFunctionParam(Name = "value", Type = typeof(string), Nullable = false)]
  [ApiFunctionParam(Name = "conjunction", Type = typeof(string), Nullable = true)]
  internal class CreateFilterString : IRequestMicroModuleHandler
  {
    public dynamic Process(IHttpRequest request, IHttpSession session)
    {
      HttpParam httpParam = request.Param;
      string field = httpParam["field"].Value;
      string op = httpParam["op"].Value;
      string value = httpParam["value"].Value;
      string conjunction = httpParam["conjunction"].Value;

      if (field == null)
        throw new BadRequestException("CreateFilterString: field is null");

      if (op == null)
        throw new BadRequestException("CreateFilterString: op is null");

      if (value == null)
        throw new BadRequestException("CreateFilterString: value is null");
      
      string val = value.Replace("\\", "\\\\").Replace("'", "\\'");
      return conjunction == null ?
          String.Format("{0}{1}'{2}'", field, op, val) :
          String.Format("{0}{1}'{2}'{3} ", field, op, val, conjunction == "and" ? "," : "|");
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}