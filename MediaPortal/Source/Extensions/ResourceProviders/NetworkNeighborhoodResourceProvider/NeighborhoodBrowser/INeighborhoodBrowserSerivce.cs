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
using System.Net;

namespace MediaPortal.Extensions.ResourceProviders.NetworkNeighborhoodResourceProvider.NeighborhoodBrowser
{
  /// <summary>
  /// A service that is able to enumerate all computers in the NetworkNeighborhood
  /// </summary>
  /// <remarks>
  /// There is no bullet-proof way to enumerate all computers in the NetworkNeighborhood. We may therefore need
  /// different ways to get a list of all computers. The system can register different <see cref="INeighborhoodBrowser"/>s
  /// with the INeighborhoodBrowserSerivce. Every <see cref="INeighborhoodBrowser"/> uses a different technique
  /// to get a list of computers in the network (e.g. by using the DirectoryEntry class or by pinging all IP addresses in the
  /// local subnet, etc.). When the <see cref="Hosts"/> property of the INeighborhoodBrowserSerivce is accessed,
  /// it calls all previously registered <see cref="INeighborhoodBrowser"/>s and assembles a unified list of computers in the
  /// network, filtering out duplicates that were found by different <see cref="INeighborhoodBrowser"/>s.
  /// </remarks>
  public interface INeighborhoodBrowserSerivce : IDisposable
  {
    /// <summary>
    /// Returns a deduplicated list of all computers in the network found by all registered <see cref="INeighborhoodBrowser"/>s
    /// </summary>
    ICollection<IPHostEntry> Hosts { get; }

    /// <summary>
    /// Registers a new <see cref="INeighborhoodBrowser"/> to be used by the INeighborhoodBrowserSerivce
    /// </summary>
    /// <param name="browser"></param>
    void RegisterBrowser(INeighborhoodBrowser browser);
  }
}
