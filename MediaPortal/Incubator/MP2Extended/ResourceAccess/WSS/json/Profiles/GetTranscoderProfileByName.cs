using System.Linq;
using HttpServer;
using HttpServer.Exceptions;
using HttpServer.Sessions;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.json.Profiles.BaseClasses;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.Profiles;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.json.Profiles
{
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, Summary = "")]
  [ApiFunctionParam(Name = "name", Type = typeof(string), Nullable = false)]
  internal class GetTranscoderProfileByName : BaseTranscoderProfile, IRequestMicroModuleHandler
  {
    public dynamic Process(IHttpRequest request, IHttpSession session)
    {
      HttpParam httpParam = request.Param;
      string name = httpParam["name"].Value;
      if (name == null)
        throw new BadRequestException("GetTranscoderProfileByName: name is null");


      return ProfileManager.Profiles.Where(x => x.Value.Name == name).Select(profile => TranscoderProfile(profile)).ToList();
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
