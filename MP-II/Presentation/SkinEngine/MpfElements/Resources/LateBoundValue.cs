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
using Presentation.SkinEngine.General;
using Presentation.SkinEngine.XamlParser.Interfaces;
using MediaPortal.Utilities.DeepCopy;

namespace Presentation.SkinEngine.MpfElements.Resources
{
  /// <summary>
  /// Class to wrap a value object which cannot directly be used. This may be the case if
  /// the object is resolved by a markup extension, for example. Objects of this class will NOT be
  /// automatically converted to the underlaying <see cref="Value"/> object. That's why the code where
  /// instances of this class are used must explicitly support <see cref="LateBoundValue"/>s.
  /// </summary>
  public class LateBoundValue : DependencyObject, INameScope, IContentEnabled, IDeepCopyable
  {
    #region Protected fields

    protected object _value = null;
    protected IDictionary<string, object> _names = new Dictionary<string, object>();
    protected INameScope _parent = null;

    #endregion

    #region Ctor

    public LateBoundValue()
    { }

    public LateBoundValue(object value)
    {
      _value = value;
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      base.DeepCopy(source, copyManager);
      LateBoundValue lbv = (LateBoundValue) source;
      Value = copyManager.GetCopy(lbv.Value);
    }

    #endregion

    public static IList<object> ConvertLateBoundValues(IEnumerable<object> parameters)
    {
      IList<object> result = new List<object>();
      foreach (object parameter in parameters)
        if (parameter is LateBoundValue)
          result.Add(((LateBoundValue) parameter).Value);
        else
          result.Add(parameter);
      return result;
    }

    #region Public properties

    public object Value
    {
      get { return _value; }
      set { _value = value; }
    }

    #endregion

    #region IContentEnabled implementation

    public bool FindContentProperty(out IDataDescriptor dd)
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
