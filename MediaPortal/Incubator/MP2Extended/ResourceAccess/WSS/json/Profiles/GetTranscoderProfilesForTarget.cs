using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HttpServer;
using HttpServer.Exceptions;
using HttpServer.Sessions;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.json.Profiles.BaseClasses;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.Profiles;
using MediaPortal.Plugins.MP2Extended.WSS.Profiles;
using MediaPortal.Plugins.MP2Extended.WSS.StreamInfo;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.json.Profiles
{
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, Summary = "")]
  [ApiFunctionParam(Name = "target", Type = typeof(string), Nullable = false)]
  internal class GetTranscoderProfilesForTarget : BaseTranscoderProfile, IRequestMicroModuleHandler
  {
    public dynamic Process(IHttpRequest request, IHttpSession session)
    {
      HttpParam httpParam = request.Param;
      string target = httpParam["target"].Value;
      if (target == null)
        throw new BadRequestException("GetTranscoderProfilesForTarget: target is null");

      TargetComparer targetComparer = new TargetComparer();
      return ProfileManager.Profiles.Where(x => x.Value.Targets.Contains(target, targetComparer) || x.Value.Targets.Count == 0).Select(profile => TranscoderProfile(profile)).ToList();
    }

    class TargetComparer : IEqualityComparer<string>
    {
      public bool Equals(string x, string y)
      {
        if (string.IsNullOrEmpty(x)) return true;
        if (string.IsNullOrEmpty(y)) return true;
        return x.Equals(y, StringComparison.InvariantCultureIgnoreCase);
      }

      public int GetHashCode(string x)
      {
        return x.GetHashCode();
      }
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
