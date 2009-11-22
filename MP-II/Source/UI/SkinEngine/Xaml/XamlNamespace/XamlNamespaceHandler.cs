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
using MediaPortal.UI.SkinEngine.Xaml.Exceptions;
using MediaPortal.UI.SkinEngine.Xaml.Interfaces;
using MediaPortal.UI.SkinEngine.Xaml.XamlNamespace;

namespace MediaPortal.UI.SkinEngine.Xaml.XamlNamespace
{
  /// <summary>
  /// Defines the handler for the standard XAML namespace.
  /// </summary>
  public class XamlNamespaceHandler : INamespaceHandler
  {
    protected static IDictionary<string, Type> objectTypes = new Dictionary<string, Type>();
    static XamlNamespaceHandler()
    {
      objectTypes.Add("Array", typeof(ArrayMarkupExtension));
      objectTypes.Add("Null", typeof(NullMarkupExtension));
      objectTypes.Add("Static", typeof(StaticMarkupExtension));
      objectTypes.Add("Type", typeof(TypeMarkupExtension));
      objectTypes.Add("XData", typeof(XDataDirective));
    }

    #region INamespaceHandler implementation

    public object InstantiateElement(IParserContext context, string typeName, string namespaceURI,
        IList<object> parameters)
    {
      try
      {
        Type t = GetElementType(typeName, namespaceURI);
        object[] parameterObjects = new object[parameters.Count];
        parameters.CopyTo(parameterObjects, 0);
        return Activator.CreateInstance(t, parameterObjects);
      }
      catch (Exception e)
      {
        if (e is XamlParserException)
          throw e;
        throw new XamlParserException("Error creating element type '{0}' in namespace '{1}'",
          e, typeName, namespaceURI);
      }
    }

    public Type GetElementType(string typeName, string namespaceURI)
    {
      try
      {
        return objectTypes[typeName];
      }
      catch
      {
        throw new XamlParserException("Element type '{0}' is not present in namespace '{1}'",
          typeName, namespaceURI);
      }
    }

    public IDataDescriptor GetAttachedProperty(string propertyProvider,
        string propertyName, object targetObject, string namespaceURI)
    {
      throw new XamlBindingException("Namespace handler {0} doesn't provide attached properties",
        typeof(XamlNamespaceHandler).Name);
    }

    public bool HasAttachedProperty(string propertyProvider,
        string propertyName, object targetObject, string namespaceURI)
    {
      return false;
    }

    #endregion
  }
}
