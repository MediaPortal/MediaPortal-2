//
// System.Web.IHttpRequest.cs 
//
// 
// Author:
//	Miguel de Icaza (miguel@novell.com)
//	Gonzalo Paniagua Javier (gonzalo@novell.com)
//

//
// Copyright (C) 2005 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
using System;
using System.Globalization;
using System.Text;
using System.IO;

namespace HttpServer.FormDecoders
{
  internal class HttpMultipart
  {
    /// <summary>Represents a field in a multipart form</summary>
    public class Element
    {
      public string ContentType;
      public string Name;
      public string Filename;
      public long Start;
      public long Length;

      public override string ToString()
      {
        return "ContentType " + ContentType + ", Name " + Name + ", Filename " + Filename + ", Start " +
            Start + ", Length " + Length;
      }
    }

    private readonly Stream _data;
    private readonly string _boundary;
    private readonly byte[] _boundaryBytes;
    private readonly byte[] _buffer;
    private bool _eof;
    private readonly Encoding _encoding;
    private readonly StringBuilder _sb;

    private const byte Lf = (byte) '\n', Cr = (byte) '\r';

    // See RFC 2046 
    // In the case of multipart entities, in which one or more different
    // sets of data are combined in a single body, a "multipart" media type
    // field must appear in the entity's header.  The body must then contain
    // one or more body parts, each preceded by a boundary delimiter line,
    // and the last one followed by a closing boundary delimiter line.
    // After its boundary delimiter line, each body part then consists of a
    // header area, a blank line, and a body area.  Thus a body part is
    // similar to an RFC 822 message in syntax, but different in meaning.

    public HttpMultipart(Stream data, string boundry, Encoding encoding)
    {
      _data = data;
      _boundary = boundry;
      _boundaryBytes = encoding.GetBytes(boundry);
      _buffer = new byte[_boundaryBytes.Length + 2]; // CRLF or '--'
      _encoding = encoding;
      _sb = new StringBuilder();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    /// <exception cref="ObjectDisposedException"></exception>
    private string ReadLine()
    {
      // CRLF or Lf are ok as line endings.
      bool gotCr = false;
      _sb.Length = 0;
      while (true)
      {
        int b = _data.ReadByte();
        if (b == -1)
          return null;

        if (b == Lf)
          break;

        gotCr = (b == Cr);
        _sb.Append((char) b);
      }

      if (gotCr)
        _sb.Length--;

      return _sb.ToString();
    }

    private static string GetContentDispositionAttribute(string l, string name)
    {
      int idx = l.IndexOf(name + "=\"");
      if (idx < 0)
        return null;
      int begin = idx + name.Length + "=\"".Length;
      int end = l.IndexOf('"', begin);
      if (end < 0)
        return null;
      if (begin == end)
        return "";
      return l.Substring(begin, end - begin);
    }

    private string GetContentDispositionAttributeWithEncoding(string l, string name)
    {
      int idx = l.IndexOf(name + "=\"");
      if (idx < 0)
        return null;
      int begin = idx + name.Length + "=\"".Length;
      int end = l.IndexOf('"', begin);
      if (end < 0)
        return null;
      if (begin == end)
        return "";

      string temp = l.Substring(begin, end - begin);
      byte[] source = new byte[temp.Length];
      for (int i = temp.Length - 1; i >= 0; i--)
        source[i] = (byte) temp[i];

      return _encoding.GetString(source);
    }

    private bool ReadBoundary()
    {
      try
      {
        string line = ReadLine();
        while (line == "")
          line = ReadLine();
        if (line[0] != '-' || line[1] != '-')
          return false;

        if (!line.EndsWith(_boundary, false, CultureInfo.InvariantCulture))
          return true;
      }
      catch (ArgumentException)
      {
      }
      catch (ObjectDisposedException)
      {
      }

      return false;
    }

    private string ReadHeaders()
    {
      string s = ReadLine();
      if (s == "")
        return null;

      return s;
    }

    public static bool CompareBytes(byte[] orig, byte[] other)
    {
      for (int i = orig.Length - 1; i >= 0; i--)
        if (orig[i] != other[i])
          return false;

      return true;
    }

    private long MoveToNextBoundary()
    {
      long retval = 0;
      bool gotCr = false;

      int state = 0;
      int c = _data.ReadByte();
      while (true)
      {
        if (c == -1)
          return -1;

        if (state == 0 && c == Lf)
        {
          retval = _data.Position - 1;
          if (gotCr)
            retval--;
          state = 1;
          c = _data.ReadByte();
        }
        else if (state == 0)
        {
          gotCr = (c == Cr);
          c = _data.ReadByte();
        }
        else if (state == 1 && c == '-')
        {
          c = _data.ReadByte();
          if (c == -1)
            return -1;

          if (c != '-')
          {
            state = 0;
            gotCr = false;
            continue; // no ReadByte() here
          }

          int nread = _data.Read(_buffer, 0, _buffer.Length);
          int bl = _buffer.Length;
          if (nread != bl)
            return -1;

          if (!CompareBytes(_boundaryBytes, _buffer))
          {
            state = 0;
            _data.Position = retval + 2;
            if (gotCr)
            {
              _data.Position++;
              gotCr = false;
            }
            c = _data.ReadByte();
            continue;
          }

          if (_buffer[bl - 2] == '-' && _buffer[bl - 1] == '-')
          {
            _eof = true;
          }
          else if (_buffer[bl - 2] != Cr || _buffer[bl - 1] != Lf)
          {
            state = 0;
            _data.Position = retval + 2;
            if (gotCr)
            {
              _data.Position++;
              gotCr = false;
            }
            c = _data.ReadByte();
            continue;
          }
          _data.Position = retval + 2;
          if (gotCr)
            _data.Position++;
          break;
        }
        else
        {
          // state == 1
          state = 0; // no ReadByte() here
        }
      }

      return retval;
    }

    public Element ReadNextElement()
    {
      if (_eof || ReadBoundary())
        return null;

      Element elem = new Element();
      string header;
      while ((header = ReadHeaders()) != null)
      {
        if (header.StartsWith("Content-Disposition:", true, CultureInfo.InvariantCulture))
        {
          elem.Name = GetContentDispositionAttribute(header, "name");
          elem.Filename = StripPath(GetContentDispositionAttributeWithEncoding(header, "filename"));
        }
        else if (header.StartsWith("Content-Type:", true, CultureInfo.InvariantCulture))
        {
          elem.ContentType = header.Substring("Content-Type:".Length).Trim();
        }
      }

      long start = _data.Position;
      elem.Start = start;
      long pos = MoveToNextBoundary();
      if (pos == -1)
        return null;

      elem.Length = pos - start;
      return elem;
    }

    private static string StripPath(string path)
    {
      if (path == null || path.Length == 0)
        return path;

      if (path.IndexOf(":\\") != 1)
        return path;
      return path.Substring(path.LastIndexOf("\\") + 1);
    }
  }
}