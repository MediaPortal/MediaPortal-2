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
using MediaPortal.Common;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.SystemCommunication;
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

    public void RegisterLocallyKnownMediaItemAspectType(MediaItemAspectMetadata miaType)
    {
      if (_locallyKnownMediaItemAspectTypes.ContainsKey(miaType.AspectId))
        return;
      _locallyKnownMediaItemAspectTypes.Add(miaType.AspectId, miaType);
      IServerConnectionManager serverConnectionManager = ServiceRegistration.Get<IServerConnectionManager>();
      IContentDirectory cd = serverConnectionManager == null ? null :
          serverConnectionManager.ContentDirectory;
      if (cd != null)
        cd.AddMediaItemAspectStorage(miaType);
    }
  }
}
