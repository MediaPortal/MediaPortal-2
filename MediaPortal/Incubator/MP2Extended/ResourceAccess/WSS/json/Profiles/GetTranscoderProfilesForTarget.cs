﻿#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
    http://www.team-mediaportal.com

    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.json.Profiles.BaseClasses;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.Profiles;
using MediaPortal.Plugins.MP2Extended.WSS.Profiles;
using Microsoft.Owin;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.json.Profiles
{
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, Summary = "")]
  [ApiFunctionParam(Name = "target", Type = typeof(string), Nullable = false)]
  internal class GetTranscoderProfilesForTarget : BaseTranscoderProfile
  {
    public static Task<IList<WebTranscoderProfile>> ProcessAsync(IOwinContext context, string target)
    {
      TargetComparer targetComparer = new TargetComparer();
      return Task.FromResult<IList<WebTranscoderProfile>>(ProfileManager.Profiles.Where(x => x.Value.Targets.Contains(target, targetComparer) || 
        x.Value.Targets.Count == 0).Select(profile => TranscoderProfile(profile)).ToList());
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
