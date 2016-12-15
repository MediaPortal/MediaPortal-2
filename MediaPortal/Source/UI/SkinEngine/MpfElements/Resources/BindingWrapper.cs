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

using MediaPortal.Common.General;
using MediaPortal.Utilities.DeepCopy;
using MediaPortal.UI.SkinEngine.MarkupExtensions;
using MediaPortal.UI.SkinEngine.Xaml;
using MediaPortal.UI.SkinEngine.Xaml.Interfaces;

namespace MediaPortal.UI.SkinEngine.MpfElements.Resources
{
  /// <summary>
  /// Class to wrap a Binding instance. This is useful if a binding should be
  /// used as a template for a usage in another place. The binding can be accessed
  /// and copied by using the <see cref="PickupBindingExtension"/>.
  /// </summary>
  public class BindingWrapper : DependencyObject, IContentEnabled
  {
    #region Protected fields

    protected SProperty _bindingProperty = new SProperty(typeof(IBinding), null);

    #endregion

    #region Ctor

    public BindingWrapper()
    { }

    public BindingWrapper(IBinding binding)
    {
      Binding = binding;
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      base.DeepCopy(source, copyManager);
      BindingWrapper bw = (BindingWrapper) source;
      Binding = copyManager.GetCopy(bw.Binding);
    }

    #endregion

    #region Public properties

    public AbstractProperty BindingProperty
    {
      get { return _bindingProperty; }
    }

    public IBinding Binding
    {
      get { return (IBinding) _bindingProperty.GetValue(); }
      set { _bindingProperty.SetValue(value); }
    }

    #endregion

    #region Base overrides

    public bool FindContentProperty(out IDataDescriptor dd)
    {
      dd = new SimplePropertyDataDescriptor(this, GetType().GetProperty("Binding"));
      return true;
    }

    #endregion
  }
}
