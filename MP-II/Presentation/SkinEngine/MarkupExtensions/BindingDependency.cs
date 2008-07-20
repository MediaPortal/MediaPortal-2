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

using Presentation.SkinEngine.Xaml;

namespace Presentation.SkinEngine.MarkupExtensions
{
  /// <summary>
  /// Handles the dependency between two data endpoints.
  /// </summary>
  public class BindingDependency
  {
    protected IDataDescriptor _sourceDd;
    protected IDataDescriptor _targetDd;
    protected bool _attachedToSource = false;
    protected bool _attachedToTarget = false;
    protected bool _negate = false;

    public BindingDependency(
        IDataDescriptor sourceDd, IDataDescriptor targetDd,
        bool autoAttachToSource, bool autoAttachToTarget, bool negate)
    {
      _sourceDd = sourceDd;
      _targetDd = targetDd;
      _negate = negate;
      if (autoAttachToSource && sourceDd.SupportsChangeNotification)
      {
        sourceDd.Attach(OnSourceChanged);
        _attachedToSource = true;
      }
      if (autoAttachToTarget && targetDd.SupportsChangeNotification)
      {
        targetDd.Attach(OnTargetChanged);
        _attachedToTarget = true;
      }
      // Initially update endpoints
      if (autoAttachToSource)
        UpdateTarget();
      if (autoAttachToTarget && !autoAttachToSource) // If we are attached to both, only update one direction
        UpdateSource();
    }

    protected void OnSourceChanged(IDataDescriptor source)
    {
      UpdateTarget();
    }

    protected void OnTargetChanged(IDataDescriptor target)
    {
      UpdateSource();
    }

    public void Detach()
    {
      if (_attachedToSource)
        _sourceDd.Detach(OnSourceChanged);
      _attachedToSource = false;
      if (_attachedToTarget)
        _targetDd.Detach(OnTargetChanged);
      _attachedToTarget = false;
    }

    public void UpdateSource()
    {
      object newValue;
      if (!TypeConverter.Convert(_targetDd.Value, _sourceDd.DataType, out newValue))
        return;
      if (_negate)
        newValue = !(bool)newValue;
      if (_sourceDd.Value == newValue)
        return;
      _sourceDd.Value = newValue;
    }

    public void UpdateTarget()
    {
      object newValue;
      if (!TypeConverter.Convert(_sourceDd.Value, _targetDd.DataType, out newValue))
        return;
      if (_negate)
        newValue = !(bool) newValue;
      if (_targetDd.Value == newValue)
        return;
      _targetDd.Value = newValue;
    }
  }
}
