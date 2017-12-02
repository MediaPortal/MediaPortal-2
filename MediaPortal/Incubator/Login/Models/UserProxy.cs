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
using MediaPortal.Common.UserProfileDataManagement;
using System.Collections.Generic;
using System.Linq;
using MediaPortal.Common.MediaManagement;
using MediaPortal.UiComponents.Login.General;
using MediaPortal.UI.Presentation.DataObjects;

namespace MediaPortal.UiComponents.Login.Models
{
  /// <summary>
  /// Base data class which has two orthogonal jobs:
  /// 1) Collecting all user data during the user add or edit workflow and at the same time,
  /// 2) handling the communication with the local or server shares management.
  /// </summary>
  public class UserProxy : ListItem, IDisposable
  {
    #region Protected fields

    protected List<Guid> _selectedSharesList;
    protected AbstractProperty _isUserValidProperty;
    protected AbstractProperty _profileTypeProperty;
    protected AbstractProperty _restrictAgesProperty;
    protected AbstractProperty _restrictSharesProperty;
    protected AbstractProperty _allowedAgeProperty;
    protected AbstractProperty _includeParentGuidedAgesProperty;
    protected AbstractProperty _includeUnratedProperty;
    protected AbstractProperty _passwordProperty;
    protected AbstractProperty _userNameProperty;
    protected AbstractProperty _lastLoginProperty;
    protected AbstractProperty _imageProperty;
    protected AbstractProperty _idProperty;

    protected Timer _inputTimer;
    protected readonly object _syncObj = new object();
    protected string _originalPassword = null;

    #endregion

    public UserProxy()
    {
      _idProperty = new WProperty(typeof(Guid), Guid.Empty);
      _userNameProperty = new WProperty(typeof(string), string.Empty);
      _selectedSharesList = new List<Guid>();
      _isUserValidProperty = new WProperty(typeof(bool), false);
      _profileTypeProperty = new WProperty(typeof(UserProfileType), UserProfileType.UserProfile);
      _restrictAgesProperty = new WProperty(typeof(bool), false);
      _restrictSharesProperty = new WProperty(typeof(bool), false);
      _allowedAgeProperty = new WProperty(typeof(int), 5);
      _passwordProperty = new WProperty(typeof(string), string.Empty);
      _includeParentGuidedAgesProperty = new WProperty(typeof(bool), false);
      _includeUnratedProperty = new WProperty(typeof(bool), false);
      _lastLoginProperty = new WProperty(typeof(DateTime), DateTime.MinValue);
      _imageProperty = new WProperty(typeof(byte[]), null);

      _userNameProperty.Attach(OnUserChanged);
      _profileTypeProperty.Attach(OnUserChanged);
    }

    public UserProxy(UserProfile userProfile)
    {
      SetUserProfile(userProfile);
    }

    public void SetUserProfile(UserProfile userProfile, ItemsList localSharesList = null, ItemsList serverSharesList = null)
    {
      Id = userProfile.ProfileId;
      Name = userProfile.Name;
      Password = userProfile.Password;
      _originalPassword = userProfile.Password;
      ProfileType = userProfile.ProfileType;
      LastLogin = userProfile.LastLogin ?? DateTime.MinValue;
      Image = userProfile.Image;

      SelectedShares.Clear();

      int allowedAge = 5;
      bool allowAllAges = true;
      bool allowAllShares = true;
      bool includeParentContent = false;
      bool includeUnratedContent = false;
      string preferredMovieCountry = string.Empty;
      string preferredSeriesCountry = string.Empty;

      foreach (var data in userProfile.AdditionalData)
      {
        foreach (var val in data.Value)
        {
          if (data.Key == UserDataKeysKnown.KEY_ALLOWED_AGE)
            allowedAge = Convert.ToInt32(val.Value);
          else if (data.Key == UserDataKeysKnown.KEY_ALLOW_ALL_AGES)
            allowAllAges = Convert.ToInt32(val.Value) > 0;
          else if (data.Key == UserDataKeysKnown.KEY_ALLOW_ALL_SHARES)
            allowAllShares = Convert.ToInt32(val.Value) > 0;
          else if (data.Key == UserDataKeysKnown.KEY_ALLOWED_SHARE)
          {
            Guid shareId = Guid.Parse(val.Value);
            if (localSharesList != null && localSharesList.Any(i => ((Share)i.AdditionalProperties[Consts.KEY_SHARE]).ShareId == shareId) ||
                serverSharesList != null && serverSharesList.Any(i => ((Share)i.AdditionalProperties[Consts.KEY_SHARE]).ShareId == shareId))
              SelectedShares.Add(shareId);
          }
          else if (data.Key == UserDataKeysKnown.KEY_INCLUDE_PARENT_GUIDED_CONTENT)
            includeParentContent = Convert.ToInt32(val.Value) > 0;
          else if (data.Key == UserDataKeysKnown.KEY_INCLUDE_UNRATED_CONTENT)
            includeUnratedContent = Convert.ToInt32(val.Value) > 0;
        }
      }

      RestrictAges = !allowAllAges;
      RestrictShares = !allowAllShares;
      AllowedAge = allowedAge;
      IncludeParentGuidedContent = includeParentContent;
      IncludeUnratedContent = includeUnratedContent;
    }

    public void Clear()
    {
      Id = Guid.Empty;
      Name = String.Empty;
      Password = String.Empty;
      ProfileType = UserProfileType.UserProfile;
      LastLogin = DateTime.MinValue;
      Image = null;

      SelectedShares.Clear();

      RestrictAges = false;
      RestrictShares = false;
      AllowedAge = 5;
      IncludeParentGuidedContent = false;
      IncludeUnratedContent = false;
    }

    private void OnUserChanged(AbstractProperty property, object oldValue)
    {
      bool valid = true;
      valid &= !string.IsNullOrEmpty(Name);
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

    public AbstractProperty NameProperty
    {
      get { return _userNameProperty; }
    }

    public string Name
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

    public UserProfileType ProfileType
    {
      get { return (UserProfileType)_profileTypeProperty.GetValue(); }
      set { _profileTypeProperty.SetValue(value); }
    }

    public AbstractProperty RestrictAgesProperty
    {
      get { return _restrictAgesProperty; }
    }

    public bool RestrictAges
    {
      get { return (bool)_restrictAgesProperty.GetValue(); }
      set { _restrictAgesProperty.SetValue(value); }
    }

    public AbstractProperty RestrictSharesProperty
    {
      get { return _restrictSharesProperty; }
    }

    public bool RestrictShares
    {
      get { return (bool)_restrictSharesProperty.GetValue(); }
      set { _restrictSharesProperty.SetValue(value); }
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

    public AbstractProperty IncludeUnratedContentProperty
    {
      get { return _includeUnratedProperty; }
    }

    public bool IncludeUnratedContent
    {
      get { return (bool)_includeUnratedProperty.GetValue(); }
      set { _includeUnratedProperty.SetValue(value); }
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

    public bool IsPasswordChanged
    {
      get
      {
        return _originalPassword != null ? !_originalPassword.Equals(Password) : Password != null;
      }
    }

    public void Dispose()
    {
    }

    #endregion
  }
}
