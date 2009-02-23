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
using System.IO;
using MediaPortal.Core;
using MediaPortal.Core.MediaManagement.MediaProviders;
using MediaPortal.Utilities.Exceptions;

namespace MediaPortal.Media.ClientMediaManager
{
  /// <summary>
  /// Temporary local accessor instance for a media item which might located anywhere in an MP-II system.
  /// Via this instance, the media item, which potentially is located in a remote system, can be accessed
  /// via a local media provider specified by the <see cref="LocalMediaProviderId"/>.
  /// To get a media item accessor, build a <see cref="MediaItemAccessor"/> and use its
  /// <see cref="MediaItemLocator.CreateAccessor"/> method.
  /// The temporary media item accessor must be disposed using its <see cref="MediaItemAccessorBase.Dispose"/> method
  /// when it is not needed any more.
  /// </summary>
  public class MediaItemAccessor : MediaItemAccessorBase
  {
    protected Guid _localMediaProviderId;
    protected string _localMediaProviderPath;

    internal MediaItemAccessor(MediaItemLocator locator,
        Guid localMediaProviderId, string localMediaProviderPath, ITidyUpExecutor tidyUpExecutor) :
        base(locator, tidyUpExecutor)
    {
      _localMediaProviderId = localMediaProviderId;
      _localMediaProviderPath = localMediaProviderPath;
    }

    public Guid LocalMediaProviderId
    {
      get { return _localMediaProviderId; }
    }

    public string LocalMediaProviderPath
    {
      get { return _localMediaProviderPath; }
    }

    /// <summary>
    /// Convenience method for calling the <see cref="IMediaProvider.OpenRead"/> method on the local media provider
    /// with the <see cref="LocalMediaProviderId"/>.
    /// </summary>
    /// <returns>Stream with the media item contents which was opened for read operations.</returns>
    public Stream OpenRead()
    {
      MediaManager mediaManager = ServiceScope.Get<MediaManager>();
      IMediaProvider mediaProvider;
      if (!mediaManager.LocalMediaProviders.TryGetValue(_localMediaProviderId, out mediaProvider))
        throw new IllegalCallException("The media provider with Id '{0}' is not accessible in the current system", _localMediaProviderId);
      return mediaProvider.OpenRead(LocalMediaProviderPath);
    }

    /// <summary>
    /// Convenience method for calling the <see cref="IMediaProvider.OpenWrite"/> method on the local media provider
    /// with the <see cref="LocalMediaProviderId"/>.
    /// </summary>
    /// <returns>Stream with the media item contents which was opened for write operations.</returns>
    public Stream OpenWrite()
    {
      MediaManager mediaManager = ServiceScope.Get<MediaManager>();
      IMediaProvider mediaProvider;
      if (!mediaManager.LocalMediaProviders.TryGetValue(_localMediaProviderId, out mediaProvider))
        throw new IllegalCallException("The media provider with Id '{0}' is not accessible in the current system", _localMediaProviderId);
      return mediaProvider.OpenWrite(LocalMediaProviderPath);
    }
  }
}