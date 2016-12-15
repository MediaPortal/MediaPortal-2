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
using System.Windows.Markup;
using MediaPortal.Common.General;
using MediaPortal.UI.SkinEngine.Xaml.Interfaces;
using MediaPortal.Utilities.DeepCopy;
using INameScope = MediaPortal.UI.SkinEngine.Xaml.Interfaces.INameScope;

namespace MediaPortal.UI.SkinEngine.MpfElements.Resources
{
  /// <summary>
  /// Class to wrap a value object which cannot directly be used. This may be the case if
  /// the object is resolved by a markup extension, for example. Instances of this class
  /// will be automatically converted to the underlaying <see cref="Resource"/> object.
  /// </summary>
  [ContentProperty("Resource")]
  public class ResourceWrapper : ValueWrapper, INameScope, IBindingContainer
  {
    #region Protected fields

    protected bool _enableBindings = false;
    protected IDictionary<string, object> _names = null;

    #endregion

    #region Ctor

    public ResourceWrapper() { }

    public ResourceWrapper(object resource): base(resource) { }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      base.DeepCopy(source, copyManager);
      ResourceWrapper rw = (ResourceWrapper) source;
      if (rw._names == null)
        _names = null;
      else
      {
        _names = new Dictionary<string, object>(rw._names.Count);
        foreach (KeyValuePair<string, object> kvp in rw._names)
          if (_names.ContainsKey(kvp.Key))
            continue;
          else
            _names.Add(kvp.Key, copyManager.GetCopy(kvp.Value));
      }
    }

    #endregion

    #region Protected methods

    protected INameScope FindParentNamescope()
    {
      DependencyObject current = LogicalParent;
      while (current != null)
      {
        if (current is INameScope)
          return (INameScope) current;
        current = current.LogicalParent;
      }
      return null;
    }

    protected IDictionary<string, object> GetOrCreateNames()
    {
      return _names ?? (_names = new Dictionary<string, object>());
    }

    #endregion

    #region Public properties

    public AbstractProperty ResourceProperty
    {
      get { return ValueProperty; }
    }

    public object Resource
    {
      get { return Value; }
      set { Value = value; }
    }

    public bool EnableBindings
    {
      get { return _enableBindings; }
      set
      {
        _enableBindings = value;
        if (_enableBindings)
          ActivateBindings();
      }
    }

    #endregion

    #region INameScope implementation

    public object FindName(string name)
    {
      object obj;
      if (_names != null && _names.TryGetValue(name, out obj))
        return obj;
      INameScope parent = FindParentNamescope();
      if (parent != null)
        return parent.FindName(name);
      return null;
    }

    public void RegisterName(string name, object instance)
    {
      IDictionary<string, object> names = GetOrCreateNames();
      object old;
      if (names.TryGetValue(name, out old) && ReferenceEquals(old, instance))
        return;
      names.Add(name, instance);
    }

    public void UnregisterName(string name)
    {
      if (_names == null)
        return;
      _names.Remove(name);
    }

    #endregion

    #region IBindingContainer implementation

    void IBindingContainer.AddBindings(IEnumerable<IBinding> bindings)
    {
      if (_enableBindings)
      {
        foreach (IBinding binding in bindings)
          binding.Activate();
        return;
      }
      foreach (IBinding binding in bindings)
        AddDeferredBinding(binding);
    }

    #endregion
  }
}
