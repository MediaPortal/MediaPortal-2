using HttpServer;
using HttpServer.Exceptions;
using HttpServer.Sessions;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.SlimTv.Interfaces;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.TAS.Timeshiftings
{
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, Summary = "")]
  [ApiFunctionParam(Name = "userName", Type = typeof(string), Nullable = false)]
  internal class CancelCurrentTimeShifting : IRequestMicroModuleHandler
  {
    public dynamic Process(IHttpRequest request, IHttpSession session)
    {
      HttpParam httpParam = request.Param;
      string userName = httpParam["userName"].Value;
      

      if (!ServiceRegistration.IsRegistered<ITvProvider>())
        throw new BadRequestException("CancelCurrentTimeShifting: ITvProvider not found");

      if (userName == null)
        throw new BadRequestException("CancelCurrentTimeShifting: userName is null");

      ITimeshiftControlEx timeshiftControl = ServiceRegistration.Get<ITvProvider>() as ITimeshiftControlEx;

      bool result = timeshiftControl.StopTimeshift(SlotControl.GetSlotIndex(userName));
      SlotControl.DeleteSlotIndex(userName);


      return new WebBoolResult { Result = result };
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}