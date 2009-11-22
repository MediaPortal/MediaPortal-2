#region Copyright (C) 2007-2009 Team MediaPortal

/*
    Copyright (C) 2007-2009 Team MediaPortal
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

namespace MediaPortal.Utilities.Xml
{
  /// <summary>
  /// Xml Writer that doesn't write the Namespace attributes when they are standard.
  /// </summary>
  public class XmlNoNamespaceWriter : System.Xml.XmlTextWriter
  {
    #region Variables
    bool _skipAttribute = false;
    #endregion

    #region Constructors/Destructors
    /// <summary>
    /// Initializes a new instance of the <see cref="XmlNoNamespaceWriter"/> class.
    /// </summary>
    /// <param name="writer">The writer.</param>
    public XmlNoNamespaceWriter(System.IO.TextWriter writer)
      : base(writer)
    {
    }
    #endregion

    #region Public Methods
    /// <summary>
    /// Writes the specified start tag and associates it with the given namespace and prefix.
    /// </summary>
    /// <param name="prefix">The namespace prefix of the element.</param>
    /// <param name="localName">The local name of the element.</param>
    /// <param name="ns">The namespace URI to associate with the element. If this namespace is already in scope and has an associated prefix then the writer automatically writes that prefix also.</param>
    /// <exception cref="T:System.InvalidOperationException">The writer is closed. </exception>
    public override void WriteStartElement(string prefix, string localName, string ns)
    {
      base.WriteStartElement(null, localName, null);
    }

    /// <summary>
    /// Writes the start of an attribute.
    /// </summary>
    /// <param name="prefix">Namespace prefix of the attribute.</param>
    /// <param name="localName">LocalName of the attribute.</param>
    /// <param name="ns">NamespaceURI of the attribute</param>
    /// <exception cref="T:System.ArgumentException">localName is either null or String.Empty. </exception>
    public override void WriteStartAttribute(string prefix, string localName, string ns)
    {
      //If the prefix or localname are "xmlns", don't write it.
      if ((prefix != null && prefix.CompareTo("xmlns") == 0) ||
        (localName != null && localName.CompareTo("xmlns") == 0))
      {
        _skipAttribute = true;
      }
      else
      {
        base.WriteStartAttribute(null, localName, null);
      }
    }

    /// <summary>
    /// Writes the given text content.
    /// </summary>
    /// <param name="text">Text to write.</param>
    /// <exception cref="T:System.ArgumentException">The text string contains an invalid surrogate pair. </exception>
    public override void WriteString(string text)
    {
      //If we are writing an attribute, the text for the xmlns
      //or xmlns:prefix declaration would occur here.  Skip
      //it if this is the case.
      if (!_skipAttribute)
      {
        base.WriteString(text);
      }
    }

    /// <summary>
    /// Closes the previous <see cref="M:System.Xml.XmlTextWriter.WriteStartAttribute(System.String,System.String,System.String)"></see> call.
    /// </summary>
    public override void WriteEndAttribute()
    {
      //If we skipped the WriteStartAttribute call, we have to
      //skip the WriteEndAttribute call as well or else the XmlWriter
      //will have an invalid state.
      if (!_skipAttribute)
      {
        base.WriteEndAttribute();
      }
      //reset the boolean for the next attribute.
      _skipAttribute = false;
    }

    /// <summary>
    /// Writes out the namespace-qualified name. This method looks up the prefix that is in scope for the given namespace.
    /// </summary>
    /// <param name="localName">The local name to write.</param>
    /// <param name="ns">The namespace URI to associate with the name.</param>
    /// <exception cref="T:System.ArgumentException">localName is either null or String.Empty.localName is not a valid name according to the W3C Namespaces spec. </exception>
    public override void WriteQualifiedName(string localName, string ns)
    {
      //Always write the qualified name using only the
      //localname.
      base.WriteQualifiedName(localName, null);
    }
    #endregion
  }
}
