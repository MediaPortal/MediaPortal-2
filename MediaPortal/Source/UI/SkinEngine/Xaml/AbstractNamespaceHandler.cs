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
using System.Reflection;
using System.Runtime.Serialization;
using MediaPortal.Common.General;
using MediaPortal.UI.SkinEngine.Xaml.Exceptions;
using MediaPortal.UI.SkinEngine.Xaml.Interfaces;

namespace MediaPortal.UI.SkinEngine.Xaml
{
  public abstract class AbstractNamespaceHandler : INamespaceHandler
  {
    #region Protected methods

    internal MethodInfo GetAttachedPropertyGetter(string propertyProvider, string propertyName)
    {
      Type type = GetElementType(propertyProvider, true);
      MethodInfo mi = null;
      while (mi == null && type != null)
      {
        mi = type.GetMethod("Get" + propertyName + "AttachedProperty", BindingFlags.Public | BindingFlags.Static);
        type = type.BaseType;
      }
      return mi;
    }

    internal static bool FindBestConstructor(Type t, IList<object> parameters, out ConstructorInfo constructorInfo, out object[] convertedParameters)
    {
      MethodBase methodBase;
      object[] parameterObjects = new object[parameters.Count];
      parameters.CopyTo(parameterObjects, 0);
      if (ReflectionHelper.FindBestMember(t.GetConstructors(), parameterObjects, out methodBase, out convertedParameters))
      {
        constructorInfo = (ConstructorInfo) methodBase;
        return true;
      }
      constructorInfo = null;
      return false;
    }

    #endregion

    #region INamespaceHandler implementation

    public object InstantiateElement(IParserContext context, string typeName, IList<object> parameters)
    {
      try
      {
        Type t = GetElementType(typeName);
        // Special case for structs: they cannot be created like classes. So we use another way...
        if (t.IsValueType)
          return FormatterServices.GetUninitializedObject(t);

        // Regular class types
        ConstructorInfo constructorInfo;
        object[] convertedParameters;
        if (!FindBestConstructor(t, parameters, out constructorInfo, out convertedParameters))
          throw new XamlParserException("Error creating element type '{0}'", typeName);
        return constructorInfo.Invoke(convertedParameters);
      }
      catch (Exception e)
      {
        if (e is XamlParserException)
          throw;
        throw new XamlParserException("Error creating element type '{0}'", e, typeName);
      }
    }

    public virtual Type GetElementType(string typeName)
    {
      return GetElementType(typeName, false);
    }

    public abstract Type GetElementType(string typeName, bool includeAbstractTypes);

    public bool HasAttachedProperty(string propertyProvider, string propertyName, object targetObject)
    {
      return GetAttachedPropertyGetter(propertyProvider, propertyName) != null;
    }

    public AbstractProperty GetAttachedProperty(string propertyProvider, string propertyName, object targetObject)
    {
      MethodInfo mi = GetAttachedPropertyGetter(propertyProvider, propertyName);
      if (mi != null)
        return (AbstractProperty) mi.Invoke(targetObject, new object[] {targetObject});
      throw new InvalidOperationException(string.Format("Attached property '{0}.{1}' is not available on new target object '{2}'",
          propertyProvider, propertyName, targetObject));
    }

    #endregion
  }
}
