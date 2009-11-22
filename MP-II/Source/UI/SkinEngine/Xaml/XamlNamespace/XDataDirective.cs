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

using System.Xml;
using MediaPortal.UI.SkinEngine.Xaml.Interfaces;

namespace MediaPortal.UI.SkinEngine.Xaml.XamlNamespace
{
  /// <summary>
  /// Implements the x:XData directive element of the XAML namespace.
  /// TODO: This implementation is not finished yet, it still supports the API to set the
  /// XML text and to be parsed by the XAML parser. It is still unusable as there is
  /// no API to access the XML.
  /// </summary>
  public class XDataDirective: INativeXamlObject
  {

    #region Protected fields

    protected string _xmlString = null;

    #endregion

    public XDataDirective() { }

    #region Properties

    /// <summary>
    /// Returns the full member name as specified in the constructing element
    /// for this markup extension instance.
    /// </summary>
    public string XmlString
    {
      get { return _xmlString; }
      set { _xmlString = value; }
    }

    #endregion

    #region INativeXamlObject implementation

    void INativeXamlObject.HandleChildren(IParserContext context, XmlElement thisElement)
    {
      _xmlString = thisElement.InnerXml;
    }

    #endregion
  }
}
