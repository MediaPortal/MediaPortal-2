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
using System.Reflection;
using Presentation.SkinEngine.XamlParser;
using MediaPortal.Presentation.Properties;

namespace Presentation.SkinEngine.Loader
{
  public class MseAttachedPropertyDataDescriptor: DependencyPropertyDataDescriptor
  {
    protected MseNamespaceHandler _namespaceHandler;
    protected string _propertyProvider;

    public MseAttachedPropertyDataDescriptor(
        MseNamespaceHandler parent, object targetObject,
        string propertyProvider, string propertyName):
        base(targetObject, propertyName, GetAttachedProperty(propertyProvider, propertyName, targetObject))
    {
      _namespaceHandler = parent;
      _propertyProvider = propertyProvider;
    }

    internal static Property GetAttachedProperty(string propertyProvider, string propertyName,
        object targetObject)
    {
      MethodInfo mi = MseNamespaceHandler.GetAttachedPropertyGetter(propertyProvider, propertyName);
      if (mi != null)
        return (Property) mi.Invoke(targetObject, new object[] {targetObject});
      else
        throw new InvalidOperationException(string.Format(
            "Attached property '{0}.{1}' is not available on new target object '{2}'",
            propertyProvider, propertyName, targetObject));
    }

    public override IDataDescriptor Retarget(object newTarget)
    {
      MseAttachedPropertyDataDescriptor result;
      if (!CreateAttachedPropertyDataDescriptor(_namespaceHandler, newTarget,
          _propertyProvider, _propertyName, out result))
        throw new InvalidOperationException(string.Format(
                                              "Attached property '{0}.{1}' is not available on new target object '{2}'",
                                              _propertyProvider, _propertyName, newTarget));
      return result;
    }

    public static bool CreateAttachedPropertyDataDescriptor(MseNamespaceHandler parent,
        object targetObj, string propertyProvider, string propertyName,
        out MseAttachedPropertyDataDescriptor result)
    {
      result = null;
      if (targetObj == null)
        throw new NullReferenceException("Target object 'null' is not supported");
      if (!MseNamespaceHandler.HasAttachedProperty(propertyProvider, propertyName, targetObj))
        return false;
      result = new MseAttachedPropertyDataDescriptor(parent, targetObj, propertyProvider, propertyName);
      return true;
    }
  }
}
