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
