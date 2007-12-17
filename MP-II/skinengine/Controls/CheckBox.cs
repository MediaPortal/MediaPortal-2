#region Copyright (C) 2007 Team MediaPortal

/*
    Copyright (C) 2007 Team MediaPortal
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

using MediaPortal.Core.InputManager;
using MediaPortal.Core.Properties;
using MediaPortal.Core.Commands;
using SkinEngine.Commands;


namespace SkinEngine.Controls
{
  public class CheckBox : Button
  {
    #region variables

    private Property _isSelected;
    private ICommand _onSelectedItemChangeCommand;

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="CheckBox"/> class.
    /// </summary>
    /// <param name="parent">The parent.</param>
    public CheckBox(Control parent)
      : base(parent)
    {
      _isSelected = new Property(false);
    }

    /// <summary>
    /// Gets or sets a value indicating whether this checkbox is selected.
    /// </summary>
    /// <value>
    /// 	<c>true</c> if this checkbox is selected; otherwise, <c>false</c>.
    /// </value>
    public bool IsSelected
    {
      get { return (bool)_isSelected.GetValue(); }
      set { _isSelected.SetValue(value); }
    }

    public Property IsSelectedProperty
    {
      get { return _isSelected; }
      set { _isSelected = value; }
    }

    /// <summary>
    /// handles any keypresses
    /// </summary>
    /// <param name="key">The key.</param>
    public override void OnKeyPressed(ref Key key)
    {
      if (!HasFocus)
      {
        return;
      }
      if (key == Key.Enter)
      {
        IsSelected = !IsSelected;
        UpdateSelectedItem();
        return;
      }
      base.OnKeyPressed(ref key);
    }

    /// <summary>
    /// Gets or sets the command to execute when the selected item has changed
    /// </summary>
    /// <value>The  command.</value>
    public ICommand OnSelectedItemChangeCommand
    {
      get
      {
        return _onSelectedItemChangeCommand;
      }
      set
      {
        _onSelectedItemChangeCommand = value;
      }
    }
    void UpdateSelectedItem()
    {
      if (_onSelectedItemChangeCommand != null)
      {
        _onSelectedItemChangeCommand.Execute(new StringParameter("this.IsSelected"));
      }
    }
  }
}