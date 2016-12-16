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

using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.MLQueries;
using System;
using System.Collections.Generic;

namespace MediaPortal.Backend.Services.MediaLibrary.QueryEngine
{
    public class MultipleMIAQueryBuilder : MainQueryBuilder
    {
        private MultipleMediaItemAspectMetadata _requestedMIA;

        public MultipleMIAQueryBuilder(MIA_Management miaManagement, IEnumerable<QueryAttribute> simpleSelectAttributes,
            MultipleMediaItemAspectMetadata requestedMIA,
            Guid[] mediaItemIds)
          : base(miaManagement, simpleSelectAttributes,
          null,
          new List<MediaItemAspectMetadata> { requestedMIA }, new List<MediaItemAspectMetadata> { },
          new MediaItemIdFilter(mediaItemIds), null)
        {
          _requestedMIA = requestedMIA;
        }

        protected override bool Include(MediaItemAspectMetadata miam)
        {
            // Make it's only the requested MIA in use
            return miam == _requestedMIA;
        }
    }
}
