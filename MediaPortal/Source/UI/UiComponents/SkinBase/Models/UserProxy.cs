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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using MediaPortal.Common.General;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UiComponents.SkinBase.General;
using MediaPortal.Utilities;
using MediaPortal.Common.UserProfileDataManagement;

namespace MediaPortal.UiComponents.SkinBase.Models
{
  /// <summary>
  /// Base data class which has two orthogonal jobs:
  /// 1) Collecting all user data during the user add or edit workflow and at the same time,
  /// 2) handling the communication with the local or server shares management.
  /// </summary>
  public abstract class UserProxy : IDisposable
  {
    #region Enums

    public enum UserEditMode
    {
      AddUser,
      EditUser,
    }

    #endregion

    #region Protected fields

    protected UserEditMode? _editMode;
    protected ItemsList _selectedSharesList;
    protected AbstractProperty _isUserValidProperty;
    protected AbstractProperty _profileTypeProperty;
    protected AbstractProperty _allAgesFilterProperty;
    protected AbstractProperty _ageCountryFilterProperty;
    protected AbstractProperty _ageFilterProperty;
    protected AbstractProperty _includeParentGuidedAgesProperty;
    protected AbstractProperty _passwordProperty;
    protected AbstractProperty _selectedShareCountProperty;

    protected Timer _inputTimer;
    protected readonly object _syncObj = new object();

    #endregion

    protected UserProxy(UserEditMode? editMode)
    {
      _editMode = editMode;
      _selectedSharesList = new ItemsList();
      _isUserValidProperty = new WProperty(typeof(bool), false);
      _profileTypeProperty = new WProperty(typeof(int), UserProfile.USER_PROFILE);
      _allAgesFilterProperty = new WProperty(typeof(bool), true);
      _ageFilterProperty = new WProperty(typeof(int), 5);
      _passwordProperty = new WProperty(typeof(string), string.Empty);
      _includeParentGuidedAgesProperty = new WProperty(typeof(bool), false);
      _ageCountryFilterProperty = new WProperty(typeof(string), string.Empty);
      _selectedShareCountProperty = new WProperty(typeof(int), _selectedSharesList.Count);
    }

    #region Public properties (can be used by the GUI)

    public UserEditMode? EditMode
    {
      get { return _editMode; }
      set { _editMode = value; }
    }

    /// <summary>
    /// Returns the appropriate title for the whole share add or edit workflow, for example
    /// <c>[SharesConfig.AddServerShare]</c>, which could evaluate to the string <c>Add server share</c>, depending
    /// on the configured language.
    /// </summary>
    public abstract string ConfigUserTitle { get; }

    public ItemsList SelectedShares
    {
      get { return _selectedSharesList; }
    }

    public AbstractProperty IsUserValidProperty
    {
      get { return _isUserValidProperty; }
    }

    public bool IsUserValid
    {
      get { return (bool)_isUserValidProperty.GetValue(); }
      set { _isUserValidProperty.SetValue(value); }
    }

    public void Dispose()
    {
      
    }

    #endregion
  }
}
