using System.Linq;
using HttpServer;
using HttpServer.Sessions;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.Exceptions;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.json.Profiles.BaseClasses;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.Profiles;
using MediaPortal.Plugins.MP2Extended.WSS.Profiles;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.json.Profiles
{
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, Summary = "")]
  [ApiFunctionParam(Name = "name", Type = typeof(string), Nullable = false)]
  internal class GetTranscoderProfileByName : BaseTranscoderProfile
  {
    public WebTranscoderProfile Process(string name)
    {
      return TranscoderProfile(ProfileManager.Profiles.Single(x => x.Value.Name == name));
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
