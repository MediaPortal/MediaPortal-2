using System.Linq;
using HttpServer;
using HttpServer.Sessions;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.json.Profiles.BaseClasses;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.Profiles;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.json.Profiles
{
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, Summary = "")]
  internal class GetTranscoderProfiles : BaseTranscoderProfile, IRequestMicroModuleHandler
  {
    public dynamic Process(IHttpRequest request, IHttpSession session)
    {
      return ProfileManager.Profiles.Select(profile => TranscoderProfile(profile)).ToList();
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
