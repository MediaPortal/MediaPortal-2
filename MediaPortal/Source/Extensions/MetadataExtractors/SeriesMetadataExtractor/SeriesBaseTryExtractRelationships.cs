#region Copyright (C) 2007-2015 Team MediaPortal

/*
    Copyright (C) 2007-2015 Team MediaPortal
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
using MediaPortal.Common.MediaManagement;
using MediaPortal.Extensions.OnlineLibraries.Matchers;
using MediaPortal.Common.MediaManagement.Helpers;

namespace MediaPortal.Extensions.MetadataExtractors.SeriesMetadataExtractor
{
  class SeriesBaseTryExtractRelationships
  {
    public bool TryExtractRelationships(IDictionary<Guid, IList<MediaItemAspect>> aspects, out ICollection<IDictionary<Guid, IList<MediaItemAspect>>> extractedLinkedAspects, bool forceQuickMode)
    {
      extractedLinkedAspects = null;

      // Build the series MI

      SeriesInfo seriesInfo = new SeriesInfo();
      if (!seriesInfo.FromMetadata(aspects))
        return false;

      SeriesTheMovieDbMatcher.Instance.UpdateSeries(seriesInfo, forceQuickMode);
      SeriesTvMazeMatcher.Instance.UpdateSeries(seriesInfo, forceQuickMode);
      SeriesTvDbMatcher.Instance.UpdateSeries(seriesInfo, forceQuickMode);
      SeriesOmDbMatcher.Instance.UpdateSeries(seriesInfo, forceQuickMode);
      SeriesFanArtTvMatcher.Instance.UpdateSeries(seriesInfo, forceQuickMode);

      extractedLinkedAspects = new List<IDictionary<Guid, IList<MediaItemAspect>>>();
      IDictionary<Guid, IList<MediaItemAspect>> seriesAspects = new Dictionary<Guid, IList<MediaItemAspect>>();
      extractedLinkedAspects.Add(seriesAspects);

      return seriesInfo.SetMetadata(seriesAspects);
    }
  }
}
