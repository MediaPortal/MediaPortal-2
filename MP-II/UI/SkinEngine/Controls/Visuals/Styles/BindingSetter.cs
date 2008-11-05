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

using System;
using MediaPortal.Utilities.DeepCopy;
using MediaPortal.SkinEngine.MarkupExtensions;
using MediaPortal.SkinEngine.MpfElements;
using MediaPortal.SkinEngine.MpfElements.Resources;
using MediaPortal.SkinEngine.Xaml;

namespace MediaPortal.SkinEngine.Controls.Visuals.Styles
{
  public class BindingSetter : SetterBase
  {
    #region Protected fields

    protected BindingWrapper _bindingWrapper;

    #endregion

    #region Ctor

    public BindingSetter() { }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      base.DeepCopy(source, copyManager);
      BindingSetter bs = (BindingSetter) source;
      BindingWrapper = copyManager.GetCopy(bs.BindingWrapper);
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
      if (_bindingWrapper == null || _bindingWrapper.Binding == null)
        return;
      DependencyObject target = null;
      if (!string.IsNullOrEmpty(TargetName))
      {
        target = element.FindElement(new NameFinder(TargetName));
        if (target == null)
          return;
      }
      if (target == null)
        target = element;
      int index = Property.IndexOf('.');
      if (index != -1)
      {
        string propertyProvider = Property.Substring(0, index);
        string propertyName = Property.Substring(index + 1);
        MpfAttachedPropertyDataDescriptor targetDd;
        if (MpfAttachedPropertyDataDescriptor.CreateAttachedPropertyDataDescriptor(
            element, propertyProvider, propertyName, out targetDd))
        _bindingWrapper.Binding.CopyAndRetarget(targetDd);
      }
      else
      {
        string propertyName = Property;
        IDataDescriptor targetDd;
        if (ReflectionHelper.FindMemberDescriptor(target, propertyName, out targetDd))
          _bindingWrapper.Binding.CopyAndRetarget(targetDd);
      }
    }
  }
}
