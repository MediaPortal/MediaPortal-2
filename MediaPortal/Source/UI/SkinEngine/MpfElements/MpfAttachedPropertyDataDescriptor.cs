#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
    http://www.team-mediaportal.com

    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Reflection;
using MediaPortal.Core.General;
using MediaPortal.UI.SkinEngine.Xaml;

namespace MediaPortal.UI.SkinEngine.MpfElements
{
  public class MpfAttachedPropertyDataDescriptor: DependencyPropertyDataDescriptor
  {
    protected string _propertyProvider;

    public MpfAttachedPropertyDataDescriptor(
        object targetObject,
        string propertyProvider, string propertyName):
        base(targetObject, propertyName, GetAttachedProperty(propertyProvider, propertyName, targetObject))
    {
      _propertyProvider = propertyProvider;
    }

    internal static AbstractProperty GetAttachedProperty(string propertyProvider, string propertyName,
        object targetObject)
    {
      MethodInfo mi = MpfNamespaceHandler.GetAttachedPropertyGetter(propertyProvider, propertyName);
      if (mi != null)
        return (AbstractProperty) mi.Invoke(targetObject, new object[] {targetObject});
      else
        throw new InvalidOperationException(string.Format(
            "Attached property '{0}.{1}' is not available on new target object '{2}'",
            propertyProvider, propertyName, targetObject));
    }

    public override IDataDescriptor Retarget(object newTarget)
    {
      MpfAttachedPropertyDataDescriptor result;
      if (!CreateAttachedPropertyDataDescriptor(newTarget,
          _propertyProvider, _propertyName, out result))
        throw new InvalidOperationException(string.Format(
            "Attached property '{0}.{1}' is not available on new target object '{2}'",
            _propertyProvider, _propertyName, newTarget));
      return result;
    }

    public static bool CreateAttachedPropertyDataDescriptor(
        object targetObj, string propertyProvider, string propertyName,
        out MpfAttachedPropertyDataDescriptor result)
    {
      result = null;
      if (targetObj == null)
        throw new NullReferenceException("Target object 'null' is not supported");
      if (!MpfNamespaceHandler.HasAttachedProperty(propertyProvider, propertyName, targetObj))
        return false;
      result = new MpfAttachedPropertyDataDescriptor(targetObj, propertyProvider, propertyName);
      return true;
    }

    public override int GetHashCode()
    {
      return base.GetHashCode() + _propertyProvider.GetHashCode();
    }

    public override bool Equals(object other)
    {
      if (!(other is MpfAttachedPropertyDataDescriptor))
        return false;
      MpfAttachedPropertyDataDescriptor mapdd = (MpfAttachedPropertyDataDescriptor) other;
      return base.Equals(other) && _propertyProvider.Equals(mapdd._propertyProvider);
    }
  }
}
