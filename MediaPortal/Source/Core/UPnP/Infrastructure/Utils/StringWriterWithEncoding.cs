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
using System.IO;
using System.Text;

namespace UPnP.Infrastructure.Utils
{
  public class StringWriterWithEncoding : StringWriter
  {
    protected Encoding _encoding;

    public StringWriterWithEncoding(Encoding encoding)
    {
      _encoding = encoding;
    }

    public StringWriterWithEncoding(IFormatProvider formatProvider, Encoding encoding) : base(formatProvider)
    {
      _encoding = encoding;
    }

    public StringWriterWithEncoding(StringBuilder sb, Encoding encoding) : base(sb)
    {
      _encoding = encoding;
    }

    public StringWriterWithEncoding(StringBuilder sb, IFormatProvider formatProvider, Encoding encoding) : base(sb, formatProvider)
    {
      _encoding = encoding;
    }

    public override Encoding Encoding
    {
      get { return _encoding; }
    }
  }
}