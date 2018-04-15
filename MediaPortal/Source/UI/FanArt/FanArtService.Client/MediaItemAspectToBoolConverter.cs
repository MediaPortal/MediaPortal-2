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
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.UI.SkinEngine.MarkupExtensions;
using MediaPortal.UI.SkinEngine.Xaml;

namespace MediaPortal.Extensions.UserServices.FanArtService.Client
{
  /// <summary>
  /// <see cref="InvertedMediaItemAspectToBoolConverter"/> checks the <see cref="MediaItem.Aspects"/> for the absence of a given <see cref="MediaItemAspect"/>.
  /// </summary>
  public class InvertedMediaItemAspectToBoolConverter : MediaItemAspectToBoolConverter
  {
    public override bool Convert(IDataDescriptor[] values, Type targetType, object parameter, out object result)
    {
      result = false;
      // Special case: here we don't know if the MediaItem was null or the aspect was missing, so we need to check here again
      if (values.Length != 2 || !(values[0].Value is MediaItem))
        return false;

      object originalResult;
      if (base.Convert(values, targetType, parameter, out originalResult))
      {
        result = !(bool)originalResult;
        return true;
      }
      return false;
    }
  }

  /// <summary>
  /// <see cref="MediaItemAspectToBoolConverter"/> checks the <see cref="MediaItem.Aspects"/> for the existance of a given <see cref="MediaItemAspect"/>.
  /// </summary>
  public class MediaItemAspectToBoolConverter : IMultiValueConverter
  {
    public virtual bool Convert(IDataDescriptor[] values, Type targetType, object parameter, out object result)
    {
      result = false;
      if (values.Length != 2)
      {
        ServiceRegistration.Get<ILogger>().Error("MediaItemAspectToBoolConverter: invalid number of arguments (expects MediaItem and Guid)");
        return false;
      }
      MediaItem mediaItem = values[0].Value as MediaItem;
      if (mediaItem == null || values[1].Value == null)
        return true;

      string aspectIdString = values[1].Value as string;
      Guid aspectId;
      if (values[1].Value is Guid)
        aspectId = (Guid)values[1].Value;
      else if (!Guid.TryParse(aspectIdString, out aspectId))
        return true;

      result = mediaItem.Aspects.ContainsKey(aspectId);
      return true;
    }
  }
}
