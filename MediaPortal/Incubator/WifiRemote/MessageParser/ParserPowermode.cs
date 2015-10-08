using Deusty.Net;
using MediaPortal.Common;
using MediaPortal.Common.Runtime;
using MediaPortal.UI.Presentation.Screens;
using Newtonsoft.Json.Linq;

namespace MediaPortal.Plugins.WifiRemote.MessageParser
{
  internal class ParserPowermode
  {
    public static bool Parse(JObject message, SocketServer server, AsyncSocket sender)
    {
      switch (((string)message["PowerMode"]).ToLower())
      {
        case "logoff":
          ServiceRegistration.Get<ISystemStateService>().Logoff(true);
          break;

        case "suspend":
          ServiceRegistration.Get<ISystemStateService>().Suspend();
          break;

        case "hibernate":
          ServiceRegistration.Get<ISystemStateService>().Hibernate();
          break;

        case "reboot":
          ServiceRegistration.Get<ISystemStateService>().Restart(true);
          break;

        case "shutdown":
          ServiceRegistration.Get<ISystemStateService>().Shutdown(true);
          break;

        case "exit":
          ServiceRegistration.Get<IScreenControl>().Shutdown();
          break;
      }
      return true;
    }
  }
}