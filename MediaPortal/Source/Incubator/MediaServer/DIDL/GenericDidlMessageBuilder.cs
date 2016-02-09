#region Copyright (C) 2007-2012 Team MediaPortal

/*
    Copyright (C) 2007-2012 Team MediaPortal
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
using System.Collections;
using System.IO;
using System.Text;
using System.Xml;
using MediaPortal.Plugins.MediaServer.Objects;
using MediaPortal.Common.Logging;
using MediaPortal.Common;

namespace MediaPortal.Plugins.MediaServer.DIDL
{
  public class GenericDidlMessageBuilder
  {
    public enum ContentBuilder
    {
      GenericContentBuilder,
      SamsungContentBuilder,
      SonyContentBuilder,
      PacketContentBuilder
    }

    public static GenericDidlMessageBuilder GetDidlMessageBuilder(ContentBuilder Builder)
    {
      if (Builder == ContentBuilder.SamsungContentBuilder)
      {
        return new SamsungDidlMessageBuilder();
      }
      else if (Builder == ContentBuilder.SonyContentBuilder)
      {
        return new SonyDidlMessageBuilder();
      }
      else if (Builder == ContentBuilder.PacketContentBuilder)
      {
        return new PacketVideoDidlMessageBuilder();
      }
      return new GenericDidlMessageBuilder();
    }

    private readonly StringBuilder _message;
    protected XmlWriter _xml;
    protected bool _hasCompleted;
    protected PropertyFilter _filter;

    public GenericDidlMessageBuilder()
    {
      _message = new StringBuilder(10000);
      _hasCompleted = false;
      StartMessage();
    }

    protected void StartMessage()
    {
      _xml = XmlWriter.Create(new StringWriterWithEncoding(_message, Encoding.UTF8), DEFAULT_XML_WRITER_SETTINGS);
      //_xml.WriteStartDocument();
      _xml.WriteStartElement(string.Empty, "DIDL-Lite", "urn:schemas-upnp-org:metadata-1-0/DIDL-Lite/");
      _xml.WriteAttributeString("xmlns", "dc", null, "http://purl.org/dc/elements/1.1/");
      _xml.WriteAttributeString("xmlns", "dlna", null, "urn:schemas-dlna-org:metadata-1-0/");
      _xml.WriteAttributeString("xmlns", "upnp", null, "urn:schemas-upnp-org:metadata-1-0/upnp/");
    }

    protected void AddMessageAttribute(string prefix, string localName, string ns, string value)
    {
      _xml.WriteAttributeString(prefix, localName, ns, value);
    }

    public void Build(string filter, object directoryPropertyObject)
    {
      _filter = new PropertyFilter(filter);
      BuildInternal(directoryPropertyObject);
      EndMessage();
    }

    protected virtual void BuildInternal(object directoryPropertyObject)
    {
      if (directoryPropertyObject == null)
        return;
      if (!_hasCompleted)
      {
        if (directoryPropertyObject is IDirectoryContainer)
        {
          _xml.WriteStartElement("container");
        }
        else if (directoryPropertyObject is IDirectoryItem)
        {
          _xml.WriteStartElement("item");
        }
        else
        {
          throw new ArgumentException("directoryPropertyObject isn't either an IDirectoryContainer or IDirectoryItem");
        }
        ObjectRenderer.Render(_filter, directoryPropertyObject, _xml);
        _xml.WriteEndElement();
      }
    }

    public void BuildAll(string filter, IEnumerable objs)
    {
      _filter = new PropertyFilter(filter);
      foreach (var obj in objs)
      {
        BuildInternal(obj);
      }
      EndMessage();
    }

    protected void EndMessage()
    {
      _xml.WriteEndElement();
      _xml.Close();
      _hasCompleted = true;
#if DEBUG
      StringBuilder prettyXml = new StringBuilder();
      XmlDocument document = new XmlDocument();
      document.LoadXml(_message.ToString());
      XmlWriterSettings settings = new XmlWriterSettings();
      settings.OmitXmlDeclaration = true;
      settings.Indent = true;
      settings.NewLineOnAttributes = true;
      using (var xmlWriter = XmlWriter.Create(prettyXml, settings))
      {
        document.Save(xmlWriter);
      }
      Logger.Debug(prettyXml.ToString());
#endif
    }

    public new string ToString()
    {
      return _message.ToString();
    }

    /// <summary>
    /// Default settins to be used by XML writers.
    /// </summary>
    public static XmlWriterSettings DEFAULT_XML_WRITER_SETTINGS = new XmlWriterSettings
                                                                    {
                                                                      CheckCharacters = false,
                                                                      Encoding = Encoding.UTF8,
                                                                      Indent = false,
                                                                      OmitXmlDeclaration = true
                                                                    };

    protected static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }

  public class StringWriterWithEncoding : StringWriter
  {
    protected Encoding _encoding;

    public StringWriterWithEncoding(Encoding encoding)
    {
      _encoding = encoding;
    }

    public StringWriterWithEncoding(IFormatProvider formatProvider, Encoding encoding)
      : base(formatProvider)
    {
      _encoding = encoding;
    }

    public StringWriterWithEncoding(StringBuilder sb, Encoding encoding)
      : base(sb)
    {
      _encoding = encoding;
    }

    public StringWriterWithEncoding(StringBuilder sb, IFormatProvider formatProvider, Encoding encoding)
      : base(sb, formatProvider)
    {
      _encoding = encoding;
    }

    public override Encoding Encoding
    {
      get { return _encoding; }
    }
  }
}
