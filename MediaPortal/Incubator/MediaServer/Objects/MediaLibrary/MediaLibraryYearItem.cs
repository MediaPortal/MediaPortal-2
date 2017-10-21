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

using System;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Plugins.MediaServer.Profiles;

namespace MediaPortal.Plugins.MediaServer.Objects.MediaLibrary
{
  class MediaLibraryYearItem : MediaLibraryContainer
  {
    public MediaLibraryYearItem(string id, string title, Guid[] necessaryMiaTypeIds, Guid[] optionalMiaTypeIds, EndPointSettings client)
      : base(id, title, necessaryMiaTypeIds, optionalMiaTypeIds,
          new BooleanCombinationFilter(BooleanOperator.And, new IFilter[]
                {
                    new RelationalFilter(MediaAspect.ATTR_RECORDINGTIME, RelationalOperator.GE, new DateTime(int.Parse(title), 1, 1)),
                    new RelationalFilter(MediaAspect.ATTR_RECORDINGTIME, RelationalOperator.LT, new DateTime(int.Parse(title) + 1, 1, 1)),
                }), client)
    {
    }

    public override string Class
    {
      get { return "object.container"; }
    }
  }
}
