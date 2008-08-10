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

namespace MediaPortal.SkinEngine.Xaml.Interfaces
{
  /// <summary>
  /// Holds a context of the current parsing operation in the XAML parser.
  /// Provides context related methods like namespace resolvation and other
  /// public parser utility methods.
  /// </summary>
  public interface IParserContext
  {
    /// <summary>
    /// Returns the context stack for the current element path.
    /// </summary>
    ElementContextStack ContextStack { get; }

    /// <summary>
    /// Loads the XAML file with the specified <paramref name="fileName"/> with
    /// a parser instance configured the same as this instance.
    /// </summary>
    /// <param name="fileName">Name of the file to load, relative to this
    /// parser's file.</param>
    /// <returns>Root element created from the XAML file.</returns>
    object LoadXaml(string fileName);

    /// <summary>
    /// Given a qualified or unqualified XML object name, this method separates
    /// the local name from its namespace declaration and returns the
    /// local element name and its namespace URI. The namespace URI
    /// will be looked up in the current parsing context.
    /// </summary>
    /// <param name="elementName">Name in the form <c>[LocalName]</c> or
    /// <c>[NamespacePrefix]:[LocalName]</c> to be separated into its components</param>
    /// <param name="localName">Returns the extracted local name of the
    /// <paramref name="elementName"/>.</param>
    /// <param name="namespaceURI">Returns the namespace URI for the prefix extracted
    /// from the <paramref name="elementName"/>.</param>
    void LookupNamespace(string elementName, out string localName,
        out string namespaceURI);

    /// <summary>
    /// Returns the namespace handler callback instance for the specified namespace URI.
    /// </summary>
    /// <param name="namespaceURI">The namespaceURI which was used to register
    /// the requested namespace handler.</returns>
    INamespaceHandler GetNamespaceHandler(string namespaceURI);

    /// <summary>
    /// Sets the property specified by the <paramref name="dd"/> property descriptor
    /// to the given value.
    /// The property can either be defined on a visual's instance or
    /// on a markup extension instance, or it may be an attached property.
    /// This method handles unbound/simple values like strings or other simple typed values
    /// and also complex types which may be bound to the property, like markup extensions.
    /// </summary>
    /// <param name="dd">Property descriptor describing the property to assign.</param>
    /// <param name="value">Value to set the property to. The value type will
    /// be converted to the property's type, if necessary.</param>
    void HandlePropertyAssignment(IDataDescriptor dd, object value);
  }
}
