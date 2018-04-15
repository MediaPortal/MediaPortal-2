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
using MediaPortal.UI.Control.InputManager;
using MediaPortal.UI.SkinEngine.Commands;
using MediaPortal.UI.SkinEngine.InputManagement;
using MediaPortal.UI.SkinEngine.MpfElements;
using MediaPortal.Utilities.DeepCopy;

namespace MediaPortal.UI.SkinEngine.Controls.Visuals
{
  public class CheckBox : Button
  {
    #region Protected fields

    protected AbstractProperty _isCheckedProperty;
    protected AbstractProperty _checkedProperty;
    protected AbstractProperty _uncheckedProperty;

    #endregion

    #region Ctor

    public CheckBox()
    {
      Init();
    }

    void Init()
    {
      _isCheckedProperty = new SProperty(typeof(bool), false);
      _checkedProperty = new SProperty(typeof(IExecutableCommand), null);
      _uncheckedProperty = new SProperty(typeof(IExecutableCommand), null);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      base.DeepCopy(source, copyManager);
      CheckBox cb = (CheckBox) source;
      IsChecked = cb.IsChecked;
      Checked = copyManager.GetCopy(cb.Checked);
      Unchecked = copyManager.GetCopy(cb.Unchecked);
    }

    public override void Dispose()
    {
      MPF.TryCleanupAndDispose(Checked);
      MPF.TryCleanupAndDispose(Unchecked);
      base.Dispose();
    }

    #endregion

    public AbstractProperty IsCheckedProperty
    {
      get { return _isCheckedProperty; }
    }

    public bool IsChecked
    {
      get { return (bool) _isCheckedProperty.GetValue(); }
      set { _isCheckedProperty.SetValue(value); }
    }

    public AbstractProperty CheckedProperty
    {
      get { return _checkedProperty; }
    }

    public IExecutableCommand Checked
    {
      get { return (IExecutableCommand) _checkedProperty.GetValue(); }
      set { _checkedProperty.SetValue(value); }
    }

    public AbstractProperty UncheckedProperty
    {
      get { return _uncheckedProperty; }
    }

    public IExecutableCommand Unchecked
    {
      get { return (IExecutableCommand) _uncheckedProperty.GetValue(); }
      set { _uncheckedProperty.SetValue(value); }
    }

    internal override void OnKeyPreview(ref Key key)
    {
      bool checkedChanged = false;
      if (HasFocus && key == Key.Ok)
      {
        checkedChanged = true;
        IsChecked = !IsChecked; // First toggle the state, then execute the base handler
      }

      base.OnKeyPreview(ref key);
      if (checkedChanged)
      {
        key = Key.None;
        if (IsChecked)
        {
          IExecutableCommand cmd = Checked;
          if (cmd != null)
            InputManager.Instance.ExecuteCommand(cmd.Execute);
        }
        else
        {
          IExecutableCommand cmd = Unchecked;
          if (cmd != null)
            InputManager.Instance.ExecuteCommand(cmd.Execute);
        }
      }
    }
  }
}
