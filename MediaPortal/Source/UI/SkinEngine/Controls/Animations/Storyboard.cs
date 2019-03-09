#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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
using MediaPortal.UI.SkinEngine.MpfElements;

namespace MediaPortal.UI.SkinEngine.Controls.Animations
{
  public class Storyboard : ParallelTimeline
  {
    protected const string TARGET_NAME_ATTACHED_PROPERTY = "StoryBoard.TargetName";
    protected const string TARGET_PROPERTY_ATTACHED_PROPERTY = "StoryBoard.TargetProperty";

    #region Attached properties

    /// <summary>
    /// Getter method for the attached property <c>TargetName</c>.
    /// </summary>
    /// <param name="targetObject">The object whose property value will
    /// be returned.</param>
    /// <returns>Value of the <c>TargetName</c> property on the
    /// <paramref name="targetObject"/>.</returns>
    public static string GetTargetName(DependencyObject targetObject)
    {
      return targetObject.GetAttachedPropertyValue<string>(TARGET_NAME_ATTACHED_PROPERTY, null);
    }

    /// <summary>
    /// Setter method for the attached property <c>TargetName</c>.
    /// </summary>
    /// <param name="targetObject">The object whose property value will
    /// be set.</param>
    /// <param name="value">Value of the <c>TargetName</c> property on the
    /// <paramref name="targetObject"/> to be set.</param>
    public static void SetTargetName(DependencyObject targetObject, string value)
    {
      targetObject.SetAttachedPropertyValue<string>(TARGET_NAME_ATTACHED_PROPERTY, value);
    }

    /// <summary>
    /// Returns the <c>TargetName</c> attached property for the
    /// <paramref name="targetObject"/>. When this method is called,
    /// the property will be created if it is not yet attached to the
    /// <paramref name="targetObject"/>.
    /// </summary>
    /// <param name="targetObject">The object whose attached
    /// property should be returned.</param>
    /// <returns>Attached <c>TargetName</c> property.</returns>
    public static AbstractProperty GetTargetNameAttachedProperty(DependencyObject targetObject)
    {
      return targetObject.GetOrCreateAttachedProperty<string>(TARGET_NAME_ATTACHED_PROPERTY, null);
    }

    /// <summary>
    /// Getter method for the attached property <c>TargetProperty</c>.
    /// </summary>
    /// <param name="targetObject">The object whose property value will
    /// be returned.</param>
    /// <returns>Value of the <c>TargetProperty</c> property on the
    /// <paramref name="targetObject"/>.</returns>
    public static string GetTargetProperty(DependencyObject targetObject)
    {
      return targetObject.GetAttachedPropertyValue<string>(TARGET_PROPERTY_ATTACHED_PROPERTY, null);
    }

    /// <summary>
    /// Setter method for the attached property <c>TargetProperty</c>.
    /// </summary>
    /// <param name="targetObject">The object whose property value will
    /// be set.</param>
    /// <param name="value">Value of the <c>TargetProperty</c> property on the
    /// <paramref name="targetObject"/> to be set.</param>
    public static void SetTargetProperty(DependencyObject targetObject, string value)
    {
      targetObject.SetAttachedPropertyValue<string>(TARGET_PROPERTY_ATTACHED_PROPERTY, value);
    }

    /// <summary>
    /// Returns the <c>TargetProperty</c> attached property for the
    /// <paramref name="targetObject"/>. When this method is called,
    /// the property will be created if it is not yet attached to the
    /// <paramref name="targetObject"/>.
    /// </summary>
    /// <param name="targetObject">The object whose attached
    /// property should be returned.</param>
    /// <returns>Attached <c>TargetProperty</c> property.</returns>
    public static AbstractProperty GetTargetPropertyAttachedProperty(DependencyObject targetObject)
    {
      return targetObject.GetOrCreateAttachedProperty<string>(TARGET_PROPERTY_ATTACHED_PROPERTY, null);
    }

    #endregion
  }
}
