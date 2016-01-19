using HttpServer;
using HttpServer.Sessions;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.TAS.Timeshiftings;
using MediaPortal.Plugins.SlimTv.Interfaces;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.json.Control
{
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, Summary = "")]
  [ApiFunctionParam(Name = "identifier", Type = typeof(string), Nullable = false)]
  internal class FinishStream : IRequestMicroModuleHandler
  {
    public dynamic Process(IHttpRequest request, IHttpSession session)
    {
      HttpParam httpParam = request.Param;
      
      string identifier = httpParam["identifier"].Value;
      bool result = true;

      if (identifier == null)
      {
        Logger.Debug("FinishStream: identifier is null");
        result = false;
      }

      if (!StreamControl.ValidateIdentifie(identifier))
      {
        Logger.Debug("FinishStream: unknown identifier: {0}", identifier);
        result = false;
      }

      // Remove the stream from the stream controler
      stream.StreamItem item = StreamControl.GetStreamItem(identifier);
      StreamControl.DeleteStreamItem(identifier);

      //Stop timeshifting
      if (item != null && (item.ItemType == WebMediaType.TV || item.ItemType == WebMediaType.Radio))
      {
        ITimeshiftControlEx timeshiftControl = ServiceRegistration.Get<ITvProvider>() as ITimeshiftControlEx;
        result = timeshiftControl.StopTimeshift(SlotControl.GetSlotIndex(identifier));
        SlotControl.DeleteSlotIndex(identifier);
      }

     return new WebBoolResult { Result = result };
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}