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

using System.Collections.Generic;
using Presentation.SkinEngine.Controls;
using Presentation.SkinEngine.Xaml;
using Presentation.SkinEngine.Xaml.Interfaces;
using MediaPortal.Utilities.DeepCopy;

namespace Presentation.SkinEngine.MpfElements.Resources
{
  /// <summary>
  /// Class to wrap a value object which cannot directly be used. This may be the case if
  /// the object is resolved by a markup extension, for example.
  /// </summary>
  public class ValueWrapper : DependencyObject, INameScope, IContentEnabled, IDeepCopyable
  {
    #region Protected fields

    protected object _value = null;
    protected IDictionary<string, object> _names = new Dictionary<string, object>();
    protected INameScope _parent = null;

    #endregion

    #region Ctor

    public ValueWrapper() { }

    public ValueWrapper(object value)
    {
      _value = value;
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      base.DeepCopy(source, copyManager);
      ValueWrapper vw = (ValueWrapper) source;
      Value = copyManager.GetCopy(vw.Value);
      _parent = copyManager.GetCopy(vw._parent);
      foreach (KeyValuePair<string, object> kvp in vw._names)
        _names.Add(copyManager.GetCopy(kvp.Key), copyManager.GetCopy(kvp.Value));
    }

    #endregion

    #region Public properties

    public object Value
    {
      get { return _value; }
      set { _value = value; }
    }

    #endregion

    #region IContentEnabled implementation

    public virtual bool FindContentProperty(out IDataDescriptor dd)
    {
      dd = new SimplePropertyDataDescriptor(this, GetType().GetProperty("Value"));
      return true;
    }

    #endregion

    #region INameScope implementation

    public object FindName(string name)
    {
      if (_names.ContainsKey(name))
        return _names[name];
      else if (_parent != null)
        return _parent.FindName(name);
      else
        return null;
    }

    public void RegisterName(string name, object instance)
    {
      _names.Add(name, instance);
    }

    public void UnregisterName(string name)
    {
      _names.Remove(name);
    }

    public void RegisterParent(INameScope parent)
    {
      _parent = parent;
    }

    #endregion
  }
}
