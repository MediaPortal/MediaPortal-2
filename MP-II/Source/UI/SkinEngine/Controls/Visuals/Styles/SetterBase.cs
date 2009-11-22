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

using MediaPortal.UI.SkinEngine.MpfElements;
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

    protected SetterBase() { }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      base.DeepCopy(source, copyManager);
      SetterBase sb = (SetterBase) source;
      TargetName = copyManager.GetCopy(sb.TargetName);
      Property = copyManager.GetCopy(sb.Property);
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

    #endregion

    /// <summary>
    /// Sets the setter's value to the target property.
    /// </summary>
    /// <param name="element">The UI element which is used as starting point for this setter
    /// to earch the target element.</param>
    public abstract void Set(UIElement element);
  }
}
