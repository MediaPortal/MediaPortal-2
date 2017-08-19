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
using System.Timers;
using MediaPortal.Common.General;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.Common.UserProfileDataManagement;
using MediaPortal.Common.Localization;
using MediaPortal.UiComponents.Login.General;
using System.Collections.Generic;

namespace MediaPortal.UiComponents.Login.Models
{
  /// <summary>
  /// Base data class which has two orthogonal jobs:
  /// 1) Collecting all user data during the user add or edit workflow and at the same time,
  /// 2) handling the communication with the local or server shares management.
  /// </summary>
  public class UserProxy : IDisposable
  {
    #region Protected fields

    protected List<Guid> _selectedSharesList;
    protected AbstractProperty _isUserValidProperty;
    protected AbstractProperty _profileTypeProperty;
    protected AbstractProperty _allowAllAgesProperty;
    protected AbstractProperty _allowAllSharesProperty;
    protected AbstractProperty _preferredMovieCertificationCountryProperty;
    protected AbstractProperty _preferredSeriesCertificationCountryProperty;
    protected AbstractProperty _allowedAgeProperty;
    protected AbstractProperty _includeParentGuidedAgesProperty;
    protected AbstractProperty _passwordProperty;
    protected AbstractProperty _userNameProperty;
    protected AbstractProperty _lastLoginProperty;
    protected AbstractProperty _imageProperty;
    protected AbstractProperty _idProperty;

    protected Timer _inputTimer;
    protected readonly object _syncObj = new object();

    #endregion

    public UserProxy()
    {
      _idProperty = new WProperty(typeof(Guid), Guid.Empty);
      _userNameProperty = new WProperty(typeof(string), string.Empty);
      _selectedSharesList = new List<Guid>();
      _isUserValidProperty = new WProperty(typeof(bool), false);
      _profileTypeProperty = new WProperty(typeof(int), UserProfile.USER_PROFILE);
      _allowAllAgesProperty = new WProperty(typeof(bool), true);
      _allowAllSharesProperty = new WProperty(typeof(bool), true);
      _allowedAgeProperty = new WProperty(typeof(int), 5);
      _passwordProperty = new WProperty(typeof(string), string.Empty);
      _includeParentGuidedAgesProperty = new WProperty(typeof(bool), false);
      _preferredMovieCertificationCountryProperty = new WProperty(typeof(string), string.Empty);
      _preferredSeriesCertificationCountryProperty = new WProperty(typeof(string), string.Empty);
      _lastLoginProperty = new WProperty(typeof(DateTime), DateTime.MinValue);
      _imageProperty = new WProperty(typeof(byte[]), null);

      _userNameProperty.Attach(OnUserChanged);
      _profileTypeProperty.Attach(OnUserChanged);
    }

    private void OnUserChanged(AbstractProperty property, object oldValue)
    {
      bool valid = true;
      valid &= !string.IsNullOrEmpty(UserName);
      valid &= ProfileType >= 0;
      IsUserValid = valid;
    }

    #region Public properties (can be used by the GUI)

    public List<Guid> SelectedShares
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

    public AbstractProperty IdProperty
    {
      get { return _idProperty; }
    }

    public Guid Id
    {
      get { return (Guid)_idProperty.GetValue(); }
      set { _idProperty.SetValue(value); }
    }

    public AbstractProperty UserNameProperty
    {
      get { return _userNameProperty; }
    }

    public string UserName
    {
      get { return (string)_userNameProperty.GetValue(); }
      set { _userNameProperty.SetValue(value); }
    }

    public AbstractProperty PasswordProperty
    {
      get { return _passwordProperty; }
    }

    public string Password
    {
      get { return (string)_passwordProperty.GetValue(); }
      set { _passwordProperty.SetValue(value); }
    }

    public AbstractProperty ProfileTypeProperty
    {
      get { return _profileTypeProperty; }
    }

    public int ProfileType
    {
      get { return (int)_profileTypeProperty.GetValue(); }
      set { _profileTypeProperty.SetValue(value); }
    }

    public AbstractProperty AllowAllAgesProperty
    {
      get { return _allowAllAgesProperty; }
    }

    public bool AllowAllAges
    {
      get { return (bool)_allowAllAgesProperty.GetValue(); }
      set { _allowAllAgesProperty.SetValue(value); }
    }

    public AbstractProperty AllowAllSharesProperty
    {
      get { return _allowAllSharesProperty; }
    }

    public bool AllowAllShares
    {
      get { return (bool)_allowAllSharesProperty.GetValue(); }
      set { _allowAllSharesProperty.SetValue(value); }
    }

    public AbstractProperty AllowedAgeProperty
    {
      get { return _allowedAgeProperty; }
    }

    public int AllowedAge
    {
      get { return (int)_allowedAgeProperty.GetValue(); }
      set { _allowedAgeProperty.SetValue(value); }
    }

    public AbstractProperty IncludeParentGuidedContentProperty
    {
      get { return _includeParentGuidedAgesProperty; }
    }

    public bool IncludeParentGuidedContent
    {
      get { return (bool)_includeParentGuidedAgesProperty.GetValue(); }
      set { _includeParentGuidedAgesProperty.SetValue(value); }
    }

    public AbstractProperty PreferredMovieCertificationCountryProperty
    {
      get { return _preferredMovieCertificationCountryProperty; }
    }

    public string PreferredMovieCertificationCountry
    {
      get { return (string)_preferredMovieCertificationCountryProperty.GetValue(); }
      set { _preferredMovieCertificationCountryProperty.SetValue(value); }
    }

    public AbstractProperty PreferredSeriesCertificationCountryProperty
    {
      get { return _preferredSeriesCertificationCountryProperty; }
    }

    public string PreferredSeriesCertificationCountry
    {
      get { return (string)_preferredSeriesCertificationCountryProperty.GetValue(); }
      set { _preferredSeriesCertificationCountryProperty.SetValue(value); }
    }

    public AbstractProperty LastLoginProperty
    {
      get { return _lastLoginProperty; }
    }

    public DateTime LastLogin
    {
      get { return (DateTime)_lastLoginProperty.GetValue(); }
      set { _lastLoginProperty.SetValue(value); }
    }

    public AbstractProperty ImageProperty
    {
      get { return _imageProperty; }
    }

    public byte[] Image
    {
      get { return (byte[])_imageProperty.GetValue(); }
      set { _imageProperty.SetValue(value); }
    }

    public void Dispose()
    {
    }

    #endregion
  }
}
