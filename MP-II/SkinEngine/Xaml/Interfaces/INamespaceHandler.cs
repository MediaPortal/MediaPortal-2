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

using System;
using System.Collections.Generic;

namespace MediaPortal.SkinEngine.Xaml.Interfaces
{
  /// <summary>
  /// Defines the callback interface the <see cref="XamlParser"/> uses to
  /// handle XAML elements of an XML namespace. The term "namespace"
  /// describes here an XML namespace, not necessarily a code namespace.
  /// Often, an XML namespace will be mapped to a code namespace even so.
  /// </summary>
  public interface INamespaceHandler
  {
    /// <summary>
    /// Instantiates a XAML element and returns its visual element or the
    /// binding defined by it.
    /// </summary>
    /// <param name="context">The context instance during the parsing process.</param>
    /// <param name="typeName">The type name of the element to be instantiated.</param>
    /// <param name="namespaceURI">The URI of the namespace where the element
    /// to be instantiated is located.</param>
    /// <param name="parameters">List of parameters to pass to the constructor of the
    /// element to be instantiated.</param>
    /// <returns>The new visual's element or markup extension instance.</returns>
    object InstantiateElement(IParserContext context, string typeName, string namespaceURI,
        IList<object> parameters);

    /// <summary>
    /// Returns the type of the specified <paramref name="typeName"/> for the
    /// namespace handled by this namespace handler.
    /// </summary>
    /// <param name="typeName">XAML element name.</param>
    /// <param name="namespaceURI">XML namespace uri of the XAML element.</param>
    Type GetElementType(string typeName, string namespaceURI);

    /// <summary>
    /// Returns attached property with name <paramref name="propertyName"/>
    /// of the specified <paramref name="propertyProvider"/> for the specified
    /// <paramref name="targetObject"/> in the specified <paramref name="namespaceURI"/>.
    /// </summary>
    /// <example>
    /// Attached properties in XAML files are assigned on an element e by either
    /// <code><e [PropertyProvider].[PropertyName]=[Value]></code>
    /// or 
    /// <code><e><[PropertyProvider].[PropertyName]>[Value]</[PropertyProvider].[PropertyName]></e></code>
    /// </example>
    /// <param name="propertyProvider">Property provider of the attached property to
    /// return.</param>
    /// <param name="propertyName">Name of the attached property to return.</param>
    /// <param name="targetObject">The target object, on which the property should be
    /// set.</param>
    /// <param name="namespaceURI">Namespace of the property provider.</param>
    /// <returns>Data descriptor for the requested attached property.</returns>
    IDataDescriptor GetAttachedProperty(string propertyProvider,
        string propertyName, object targetObject, string namespaceURI);

    /// <summary>
    /// Returns the information, if the attached property with name <paramref name="propertyName"/>
    /// of the specified <paramref name="propertyProvider"/> for the specified
    /// <paramref name="targetObject"/> in the specified <paramref name="namespaceURI"/> exists.
    /// </summary>
    /// <param name="propertyProvider">Property provider of the attached property to
    /// request.</param>
    /// <param name="propertyName">Name of the attached property to request.</param>
    /// <param name="targetObject">The target object, on which the property should be
    /// placed.</param>
    /// <param name="namespaceURI">Namespace of the property provider.</param>
    /// <returns>true, if the specified attached property exists, else false.</returns>
    bool HasAttachedProperty(string propertyProvider,
        string propertyName, object targetObject, string namespaceURI);
  }
}
