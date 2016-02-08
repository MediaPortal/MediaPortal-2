using System;
using System.Collections.Generic;
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
  [ApiFunctionParam(Name = "target", Type = typeof(string), Nullable = false)]
  internal class GetTranscoderProfilesForTarget : BaseTranscoderProfile
  {
    public IList<WebTranscoderProfile> Process(string target)
    {
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
