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

using MediaPortal.UI.SkinEngine.MpfElements.Resources;
using MediaPortal.UI.SkinEngine.Xaml;

namespace MediaPortal.UI.SkinEngine.MarkupExtensions
{

  /// <summary>
  /// Class to bind to a binding of another property rather than binding to a value.
  /// This class can be used, if a binding should be copied and
  /// bound to a target property.
  /// </summary>
  /// <remarks>
  /// The source value, this instance will reference, has to be of class
  /// <see cref="BindingWrapper"/>, else this markup extension won't copy any
  /// binding.<br/>
  /// The referenced binding wrapper is used to store the "template" of a binding
  /// which will be assigned to our target data descriptor by this instance.
  /// This binding will monitor the binding source value. As soon as we have a
  /// <see cref="BindingWrapper"/> accessible, the binding of it will be copyied and
  /// retargeted to our binding target, then this binding will be disposed.
  /// </remarks>
  public class PickupBindingMarkupExtension: BindingMarkupExtension
  {
    #region Ctor

    public PickupBindingMarkupExtension()
    { }

    public PickupBindingMarkupExtension(string path): base(path)
    { }

    #endregion

    #region Protected properties and methods

    protected override string BindingTypeName
    {
      get { return "PickupBinding"; }
    }

    protected override bool UpdateBinding()
    {
      _retryBinding = true;
      IDataDescriptor sourceDd;
      if (!Evaluate(out sourceDd))
        return false;
      BindingWrapper bindingWrapper = sourceDd.Value as BindingWrapper;
      if (bindingWrapper == null || bindingWrapper.Binding == null)
        return false;
      bindingWrapper.Binding.CopyAndRetarget(_targetDataDescriptor);
      // When the binding is copied, this instance is not needed any more
      _retryBinding = false;
      Dispose();
      return true;
    }

    #endregion

  }
}
