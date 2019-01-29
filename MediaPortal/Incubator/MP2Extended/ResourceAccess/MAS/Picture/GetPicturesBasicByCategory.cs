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
using System.Linq;
using System.Threading.Tasks;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.Exceptions;
using MediaPortal.Plugins.MP2Extended.MAS.Picture;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.Picture.BaseClasses;
using Microsoft.Owin;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.Picture
{
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, Summary = "")]
  [ApiFunctionParam(Name = "id", Type = typeof(string), Nullable = false)]
  internal class GetPicturesBasicByCategory : BasePictureBasic
  {
    public static Task<IList<WebPictureBasic>> ProcessAsync(IOwinContext context, string id)
    {
      if (string.IsNullOrEmpty(id) || id.Length != 4)
        throw new BadRequestException("GetPicturesBasicByCategory: Couldn't convert id to year");

      DateTime start = new DateTime(Convert.ToInt32(id), 1, 1);
      DateTime end = new DateTime(Convert.ToInt32(id), 12, 31);
      IList<MediaItem> items = MediaLibraryAccess.GetMediaItemsByRecordingTime(context, start, end, BasicNecessaryMIATypeIds, BasicOptionalMIATypeIds);

      var output = items.Select(item => PictureBasic(item)).ToList();

      return Task.FromResult<IList<WebPictureBasic>>(output);
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
