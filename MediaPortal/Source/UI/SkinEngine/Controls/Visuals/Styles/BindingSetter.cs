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
using MediaPortal.Common.General;
using MediaPortal.UI.SkinEngine.Xaml.Interfaces;
using MediaPortal.Utilities.DeepCopy;
using MediaPortal.UI.SkinEngine.MpfElements;
using MediaPortal.UI.SkinEngine.MpfElements.Resources;
using MediaPortal.UI.SkinEngine.Xaml;

namespace MediaPortal.UI.SkinEngine.Controls.Visuals.Styles
{
  public class BindingSetter : SetterBase
  {
    #region Consts

    protected const string BINDING_SETTER_BINDINGS = "BindingSetter.Bindings";

    #endregion

    #region Protected fields

    protected BindingWrapper _bindingWrapper;

    #endregion

    #region Ctor

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      base.DeepCopy(source, copyManager);
      BindingSetter bs = (BindingSetter) source;
      BindingWrapper = copyManager.GetCopy(bs.BindingWrapper);
    }

    public override void Dispose()
    {
      MPF.TryCleanupAndDispose(_bindingWrapper);
      base.Dispose();
    }

    #endregion

    #region Properties

    public BindingWrapper BindingWrapper
    {
      get { return _bindingWrapper; }
      set { _bindingWrapper = value; }
    }

    #endregion

    public override void Set(UIElement element)
    {
      // We must activate our bindings to make dynamic resource markup extensions assigning our binding work, for example
      ActivateBindings();
      if (_bindingWrapper == null || _bindingWrapper.Binding == null)
        return;
      IDataDescriptor targetDd;
      DependencyObject targetObject;
      if (!FindPropertyDescriptor(element, out targetDd, out targetObject))
        return;
      AbstractProperty bindingsProperty = targetObject.GetOrCreateAttachedProperty(BINDING_SETTER_BINDINGS,
          (IDictionary<BindingSetter, IBinding>) new Dictionary<BindingSetter, IBinding>());
      IDictionary<BindingSetter, IBinding> bindings = (IDictionary<BindingSetter, IBinding>) bindingsProperty.GetValue();
      if (bindings.ContainsKey(this))
        return;
      bindings.Add(this, _bindingWrapper.Binding.CopyAndRetarget(targetDd));
    }

    public override void Restore(UIElement element)
    {
      IDataDescriptor targetDd;
      DependencyObject targetObject;
      if (!FindPropertyDescriptor(element, out targetDd, out targetObject))
        return;
      AbstractProperty bindingsProperty = targetObject.GetOrCreateAttachedProperty(BINDING_SETTER_BINDINGS,
          (IDictionary<BindingSetter, IBinding>) new Dictionary<BindingSetter, IBinding>());
      IDictionary<BindingSetter, IBinding> bindings = (IDictionary<BindingSetter, IBinding>) bindingsProperty.GetValue();
      IBinding binding;
      if (!bindings.TryGetValue(this, out binding))
        return;
      MPF.TryCleanupAndDispose(binding); // Also removes the binding from the binding collection in its context object
      bindings.Remove(this);
    }
  }
}
