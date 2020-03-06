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
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Extensions.MediaServer.Profiles;
using MediaPortal.Extensions.MediaServer.Objects.Basic;
using MediaPortal.Common.General;
using MediaPortal.Backend.MediaLibrary;
using MediaPortal.Common;

namespace MediaPortal.Extensions.MediaServer.Objects.MediaLibrary
{
  internal class MediaLibraryYearContainer : BasicContainer
  {
    private readonly Guid[] _necessaryMiaTypeIds;
    private readonly Guid[] _optionalMiaTypeIds;

    public MediaLibraryYearContainer(string id, Guid[] necessaryMiaTypeIds, Guid[] optionalMiaTypeIds, EndPointSettings client)
      : base(id, client)
    {
      _necessaryMiaTypeIds = necessaryMiaTypeIds;
      _optionalMiaTypeIds = optionalMiaTypeIds;
    }

    public HomogenousMap GetItems(string sortCriteria)
    {
      List<Guid> necessaryMias = new List<Guid>(_necessaryMiaTypeIds);
      if (necessaryMias.Contains(MediaAspect.ASPECT_ID)) necessaryMias.Remove(MediaAspect.ASPECT_ID); //Group MIA cannot be present
      IMediaLibrary library = ServiceRegistration.Get<IMediaLibrary>();
      return library.GetValueGroups(MediaAspect.ATTR_RECORDINGTIME, null, ProjectionFunction.DateToYear, necessaryMias, null, true, false);
    }

    public override void Initialise(string sortCriteria, uint? offset = null, uint? count = null)
    {
      base.Initialise(sortCriteria, offset, count);

      HomogenousMap items = GetItems(sortCriteria);
      foreach (KeyValuePair<object, object> item in items.OrderBy(y => y.Key.ToString()))
      {
        try
        {
          if (item.Key == null) continue;
          string title = item.Key.ToString();
          string key = Id + ":" + title;

          Add(new MediaLibraryYearItem(key, title, _necessaryMiaTypeIds, _optionalMiaTypeIds, Client));
        }
        catch (Exception ex)
        {
          Logger.Error("Item '{0}' could not be added", ex, item.Key);
        }
      }
    }
  }
}
