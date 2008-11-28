#region Copyright (C) 2007-2008 Team MediaPortal

/*
    Copyright (C) 2007-2008 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal II

    MediaPortal II is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal II is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System.IO;
using System.Text;
using System.Xml;

namespace MediaPortal.Core.Services.Settings
{
  /// <summary>
  /// Xml Writer that doesn't write the Namespace attributes nor the XML header.
  /// </summary>
  public class XmlInnerElementWriter : XmlTextWriter
  {
    #region Variables

    private bool _skipAttribute = false;

    #endregion

    #region Constructors/Destructors

    public XmlInnerElementWriter(StringBuilder output) : base(new StringWriter(output)) { }

    #endregion

    #region Public Methods

    public override void WriteStartDocument() { }

    public override void WriteStartDocument(bool standalone) { }

    public override void WriteStartElement(string prefix, string localName, string ns)
    {
      base.WriteStartElement(null, localName, null);
    }

    public override void WriteStartAttribute(string prefix, string localName, string ns)
    {
      //If the prefix or localname are "xmlns", don't write it.
      if (prefix == "xmlns" ||
          localName == "xmlns")
        _skipAttribute = true;
      else
        base.WriteStartAttribute(null, localName, null);
    }

    public override void WriteString(string text)
    {
      //If we are writing an attribute, the text for the xmlns
      //or xmlns:prefix declaration would occur here.  Skip
      //it if this is the case.
      if (!_skipAttribute)
        base.WriteString(text);
    }

    public override void WriteEndAttribute()
    {
      //If we skipped the WriteStartAttribute call, we have to
      //skip the WriteEndAttribute call as well or else the XmlWriter
      //will have an invalid state.
      if (!_skipAttribute)
        base.WriteEndAttribute();
      //reset the boolean for the next attribute.
      _skipAttribute = false;
    }

    public override void WriteQualifiedName(string localName, string ns)
    {
      //Always write the qualified name using only the
      //localname.
      base.WriteQualifiedName(localName, null);
    }

    #endregion
  }
}
