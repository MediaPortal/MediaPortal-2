#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
    protected IDictionary<Guid, MediaItemAspectMetadata> _locallyKnownMediaItemAspectTypes = new ConcurrentDictionary<Guid, MediaItemAspectMetadata>();
    protected ConcurrentDictionary<Guid, MediaItemAspectMetadata> _locallySupportedReimportMediaItemAspectTypes = new ConcurrentDictionary<Guid, MediaItemAspectMetadata>();

    public IDictionary<Guid, MediaItemAspectMetadata> LocallyKnownMediaItemAspectTypes
    {
      get { return _locallyKnownMediaItemAspectTypes; }
    }

    public IDictionary<Guid, MediaItemAspectMetadata> LocallySupportedReimportMediaItemAspectTypes
    {
      get { return _locallySupportedReimportMediaItemAspectTypes; }
    }

    public async Task RegisterLocallyKnownMediaItemAspectTypeAsync(IEnumerable<MediaItemAspectMetadata> miaTypes)
    {
      await Task.WhenAll(miaTypes.Select(RegisterLocallyKnownMediaItemAspectTypeAsync));
    }

    public async Task RegisterLocallyKnownMediaItemAspectTypeAsync(MediaItemAspectMetadata miaType)
    {
      if (_locallyKnownMediaItemAspectTypes.ContainsKey(miaType.AspectId))
        return;
      _locallyKnownMediaItemAspectTypes.Add(miaType.AspectId, miaType);
      IServerConnectionManager serverConnectionManager = ServiceRegistration.Get<IServerConnectionManager>();
      IContentDirectory cd = serverConnectionManager?.ContentDirectory;
      if (cd != null)
        await cd.AddMediaItemAspectStorageAsync(miaType);
    }

    public Task RegisterLocallySupportedReimportMediaItemAspectTypeAsync(MediaItemAspectMetadata miaType)
    {
      _locallySupportedReimportMediaItemAspectTypes.TryAdd(miaType.AspectId, miaType);
      return Task.CompletedTask;
    }
  }
}
