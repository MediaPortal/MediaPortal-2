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

using System.Net;

namespace UPnP.Infrastructure.Dv.SSDP
{
  /// <summary>
  /// Stores the information about a search request which was not handled yet.
  /// </summary>
  public class PendingSearchRequest
  {
    protected string _st;
    protected EndpointConfiguration _localEndPointConfiguration;
    protected IPEndPoint _requesterEndPoint;

    /// <summary>
    /// Creates a new instance of <see cref="PendingSearchRequest"/>.
    /// </summary>
    /// <param name="st">Search target (search parameter of the same-named parameter in the SSDP search request.</param>
    /// <param name="localEndpointConfiguration">Local UPnP endpoint where the search was received and over that the
    /// search result will be sent.</param>
    /// <param name="requesterEndPoint">IP endpoint of the search invoker.</param>
    public PendingSearchRequest(string st, EndpointConfiguration localEndpointConfiguration, IPEndPoint requesterEndPoint)
    {
      _st = st;
      _localEndPointConfiguration = localEndpointConfiguration;
      _requesterEndPoint = requesterEndPoint;
    }

    /// <summary>
    /// Search target which should be found.
    /// </summary>
    public string ST
    {
      get { return _st; }
    }

    /// <summary>
    /// UPnP endpoint to use for sending the search result.
    /// </summary>
    public EndpointConfiguration LocalEndpointConfiguration
    {
      get { return _localEndPointConfiguration; }
    }

    /// <summary>
    /// IP endpoint of the invoker of the search.
    /// </summary>
    public IPEndPoint RequesterEndPoint
    {
      get { return _requesterEndPoint; }
    }
  }
}
