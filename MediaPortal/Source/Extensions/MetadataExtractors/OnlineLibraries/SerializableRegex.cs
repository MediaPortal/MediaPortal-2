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
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries
{
  /// <summary>
  /// Wraps a <see cref="Regex"/> object and makes it XmlSerializable for our settings system
  /// </summary>
  public class SerializableRegex : IXmlSerializable
  {
    #region Consts

    private const string XML_ELEMENT_NAME_PATTERN = "pattern";
    private const string XML_ELEMENT_NAME_OPTIONS = "options";
    private const string XML_ELEMENT_NAME_TIMEOUT = "timeout";

    #endregion

    #region Constructors

    public SerializableRegex(string pattern)
    {
      Regex = new Regex(pattern);
    }

    public SerializableRegex(string pattern, RegexOptions options)
    {
      Regex = new Regex(pattern, options);
    }

    public SerializableRegex(string pattern, RegexOptions options, TimeSpan matchTimeout)
    {
      Regex = new Regex(pattern, options, matchTimeout);
    }

    // Used for deserialization only
    protected SerializableRegex() { }

    #endregion

    #region Public properties

    public Regex Regex { get; private set; }

    #endregion

    #region IXmlSerializable implementation

    public XmlSchema GetSchema()
    {
      return null;
    }

    public void ReadXml(XmlReader reader)
    {
      reader.ReadStartElement();
      var pattern = reader.ReadElementContentAsString();
      var options = (RegexOptions)Enum.Parse(typeof(RegexOptions), reader.ReadElementContentAsString());
      var timeout = TimeSpan.Parse(reader.ReadElementContentAsString());
      reader.ReadEndElement();
      Regex = new Regex(pattern, options, timeout);
    }

    public void WriteXml(XmlWriter writer)
    {
      writer.WriteElementString(XML_ELEMENT_NAME_PATTERN, Regex.ToString());
      writer.WriteElementString(XML_ELEMENT_NAME_OPTIONS, Regex.Options.ToString());
      writer.WriteElementString(XML_ELEMENT_NAME_TIMEOUT, Regex.MatchTimeout.ToString());
    }

    #endregion
  }
}
