#region Copyright (C) 2007-2021 Team MediaPortal

/*
    Copyright (C) 2007-2021 Team MediaPortal
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

using System.Threading.Tasks;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.Helpers;

namespace MediaPortal.Extensions.OnlineLibraries
{
  /// <summary>
  /// Hands <see cref="OnlineMatcherService"/> to callers that asked for
  /// <see cref="IOnlineMovieMatcher"/>.
  /// </summary>
  /// <remarks>
  /// A forwarder rather than a second interface on the service itself because the plugin manager
  /// builds one object per <c>Service</c> entry: registering <see cref="OnlineMatcherService"/> twice
  /// would give the server two of them, each with its own message queue and its own settings watcher,
  /// for a service the rest of the tree reaches through static state. Forwarding to
  /// <see cref="OnlineMatcherService.Instance"/> keeps the one instance the whole server already
  /// shares.
  /// </remarks>
  public class OnlineMovieMatcher : IOnlineMovieMatcher
  {
    /// <inheritdoc/>
    public Task<bool> FindAndUpdateMovieAsync(MovieInfo movieInfo, string category)
    {
      return OnlineMatcherService.Instance.FindAndUpdateMovieAsync(movieInfo, category);
    }
  }
}
