namespace UPnP.Infrastructure.Dv.DeviceTree
{
  /// <summary>
  /// Callback interface to retrieve icons.
  /// </summary>
  public class IconDescriptor
  {
    /// <summary>
    /// Mimetype of the item (cf. RFC 2045, 2046, and 2387). At least one icon should be of type "image/png"
    /// (Portable Network Graphics, see IETF RFC 2083).
    /// </summary>
    public string MimeType;

    /// <summary>
    /// Horizontal dimension of the icon in pixels.
    /// </summary>
    public int Width;

    /// <summary>
    /// Vertical dimension of the icon in pixels.
    /// </summary>
    public int Height;

    /// <summary>
    /// Number of color bits per pixel.
    /// </summary>
    public int ColorDepth;

    /// <summary>
    /// Delegate function to retrieve the URL to the icon's resource, depending on the network interface for a given
    /// UPnP endpoint.
    /// </summary>
    public GetURLForEndpointDlgt GetIconURLDelegate;
  }
}