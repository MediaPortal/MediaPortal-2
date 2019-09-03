#region Copyright (C) 2007-2017 Team MediaPortal

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
using System.Threading.Tasks;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.Exceptions;
using MediaPortal.Plugins.MP2Extended.MAS;
using MP2Extended.Extensions;
using Newtonsoft.Json;
using Microsoft.Owin;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.Picture.BaseClasses;
using System.Linq;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.Picture
{
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, Summary = "")]
  internal class GetPictureCategories : BasePictureBasic
  {
    public static Task<IList<WebCategory>> ProcessAsync(IOwinContext context)
    {
      IList<MediaItem> items = MediaLibraryAccess.GetMediaItemsByAspect(context, BasicNecessaryMIATypeIds, BasicOptionalMIATypeIds);
      if (items.Count == 0)
        throw new BadRequestException("No Images found");

      var output = items.Select(i => (i.GetAspect(MediaAspect.Metadata).GetAttributeValue<DateTime>(MediaAspect.ATTR_RECORDINGTIME)).ToString("yyyy")).
        Distinct().Select(y => new WebCategory { Id = y, Title = y }).ToList();

      return Task.FromResult<IList<WebCategory>>(output);
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
