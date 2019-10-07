#region Copyright (C) 2007-2012 Team MediaPortal

/*
    Copyright (C) 2007-2012 Team MediaPortal
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

using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.Exceptions;
using System.Threading.Tasks;
using Microsoft.Owin;
using MediaPortal.Plugins.MP2Extended.WSS;
using MediaPortal.Plugins.MP2Extended.Common;
using System.Collections.Generic;
using System;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.Profiles;
using System.Linq;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.json.General
{
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, Summary = "")]
  internal class GetStreamSize
  {
    public static Task<WebResolution> ProcessAsync(IOwinContext context, WebMediaType type, int? provider, string itemId, int? offset, string profileName)
    {
      if (itemId == null)
        throw new BadRequestException("GetStreamSize: itemId is null");
      if (profileName == null)
        throw new BadRequestException("GetStreamSize: profileName is null");

      ISet<Guid> necessaryMIATypes = new HashSet<Guid>();
      necessaryMIATypes.Add(MediaAspect.ASPECT_ID);
      necessaryMIATypes.Add(ProviderResourceAspect.ASPECT_ID);
      necessaryMIATypes.Add(ImporterAspect.ASPECT_ID);

      ISet<Guid> optionalMIATypes = new HashSet<Guid>();
      optionalMIATypes.Add(VideoAspect.ASPECT_ID);
      optionalMIATypes.Add(VideoStreamAspect.ASPECT_ID);
      optionalMIATypes.Add(VideoAudioStreamAspect.ASPECT_ID);
      optionalMIATypes.Add(ImageAspect.ASPECT_ID);

      var item = MediaLibraryAccess.GetMediaItemById(context, itemId, necessaryMIATypes, optionalMIATypes);
      if (item == null)
        throw new BadRequestException(String.Format("GetStreamSize: No MediaItem found with id: {0}", itemId));

      EndPointProfile profile = null;
      List<EndPointProfile> namedProfiles = ProfileManager.Profiles.Where(x => x.Value.Name == profileName).Select(namedProfile => namedProfile.Value).ToList();
      if (namedProfiles.Count > 0)
      {
        profile = namedProfiles[0];
      }
      else if (ProfileManager.Profiles.ContainsKey(profileName))
      {
        profile = ProfileManager.Profiles[profileName];
      }
      if (profile == null)
        throw new BadRequestException(string.Format("GetStreamSize: unknown profile: {0}", profileName));

      var target = new ProfileMediaItem(Guid.NewGuid().ToString(), item, profile, false);

      var output = new WebResolution();
      if (target.WebMetadata.IsImage)
      {
        output.Height = target.WebMetadata.Image.Height ?? 0;
        output.Width = target.WebMetadata.Image.Width ?? 0;
      }
      else if (target.WebMetadata.IsVideo)
      {
        output.Height = target.WebMetadata.Image.Height ?? 0;
        output.Width = target.WebMetadata.Image.Width ?? 0;
      }

      return Task.FromResult(output);
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
