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

using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.UI.SkinEngine.MpfElements;
using MediaPortal.UI.SkinEngine.Xaml;
using MediaPortal.Utilities.DeepCopy;

namespace MediaPortal.UI.SkinEngine.Controls.Visuals.Styles
{
  public abstract class SetterBase : DependencyObject
  {
    #region Protected fields

    protected string _targetName;
    protected string _propertyName;

    #endregion

    #region Ctor

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      base.DeepCopy(source, copyManager);
      SetterBase sb = (SetterBase) source;
      TargetName = sb.TargetName;
      Property = sb.Property;
    }

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets the name of the property to be set by this <see cref="Setter"/>.
    /// </summary>
    public string Property
    {
      get { return _propertyName; }
      set { _propertyName = value; }
    }

    /// <summary>
    /// Gets or sets the name of the target element where this setter will search
    /// the <see cref="Property"/> to be set.
    /// </summary>
    public string TargetName
    {
      get { return _targetName; }
      set { _targetName = value; }
    }

    /// <summary>
    /// Unique name for this setter in a parent style's name scope for a given target element.
    /// </summary>
    internal string UnambiguousPropertyName
    {
      get { return _targetName + "." + _propertyName; }
    }

    #endregion

    /// <summary>
    /// Sets the setter's value to the target property.
    /// </summary>
    /// <param name="element">The UI element which is used as starting point for this setter
    /// to earch the target element.</param>
    public abstract void Set(UIElement element);

    /// <summary>
    /// Restore the target element's original value.
    /// </summary>
    /// <param name="element">The UI element which is used as starting point for this setter
    /// to reach the target element.</param>
    public abstract void Restore(UIElement element);

    protected bool FindPropertyDescriptor(UIElement element,
        out IDataDescriptor propertyDescriptor, out DependencyObject targetObject)
    {
      string targetName = TargetName;
      propertyDescriptor = null;
      if (string.IsNullOrEmpty(targetName))
        targetObject = element;
      else
      {
        // Search the element in "normal" namescope and in the dynamic structure via the FindElement method
        // I think this is more than WPF does. It makes it possible to find elements instantiated
        // by a template, for example.
        targetObject = element.FindElementInNamescope(targetName) ??
            element.FindElement(new NameMatcher(targetName));
        if (targetObject == null)
          return false;
      }
      string property = Property;
      int index = property.IndexOf('.');
      if (index != -1)
      {
        string propertyProvider = property.Substring(0, index);
        string propertyName = property.Substring(index + 1);
        DefaultAttachedPropertyDataDescriptor result;
        if (!DefaultAttachedPropertyDataDescriptor.CreateAttachedPropertyDataDescriptor(new MpfNamespaceHandler(),
            element, propertyProvider, propertyName, out result))
        {
          ServiceRegistration.Get<ILogger>().Warn(
              string.Format("Attached property '{0}' cannot be set on element '{1}'", property, targetObject));
          return false;
        }
        propertyDescriptor = result;
        return true;
      }
      else
      {
        string propertyName = property;
        IDataDescriptor result;
        if (!ReflectionHelper.FindMemberDescriptor(targetObject, propertyName, out result))
        {
          ServiceRegistration.Get<ILogger>().Warn(
              string.Format("Property '{0}' cannot be set on element '{1}'", property, targetObject));
          return false;
        }
        propertyDescriptor = result;
        return true;
      }
    }

    #region Base overrides

    public override string ToString()
    {
      return "Setter: Property='" + Property + "', TargetName='" + TargetName + "'";
    }

    #endregion
  }
}
