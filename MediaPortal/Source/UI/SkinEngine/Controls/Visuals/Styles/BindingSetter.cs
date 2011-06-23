#region Copyright (C) 2007-2011 Team MediaPortal

/*
    Copyright (C) 2007-2011 Team MediaPortal
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

using System;
using System.Collections.Generic;
using MediaPortal.Core.General;
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
      Registration.TryCleanupAndDispose(_bindingWrapper);
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

    protected DependencyObject FindTarget(UIElement startElement)
    {
      DependencyObject target = null;
      if (!string.IsNullOrEmpty(TargetName))
      {
        target = startElement.FindElement(new NameMatcher(TargetName));
        if (target == null)
          return null;
      }
      return target ?? startElement;
    }

    public override void Set(UIElement element)
    {
      if (_bindingWrapper == null || _bindingWrapper.Binding == null)
        return;
      DependencyObject target = FindTarget(element);
      if (target == null)
        return;
      AbstractProperty bindingsProperty = target.GetOrCreateAttachedProperty(BINDING_SETTER_BINDINGS, (IDictionary<BindingSetter, IBinding>) new Dictionary<BindingSetter, IBinding>());
      IDictionary<BindingSetter, IBinding> bindings = (IDictionary<BindingSetter, IBinding>) bindingsProperty.GetValue();
      if (bindings.ContainsKey(this))
        return;
      int index = Property.IndexOf('.');
      if (index != -1)
      {
        string propertyProvider = Property.Substring(0, index);
        string propertyName = Property.Substring(index + 1);
        MpfAttachedPropertyDataDescriptor targetDd;
        if (MpfAttachedPropertyDataDescriptor.CreateAttachedPropertyDataDescriptor(
            element, propertyProvider, propertyName, out targetDd))
        bindings.Add(this, _bindingWrapper.Binding.CopyAndRetarget(targetDd));
      }
      else
      {
        string propertyName = Property;
        IDataDescriptor targetDd;
        if (ReflectionHelper.FindMemberDescriptor(target, propertyName, out targetDd))
          bindings.Add(this, _bindingWrapper.Binding.CopyAndRetarget(targetDd));
      }
    }

    public override void Restore(UIElement element)
    {
      DependencyObject target = FindTarget(element);
      if (target == null)
        return;
      AbstractProperty bindingsProperty = target.GetOrCreateAttachedProperty(BINDING_SETTER_BINDINGS, (IDictionary<BindingSetter, IBinding>) new Dictionary<BindingSetter, IBinding>());
      IDictionary<BindingSetter, IBinding> bindings = (IDictionary<BindingSetter, IBinding>) bindingsProperty.GetValue();
      IBinding binding;
      if (!bindings.TryGetValue(this, out binding))
        return;
      IDisposable d = binding as IDisposable;
      if (d != null)
        d.Dispose(); // Also removes the binding from the binding collection in its context object
      bindings.Remove(this);
    }
  }
}
