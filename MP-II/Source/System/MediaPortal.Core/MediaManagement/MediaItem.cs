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

namespace MediaPortal.Core.MediaManagement
{
  /// <summary>
  /// Base class for <see cref="MediaItem"/> and <see cref="MediaContainer"/> classes.
  /// Instances of this class are used for encapsulating single entries in media item views.
  /// </summary>
  public class MediaItem
  {
    #region Protected fields

    protected readonly IDictionary<Guid, MediaItemAspect> _aspects = new Dictionary<Guid, MediaItemAspect>();

    #endregion

    public MediaItem() { }

    public MediaItem(IEnumerable<MediaItemAspect> aspects)
    {
      foreach (MediaItemAspect aspect in aspects)
        _aspects.Add(aspect.Metadata.AspectId, aspect);
    }

    public IDictionary<Guid, MediaItemAspect> Aspects
    {
      get { return _aspects; }
    }
  }
}
