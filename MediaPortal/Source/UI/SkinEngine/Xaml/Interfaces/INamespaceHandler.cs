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
using System.Collections.Generic;
using MediaPortal.Common.General;

namespace MediaPortal.UI.SkinEngine.Xaml.Interfaces
{
  /// <summary>
  /// Defines the callback interface the <see cref="Parser"/> uses to  handle XAML elements of an XML namespace. The term "namespace"
  /// describes here an XML namespace, not necessarily a code namespace. Often, an XML namespace will be mapped to a code namespace even so.
  /// </summary>
  public interface INamespaceHandler
  {
    /// <summary>
    /// Instantiates a XAML element and returns its visual element or the binding defined by it.
    /// </summary>
    /// <param name="context">The context instance during the parsing process.</param>
    /// <param name="typeName">The type name of the element to be instantiated.</param>
    /// <param name="parameters">List of parameters to pass to the constructor of the element to be instantiated.</param>
    /// <returns>The new visual's element or markup extension instance.</returns>
    object InstantiateElement(IParserContext context, string typeName, IList<object> parameters);

    /// <summary>
    /// Returns the type of the specified <paramref name="typeName"/> for the namespace handled by this namespace handler.
    /// </summary>
    /// <param name="typeName">XAML element name.</param>
    Type GetElementType(string typeName);

    /// <summary>
    /// Returns the type of the specified <paramref name="typeName"/> for the namespace handled by this namespace handler.
    /// </summary>
    /// <param name="typeName">XAML element name.</param>
    /// <param name="includeAbstractTypes"><c>true</c> if abstract types should be included; else <c>false</c>.</param>
    Type GetElementType(string typeName, bool includeAbstractTypes);

    /// <summary>
    /// Returns the information, if the attached property with name <paramref name="propertyName"/> of the specified
    /// <paramref name="propertyProvider"/> for the specified <paramref name="targetObject"/> exists.
    /// </summary>
    /// <param name="propertyProvider">Property provider of the attached property to request.</param>
    /// <param name="propertyName">Name of the attached property to request.</param>
    /// <param name="targetObject">The target object, on which the property should be placed.</param>
    /// <returns>true, if the specified attached property exists, else false.</returns>
    bool HasAttachedProperty(string propertyProvider, string propertyName, object targetObject);

    /// <summary>
    /// Returns the attached property with name <paramref name="propertyName"/> of the specified <paramref name="propertyProvider"/>
    /// for the specified <paramref name="targetObject"/>.
    /// </summary>
    /// <remarks>
    /// <example>
    /// Attached properties in XAML files are assigned on an element e by either
    /// <code>&lt;e [PropertyProvider].[PropertyName]=[Value]&gt;</code>
    /// or 
    /// <code><e>&lt;[PropertyProvider].[PropertyName]&gt;[Value]&lt;/[PropertyProvider].[PropertyName]&gt;</e></code>
    /// </example>
    /// </remarks>
    /// <param name="propertyProvider">Property provider of the attached property to return.</param>
    /// <param name="propertyName">Name of the attached property to return.</param>
    /// <param name="targetObject">The target object, on which the property should be set.</param>
    /// <returns>Data descriptor for the requested attached property.</returns>
    AbstractProperty GetAttachedProperty(string propertyProvider, string propertyName, object targetObject);
  }
}
