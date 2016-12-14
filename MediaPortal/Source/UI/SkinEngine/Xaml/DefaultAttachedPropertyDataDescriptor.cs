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
using MediaPortal.UI.SkinEngine.Xaml.Interfaces;

namespace MediaPortal.UI.SkinEngine.Xaml
{
  /// <summary>
  /// Default implementation for an attached property data descriptor which is based on an <see cref="INamespaceHandler"/>.
  /// </summary>
  public class DefaultAttachedPropertyDataDescriptor : DependencyPropertyDataDescriptor
  {
    #region Protected fields

    protected INamespaceHandler _namespaceHandler;
    protected string _propertyProvider;

    #endregion

    public DefaultAttachedPropertyDataDescriptor(INamespaceHandler namespaceHandler, object targetObject,
        string propertyProvider, string propertyName) : base(targetObject, propertyName,
        namespaceHandler.GetAttachedProperty(propertyProvider, propertyName, targetObject))
    {
      _namespaceHandler = namespaceHandler;
      _propertyProvider = propertyProvider;
    }

    public static bool CreateAttachedPropertyDataDescriptor(INamespaceHandler namespaceHandler, object targetObj,
        string propertyProvider, string propertyName, out DefaultAttachedPropertyDataDescriptor result)
    {
      result = null;
      if (targetObj == null)
        throw new NullReferenceException("Target object 'null' is not supported");
      if (!namespaceHandler.HasAttachedProperty(propertyProvider, propertyName, targetObj))
        return false;
      result = new DefaultAttachedPropertyDataDescriptor(namespaceHandler, targetObj, propertyProvider, propertyName);
      return true;
    }

    public override IDataDescriptor Retarget(object newTarget)
    {
      DefaultAttachedPropertyDataDescriptor result;
      if (!CreateAttachedPropertyDataDescriptor(_namespaceHandler, newTarget, _propertyProvider, _propertyName, out result))
        throw new InvalidOperationException(string.Format("Attached property '{0}.{1}' is not available on new target object '{2}'",
            _propertyProvider, _propertyName, newTarget));
      return result;
    }

    public override int GetHashCode()
    {
      return base.GetHashCode() + _propertyProvider.GetHashCode();
    }

    public override bool Equals(object other)
    {
      if (!(other is DefaultAttachedPropertyDataDescriptor))
        return false;
      DefaultAttachedPropertyDataDescriptor mapdd = (DefaultAttachedPropertyDataDescriptor) other;
      return base.Equals(other) && _propertyProvider.Equals(mapdd._propertyProvider);
    }
  }
}
