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