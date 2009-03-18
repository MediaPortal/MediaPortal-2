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

using MediaPortal.SkinEngine.Xaml;
using MediaPortal.SkinEngine.Xaml.Interfaces;
using MediaPortal.Utilities.DeepCopy;

namespace MediaPortal.SkinEngine.MpfElements.Resources
{
  /// <summary>
  /// Class to wrap a value object which cannot directly be used. This may be the case if
  /// the object is resolved by a markup extension, for example.
  /// </summary>
  public class ValueWrapper : DependencyObject, IContentEnabled, IDeepCopyable
  {
    #region Protected fields

    protected object _value = null;

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
  }
}
