using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaPortal.Plugins.ServerStateService.Interfaces.UPnP
{
  public class Consts
  {
    public const string SERVICE_TYPE = "schemas-team-mediaportal-com:service:ServerStateService";
    public const int SERVICE_TYPE_VERSION = 1;
    public const string SERVICE_NAME = "ServerStateService";
    public const string SERVICE_ID = "urn:team-mediaportal-com:serviceId:ServerStateService";
    public const string STATE_PENDING_SERVER_STATES = "PendingServerStates";
    public const string ACTION_GET_STATES = "GetStates";
  }
}
