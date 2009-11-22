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
using MediaPortal.Core;
using MediaPortal.Core.MediaManagement;
using MediaPortal.UI.ServerCommunication;

namespace MediaPortal.UI.Services.MediaManagement
{
  /// <summary>
  /// Media item aspect type registration class for the MediaPortal client. Stores all registered media item aspect types
  /// and automatically registers them at the connected server.
  /// </summary>
  public class MediaItemAspectTypeRegistration : IMediaItemAspectTypeRegistration
  {
    protected IDictionary<Guid, MediaItemAspectMetadata> _locallyKnownMediaItemAspectTypes =
        new Dictionary<Guid, MediaItemAspectMetadata>();

    public IDictionary<Guid, MediaItemAspectMetadata> LocallyKnownMediaItemAspectTypes
    {
      get { return _locallyKnownMediaItemAspectTypes; }
    }

    public void RegisterLocallyKnownMediaItemAspectType(MediaItemAspectMetadata miam)
    {
      if (_locallyKnownMediaItemAspectTypes.ContainsKey(miam.AspectId))
        return;
      IServerConnectionManager serverConnectionManager = ServiceScope.Get<IServerConnectionManager>();
      UPnPContentDirectoryService cds = serverConnectionManager == null ? null :
          serverConnectionManager.ContentDirectoryService;
      if (cds != null)
        cds.AddMediaItemAspectStorage(miam);
    }
  }
}
