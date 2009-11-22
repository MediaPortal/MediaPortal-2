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

namespace MediaPortal.UI.SkinEngine.Xaml.Interfaces
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
    /// Sets the member specified by the <paramref name="dd"/> data descriptor
    /// to the given value.
    /// The member can either be defined on a visual's instance or
    /// on a markup extension instance, or it may be an attached property.
    /// This method handles unbound/simple values like strings or other simple typed values
    /// and also complex types which may be bound to the property, like markup extensions.
    /// </summary>
    /// <param name="dd">Data descriptor describing the member to assign.</param>
    /// <param name="value">Value to set the member to. The value type will
    /// be converted to the member's type, if necessary.</param>
    void HandleMemberAssignment(IDataDescriptor dd, object value);

    /// <summary>
    /// Sets a context variable which is available for the lifetime of this parser contexts, or until
    /// it is reset.
    /// </summary>
    /// <param name="key">Key of the variable.</param>
    /// <param name="value">Value of the variable.</param>
    void SetContextVariable(object key, object value);

    /// <summary>
    /// Resets a context variable. After this method is called, the specified variable cannot be accessed
    /// any more via this parser context.
    /// </summary>
    /// <param name="key">Key of the variable.</param>
    void ResetContextVariable(object key);

    /// <summary>
    /// Returns the context variable which was set for the specified <paramref name="key"/>.
    /// </summary>
    /// <param name="key">Key of the variable.</param>
    /// <returns>Context variable of the specified <paramref name="key"/> or <c>null</c>, if the variable
    /// is not present in this parser context.</returns>
    object GetContextVariable(object key);
  }
}
