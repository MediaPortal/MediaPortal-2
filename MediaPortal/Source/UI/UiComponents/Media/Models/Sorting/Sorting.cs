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

using System.Collections.Generic;
using MediaPortal.Common.MediaManagement;
using MediaPortal.UiComponents.Media.Models.ScreenData;
using System;
using System.Linq;

namespace MediaPortal.UiComponents.Media.Models.Sorting
{
  public abstract class Sorting : IComparer<MediaItem>
  {
    protected IEnumerable<Guid> _includeMias;
    protected IEnumerable<Guid> _excludeMias;

    /// <summary>
    /// Returns the mias for which at least one must be included for this sort to work.
    /// </summary>
    public IEnumerable<Guid> IncludedMias
    {
      get { return _includeMias; }
    }
    /// <summary>
    /// Returns the mias for which this sort should be unavailable.
    /// </summary>
    public IEnumerable<Guid> ExcludeMias
    {
      get { return _excludeMias; }
    }
    public abstract string DisplayName { get; }
    public abstract string GroupByDisplayName { get; }
    public abstract int Compare(MediaItem x, MediaItem y);
    public abstract object GetGroupByValue(MediaItem item);

    public virtual bool IsAvailable(AbstractScreenData visibleScreen)
    {
      return visibleScreen == null || visibleScreen.AvailableMias == null ||
        (_excludeMias != null && _excludeMias.Intersect(visibleScreen.AvailableMias).Count() > 0 ? false :
        (_includeMias != null && _includeMias.Intersect(visibleScreen.AvailableMias).Count() > 0));
    }

    public static MediaItemAspectMetadata.AttributeSpecification GetAttributeSpecification(MediaItem mediaItem, IEnumerable<MediaItemAspectMetadata.SingleAttributeSpecification> attributes, out MediaItemAspect aspect)
    {
      SingleMediaItemAspect singleAspect = null;
      MediaItemAspectMetadata.SingleAttributeSpecification attr = attributes.FirstOrDefault(a => MediaItemAspect.TryGetAspect(mediaItem.Aspects, a.ParentMIAM, out singleAspect));
      aspect = singleAspect;
      return attr;
    }

    public static MediaItemAspectMetadata.AttributeSpecification GetAttributeSpecification(MediaItem mediaItem, IEnumerable<MediaItemAspectMetadata.MultipleAttributeSpecification> attributes, out MediaItemAspect aspect)
    {
      IList<MultipleMediaItemAspect> multipleAspects = null;
      MediaItemAspectMetadata.MultipleAttributeSpecification attr = attributes.FirstOrDefault(a => MediaItemAspect.TryGetAspects(mediaItem.Aspects, a.ParentMIAM, out multipleAspects));
      aspect = attr != null ? multipleAspects[0] : null;
      return attr;
    }
  }
}
