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

using System.Collections.Generic;
using MediaPortal.UI.SkinEngine.Xaml;
using MediaPortal.UI.SkinEngine.Xaml.Interfaces;
using MediaPortal.Utilities.DeepCopy;

namespace MediaPortal.UI.SkinEngine.MpfElements.Resources
{
  /// <summary>
  /// Class to wrap a value object which cannot directly be used. Objects of this class will NOT be
  /// automatically converted to the underlaying <see cref="BindingValue"/> object. That's why the code where
  /// instances of this class are used must explicitly support <see cref="LateBoundValue"/>s.
  /// </summary>
  /// <remarks>
  /// We don't derive <see cref="LateBoundValue"/> from <see cref="ValueWrapper"/> because we must avoid
  /// that the <see cref="BindingValue"/> property gets automatically copied in the <see cref="DeepCopy"/> method.
  /// </remarks>
  public class LateBoundValue : DependencyObject, IContentEnabled
  {
    #region Protected fields

    protected object _bindingValue = null;

    #endregion

    #region Ctor

    // We don't expose the LateBoundValue(object value) constructor to avoid the misusage
    // for {LateBoundValue {Binding ...}} (which cannot work, because the binding cannot bind
    // when used as a constructor parameter)

    public LateBoundValue() { }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      base.DeepCopy(source, copyManager);
      // Don't copy the BindingValue property because it is late bound and thus the actual value is not interesting
    }

    #endregion

    #region Public members

    public object BindingValue
    {
      get { return _bindingValue; }
      set { _bindingValue = value; }
    }

    public static IList<object> ConvertLateBoundValues(IEnumerable<object> parameters)
    {
      IList<object> result = new List<object>();
      foreach (object parameter in parameters)
        if (parameter is LateBoundValue)
          result.Add(((LateBoundValue) parameter).BindingValue);
        else
          result.Add(parameter);
      return result;
    }

    #endregion

    #region IContentEnabled implementation

    public virtual bool FindContentProperty(out IDataDescriptor dd)
    {
      dd = new SimplePropertyDataDescriptor(this, GetType().GetProperty("BindingValue"));
      return true;
    }

    #endregion
  }
}
