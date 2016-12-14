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

using System.Collections.Generic;
using MediaPortal.UI.SkinEngine.MpfElements;
using MediaPortal.Utilities.DeepCopy;
using MediaPortal.UI.SkinEngine.Xaml.Interfaces;

namespace MediaPortal.UI.SkinEngine.Xaml
{
  public class NameScope: INameScope, IDeepCopyable, ISkinEngineManagedObject
  {
    protected IDictionary<string, object> _names = new Dictionary<string, object>();

    public virtual void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      NameScope ns = (NameScope) source;
      foreach (KeyValuePair<string, object> kvp in ns._names)
        if (_names.ContainsKey(kvp.Key))
          continue;
        else
          _names.Add(kvp.Key, copyManager.GetCopy(kvp.Value));
    }

    #region INamescope implementation

    public object FindName(string name)
    {
      object obj;
      if (_names.TryGetValue(name, out obj))
        return obj;
      return null;
    }

    public void RegisterName(string name, object instance)
    {
      object old;
      if (_names.TryGetValue(name, out old) && ReferenceEquals(old, instance))
        return;
      _names.Add(name, instance);
    }

    public void UnregisterName(string name)
    {
      _names.Remove(name);
    }

    #endregion
  }
}
