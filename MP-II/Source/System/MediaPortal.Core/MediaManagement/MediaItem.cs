#region Copyright (C) 2007-2008 Team MediaPortal

/*
    Copyright (C) 2007-2008 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal II

    MediaPortal II is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal II is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Collections.Generic;
using MediaPortal.Core.MediaManagement.DefaultItemAspects;

namespace MediaPortal.Core.MediaManagement
{
  /// <summary>
  /// Instances of this class are used for holding the data for single entries in media item views.
  /// </summary>
  /// <remarks>
  /// Instances of this class contain multiple media item aspect instances; not necessarily all media item
  /// aspects are contained here.
  /// </remarks>
  public class MediaItem : IEquatable<MediaItem>
  {
    #region Protected fields

    protected readonly IDictionary<Guid, MediaItemAspect> _aspects = new Dictionary<Guid, MediaItemAspect>();

    #endregion

    public MediaItem() { }

    public MediaItem(IEnumerable<MediaItemAspect> aspects)
    {
      foreach (MediaItemAspect aspect in aspects)
        _aspects.Add(aspect.Metadata.AspectId, aspect);
      if (!_aspects.ContainsKey(ProviderResourceAspect.ASPECT_ID))
        throw new ArgumentException(string.Format("Media items always have to contain the '{0}' aspect",
            typeof(ProviderResourceAspect).Name));
    }

    public IDictionary<Guid, MediaItemAspect> Aspects
    {
      get { return _aspects; }
    }

    /// <summary>
    /// Returns the media item aspect of the specified <paramref name="mediaItemAspectId"/>, if it is
    /// contained in this media item. If the specified aspect is contained in this instance depends on two
    /// conditions: 1) the aspect has to be present on this media item in the media storage (media library
    /// or local storage), 2) the aspect data have to be added to this instance.
    /// </summary>
    /// <param name="mediaItemAspectId">Id of the media item aspect to retrieve.</param>
    /// <returns>Media item aspect of the specified <paramref name="mediaItemAspectId"/>, or <c>null</c>,
    /// if the aspect is not contained in this instance.</returns>
    public MediaItemAspect this[Guid mediaItemAspectId]
    {
      get { return _aspects.ContainsKey(mediaItemAspectId) ? _aspects[mediaItemAspectId] : null; }
    }

    #region IEquatable<MediaItem> implementation

    public bool Equals(MediaItem other)
    {
      if (other == null)
        return false;
      MediaItemAspect myProviderAspect = _aspects[ProviderResourceAspect.ASPECT_ID];
      MediaItemAspect otherProviderAspect = other._aspects[ProviderResourceAspect.ASPECT_ID];
      return
          myProviderAspect[ProviderResourceAspect.ATTR_PROVIDER_ID] == otherProviderAspect[ProviderResourceAspect.ATTR_PROVIDER_ID] &&
          myProviderAspect[ProviderResourceAspect.ATTR_PATH] == otherProviderAspect[ProviderResourceAspect.ATTR_PATH];
    }

    #endregion

    #region Base overrides

    public override int GetHashCode()
    {
      MediaItemAspect providerAspect = _aspects[ProviderResourceAspect.ASPECT_ID];
      return providerAspect[ProviderResourceAspect.ATTR_PROVIDER_ID].GetHashCode() +
          providerAspect[ProviderResourceAspect.ATTR_PATH].GetHashCode();
    }

    public override bool Equals(object obj)
    {
      return Equals(obj as MediaItem);
    }

    #endregion
  }
}
