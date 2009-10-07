#region Copyright (C) 2005-2008 Team MediaPortal

/* 
 *  Copyright (C) 2005-2008 Team MediaPortal
 *  http://www.team-mediaportal.com
 *
 *  This Program is free software; you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation; either version 2, or (at your option)
 *  any later version.
 *   
 *  This Program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 *  GNU General Public License for more details.
 *   
 *  You should have received a copy of the GNU General Public License
 *  along with GNU Make; see the file COPYING.  If not, write to
 *  the Free Software Foundation, 675 Mass Ave, Cambridge, MA 02139, USA. 
 *  http://www.gnu.org/copyleft/gpl.html
 *
 */

#endregion

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
