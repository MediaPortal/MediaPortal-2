#region Copyright (C) 2007-2009 Team MediaPortal

/* 
 *  Copyright (C) 2007-2009 Team MediaPortal
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

using System.IO;
using InvalidDataException=MediaPortal.Utilities.Exceptions.InvalidDataException;

namespace UPnP.Infrastructure.Dv.HTTP
{
  /// <summary>
  /// Encapsulates an HTTP request.
  /// </summary>
  /// <remarks>
  /// This class contains methods to create an HTTP request which can produce a byte array to be sent to the network
  /// and the other way, to parse an <see cref="SimpleHTTPRequest"/> instance from a given byte stream.
  /// </remarks>
  public class SimpleHTTPRequest : SimpleHTTPMessage
  {
    protected string _method;
    protected string _param;

    internal SimpleHTTPRequest() { }

    public SimpleHTTPRequest(string method, string param)
    {
      _method = method;
      _param = param;
    }

    public string Method
    {
      get { return _method; }
      set { _method = value; }
    }

    public string Param
    {
      get { return _param; }
      set { _param = value; }
    }

    protected override string EncodeStartingLine()
    {
      return string.Format("{0} {1} {2}", _method, _param, _httpVersion);
    }

    /// <summary>
    /// Parses the HTTP request out of the given <paramref name="stream"/>.
    /// </summary>
    /// <param name="stream">HTTP data stream to parse.</param>
    /// <param name="result">Returns the parsed HTTP request instance.</param>
    /// <exception cref="MediaPortal.Utilities.Exceptions.InvalidDataException">If the given <paramref name="data"/> is malformed.</exception>
    public static void Parse(Stream stream, out SimpleHTTPRequest result)
    {
      result = new SimpleHTTPRequest();
      string firstLine;
      result.ParseHeaderAndBody(stream, out firstLine);
      string[] elements = firstLine.Split(' ');
      if (elements.Length != 3)
        throw new InvalidDataException("Invalid HTTP request header starting line '{0}'", firstLine);
      string httpVersion = elements[2];
      if (httpVersion != "HTTP/1.0" && httpVersion != "HTTP/1.1")
        throw new InvalidDataException("Invalid HTTP request header starting line '{0}'", firstLine);
      result._method = elements[0];
      result._param = elements[1];
      result._httpVersion = httpVersion;
    }
  }
}
