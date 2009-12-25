#region Copyright (C) 2007-2009 Team MediaPortal

/*
    Copyright (C) 2007-2009 Team MediaPortal
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
  public interface IMediaItemAspectTypeRegistration
  {
    /// <summary>
    /// Returns all media item types which were registered in this registration instance.
    /// </summary>
    /// <value>Mapping of aspect type ids to aspect types.</value>
    IDictionary<Guid, MediaItemAspectMetadata> LocallyKnownMediaItemAspectTypes { get; }

    /// <summary>
    /// Registration method for all media item aspect types which are known by the local system.
    /// Each module, which brings in new media item aspect types, must register them at each system start
    /// (or at least before working with them).
    /// </summary>
    /// <remarks>
    /// This method will store media item aspect types which are not registered yet; others, which were already
    /// registered before, are ignored. It will also register the aspect types at the media portal server.
    /// It is needed to register a media item aspect type 1) before the local importer can send media item
    /// data of that type (extracted by a metadata extractor) to the MediaPortal server and 2) for the deserialization
    /// system.
    /// </remarks>
    /// <param name="miaType">Media item aspect type to register.</param>
    void RegisterLocallyKnownMediaItemAspectType(MediaItemAspectMetadata miaType);
  }
}
