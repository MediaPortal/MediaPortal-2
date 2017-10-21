using HttpServer;
using HttpServer.Sessions;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.Common;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.json.Control
{
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, ReturnType = typeof(WebBoolResult), Summary = "")]
  [ApiFunctionParam(Name = "identifier", Type = typeof(string), Nullable = false)]
  internal class StopStream
  {
    public WebBoolResult Process(string identifier)
    {
      bool result = true;

      if (identifier == null)
      {
        Logger.Debug("StopStream: identifier is null");
        result = false;
      }

      if (!StreamControl.ValidateIdentifier(identifier))
      {
        Logger.Debug("StopStream: unknown identifier: {0}", identifier);
        result = false;
      }

      StreamControl.StopStreaming(identifier);

     return new WebBoolResult { Result = result };
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
