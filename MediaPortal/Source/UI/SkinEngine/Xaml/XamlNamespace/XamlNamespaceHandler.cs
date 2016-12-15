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
using MediaPortal.UI.SkinEngine.Xaml.Exceptions;
using MediaPortal.UI.SkinEngine.Xaml.Interfaces;

namespace MediaPortal.UI.SkinEngine.Xaml.XamlNamespace
{
  /// <summary>
  /// Defines the handler for the standard XAML namespace.
  /// </summary>
  public class XamlNamespaceHandler : INamespaceHandler
  {
    /// <summary>
    /// URI for the "x:" namespace.
    /// </summary>
    public const string XAML_NS_URI = "http://schemas.microsoft.com/winfx/2006/xaml";

    protected static IDictionary<string, Type> objectTypes = new Dictionary<string, Type>();
    static XamlNamespaceHandler()
    {
      objectTypes.Add("Array", typeof(ArrayExtension));
      objectTypes.Add("Null", typeof(NullExtension));
      objectTypes.Add("Static", typeof(StaticExtension));
      objectTypes.Add("Type", typeof(TypeExtension));
      objectTypes.Add("XData", typeof(XDataDirective));
    }

    #region INamespaceHandler implementation

    public object InstantiateElement(IParserContext context, string typeName, IList<object> parameters)
    {
      try
      {
        Type t = GetElementType(typeName);
        object[] parameterObjects = new object[parameters.Count];
        parameters.CopyTo(parameterObjects, 0);
        return Activator.CreateInstance(t, parameterObjects);
      }
      catch (Exception e)
      {
        if (e is XamlParserException)
          throw;
        throw new XamlParserException("Error creating element type '{0}'", e, typeName);
      }
    }

    public Type GetElementType(string typeName)
    {
      return GetElementType(typeName, false);
    }

    public Type GetElementType(string typeName, bool includeAbstractTypes)
    {
      Type type;
      try
      {
        type = objectTypes[typeName];
      }
      catch
      {
        throw new XamlParserException("Element type '{0}' is not present", typeName);
      }
      if (!includeAbstractTypes && type.IsAbstract)
      {
        throw new XamlParserException("Element type '{0}' is abstract", typeName);
      }
      return type;
    }

    public AbstractProperty GetAttachedProperty(string propertyProvider, string propertyName, object targetObject)
    {
      throw new XamlBindingException("Namespace handler {0} doesn't provide attached properties", typeof(XamlNamespaceHandler).Name);
    }

    public bool HasAttachedProperty(string propertyProvider, string propertyName, object targetObject)
    {
      return false;
    }

    #endregion
  }
}
