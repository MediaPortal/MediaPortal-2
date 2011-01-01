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

using MediaPortal.Core.General;
using MediaPortal.UI.SkinEngine.MpfElements;
using MediaPortal.Utilities.DeepCopy;
using SlimDX;

namespace MediaPortal.UI.SkinEngine.Controls.Transforms
{
  public class Transform : DependencyObject, IObservable
  {
    #region Private/protected fields

    protected bool _needUpdate = true;
    protected Matrix _matrix = Matrix.Identity;

    #endregion

    #region Ctor

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      base.DeepCopy(source, copyManager);
      _needUpdate = true;
    }

    #endregion

    public event ObjectChangedHandler ObjectChanged;

    #region Protected methods

    protected void OnPropertyChanged(AbstractProperty property, object oldValue)
    {
      _needUpdate = true;
      Fire();
    }

    #endregion

    protected void Fire()
    {
      if (ObjectChanged != null)
        ObjectChanged(this);
    }

    public virtual Matrix GetTransform()
    {
      if (_needUpdate)
      {
        UpdateTransform();
        _needUpdate = false;
      }
      return _matrix;
    }

    public virtual void UpdateTransform()
    { }
  }
}
