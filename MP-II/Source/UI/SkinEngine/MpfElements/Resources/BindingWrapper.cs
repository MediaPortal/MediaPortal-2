#region Copyright (C) 2007-2009 Team MediaPortal

/*
    Copyright (C) 2007-2009 Team MediaPortal
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

using MediaPortal.Utilities.DeepCopy;
using MediaPortal.UI.SkinEngine.MarkupExtensions;
using MediaPortal.UI.SkinEngine.Xaml;
using MediaPortal.UI.SkinEngine.Xaml.Interfaces;

namespace MediaPortal.UI.SkinEngine.MpfElements.Resources
{
  /// <summary>
  /// Class to wrap a Binding instance. This is useful if a binding should be
  /// used as a template for a usage in another place. The binding can be accessed
  /// and copied by using the <see cref="PickupBindingMarkupExtension"/>.
  /// </summary>
  public class BindingWrapper : ValueWrapper
  {
    #region Protected fields

    protected bool _freezable = false;

    #endregion

    #region Ctor

    public BindingWrapper()
    { }

    public BindingWrapper(IBinding binding): base(binding)
    { }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      base.DeepCopy(source, copyManager);
      BindingWrapper rw = (BindingWrapper) source;
      Freezable = copyManager.GetCopy(rw.Freezable);
    }

    #endregion

    #region Public properties

    public IBinding Binding
    {
      get { return (IBinding) Value; }
      set { Value = value; }
    }

    public bool Freezable
    {
      get { return _freezable; }
      set { _freezable = value; }
    }

    #endregion

    #region Base overrides

    public override bool FindContentProperty(out IDataDescriptor dd)
    {
      dd = new SimplePropertyDataDescriptor(this, GetType().GetProperty("Binding"));
      return true;
    }

    #endregion
  }
}
