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
using MediaPortal.Common;
using MediaPortal.Common.Exceptions;
using MediaPortal.Common.General;
using MediaPortal.Common.Logging;
using MediaPortal.Common.Messaging;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Models;
using MediaPortal.UI.Presentation.Screens;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UI.ServerCommunication;
using MediaPortal.UiComponents.Login.General;
using MediaPortal.Common.UserProfileDataManagement;
using MediaPortal.Common.Localization;
using System.Threading.Tasks;
using MediaPortal.Common.UserManagement;
using System.Globalization;
using MediaPortal.Common.Settings;

namespace MediaPortal.UiComponents.Login.Models
{
  /// <summary>
  /// Provides a workflow model to attend the complex configuration process for server and client shares
  /// in the MP2 configuration.
  /// </summary>
  public class UserLanguageModel : IWorkflowModel, IDisposable
  {
    internal class AccessCheck : IUserRestriction
    {
      public string RestrictionGroup { get; set; }
    }

    protected enum LanguageMode
    {
      LangAudioMain,
      LangAudioSecondary,
      LangSubtitleMain,
      LangSubtitleSecondary,
      LangMenuMain,
      LangMenuSecondary,
    }

    #region Consts

    public const string STR_MODEL_ID_USERLANGUAGE = "E26A7613-4B9C-4B84-A982-A7B69D83DC3A";
    public static readonly Guid MODEL_ID_USERLANGUAGE = new Guid(STR_MODEL_ID_USERLANGUAGE);

    #endregion

    #region Protected fields

    protected object _syncObj = new object();
    protected bool _updatingProperties = false;
    protected LanguageMode _languageMode = LanguageMode.LangAudioMain;
    protected ItemsList _userList = null;
    protected ItemsList _langaugeList = null;
    protected UserProxy _userProxy = null; // Encapsulates state and communication of user configuration
    protected AbstractProperty _isHomeServerConnectedProperty;
    protected AbstractProperty _isRestrictedToOwnProperty;
    protected AbstractProperty _isUserSelectedProperty;
    protected AsynchronousMessageQueue _messageQueue = null;

    #endregion

    #region Ctor

    public UserLanguageModel()
    {
      _isHomeServerConnectedProperty = new WProperty(typeof(bool), false);
      _isRestrictedToOwnProperty = new WProperty(typeof(bool), false);
      _isUserSelectedProperty = new WProperty(typeof(bool), false);

      List<CultureInfo> cultures = new List<CultureInfo>(CultureInfo.GetCultures(CultureTypes.SpecificCultures));
      cultures.Sort(CompareByName);

      _langaugeList = new ItemsList();
      ListItem item = new ListItem();
      item.SetLabel(Consts.KEY_NAME, LocalizationHelper.CreateResourceString("[UserConfig.Default]"));
      item.AdditionalProperties[Consts.KEY_LANGUAGE] = "";
      _langaugeList.Add(item);
      foreach (var ci in cultures)
      {
        item = new ListItem();
        item.SetLabel(Consts.KEY_NAME, LocalizationHelper.CreateStaticString(ci.DisplayName));
        item.AdditionalProperties[Consts.KEY_LANGUAGE] = ci;
        _langaugeList.Add(item);
      }

      UserProxy = new UserProxy();
    }

    protected static int CompareByName(CultureInfo culture1, CultureInfo culture2)
    {
      return string.Compare(culture1.DisplayName, culture2.DisplayName);
    }

    public void Dispose()
    {
      UserProxy = null;
      _userList = null;
      _langaugeList = null;
    }

    #endregion

    void SubscribeToMessages()
    {
      AsynchronousMessageQueue messageQueue = new AsynchronousMessageQueue(this, new string[]
        {
           ServerConnectionMessaging.CHANNEL,
        });
      messageQueue.MessageReceived += OnMessageReceived;
      messageQueue.Start();
      lock (_syncObj)
        _messageQueue = messageQueue;
    }

    void UnsubscribeFromMessages()
    {
      AsynchronousMessageQueue messageQueue;
      lock (_syncObj)
      {
        messageQueue = _messageQueue;
        _messageQueue = null;
      }
      if (messageQueue == null)
        return;
      messageQueue.Shutdown();
    }

    void OnMessageReceived(AsynchronousMessageQueue queue, SystemMessage message)
    {
      if (message.ChannelName == ServerConnectionMessaging.CHANNEL)
      {
        ServerConnectionMessaging.MessageType messageType =
            (ServerConnectionMessaging.MessageType)message.MessageType;
        switch (messageType)
        {
          case ServerConnectionMessaging.MessageType.HomeServerAttached:
          case ServerConnectionMessaging.MessageType.HomeServerDetached:
          case ServerConnectionMessaging.MessageType.HomeServerConnected:
          case ServerConnectionMessaging.MessageType.HomeServerDisconnected:
            _ = UpdateUserLists_NoLock(false);
            break;
        }
      }
    }

    #region Public properties (Also accessed from the GUI)

    public UserProxy UserProxy
    {
      get { return _userProxy; }
      private set
      {
        lock (_syncObj)
        {
          if (_userProxy != null)
            _userProxy.Dispose();
          _userProxy = value;
        }
      }
    }

    public ItemsList LanguageList
    {
      get
      {
        lock (_syncObj)
          return _langaugeList;
      }
    }

    public ItemsList UserList
    {
      get
      {
        lock (_syncObj)
          return _userList;
      }
    }

    public AbstractProperty IsHomeServerConnectedProperty
    {
      get { return _isHomeServerConnectedProperty; }
    }

    public bool IsHomeServerConnected
    {
      get { return (bool)_isHomeServerConnectedProperty.GetValue(); }
      set { _isHomeServerConnectedProperty.SetValue(value); }
    }

    public AbstractProperty IsUserSelectedProperty
    {
      get { return _isUserSelectedProperty; }
    }

    public bool IsUserSelected
    {
      get { return (bool)_isUserSelectedProperty.GetValue(); }
      set { _isUserSelectedProperty.SetValue(value); }
    }

    public AbstractProperty IsRestrictedToOwnProperty
    {
      get { return _isRestrictedToOwnProperty; }
    }

    public bool IsRestrictedToOwn
    {
      get { return (bool)_isRestrictedToOwnProperty.GetValue(); }
      set { _isRestrictedToOwnProperty.SetValue(value); }
    }

    #endregion

    #region Public methods

    public void OpenAudioMainLanguageDialog()
    {
      _languageMode = LanguageMode.LangAudioMain;
      ServiceRegistration.Get<IScreenManager>().ShowDialog("DialogSelectLanguage");
    }

    public void OpenAudioSecondaryLanguageDialog()
    {
      _languageMode = LanguageMode.LangAudioSecondary;
      ServiceRegistration.Get<IScreenManager>().ShowDialog("DialogSelectLanguage");
    }

    public void OpenSubtitleMainLanguageDialog()
    {
      _languageMode = LanguageMode.LangSubtitleMain;
      ServiceRegistration.Get<IScreenManager>().ShowDialog("DialogSelectLanguage");
    }

    public void OpenSubtitleSecondaryLanguageDialog()
    {
      _languageMode = LanguageMode.LangSubtitleSecondary;
      ServiceRegistration.Get<IScreenManager>().ShowDialog("DialogSelectLanguage");
    }

    public void OpenMenuMainLanguageDialog()
    {
      _languageMode = LanguageMode.LangMenuMain;
      ServiceRegistration.Get<IScreenManager>().ShowDialog("DialogSelectLanguage");
    }

    public void OpenMenuSecondaryLanguageDialog()
    {
      _languageMode = LanguageMode.LangMenuSecondary;
      ServiceRegistration.Get<IScreenManager>().ShowDialog("DialogSelectLanguage");
    }

    public void SelectLanguage(ListItem item)
    {
      CultureInfo culture = (CultureInfo)item.AdditionalProperties[Consts.KEY_LANGUAGE];
      if (UserProxy != null)
      {
        switch (_languageMode)
        {
          case LanguageMode.LangAudioMain:
            UserProxy.LanguageAudioMain = culture?.DisplayName;
            break;
          case LanguageMode.LangAudioSecondary:
            UserProxy.LanguageAudioSecondary = culture?.DisplayName;
            break;
          case LanguageMode.LangSubtitleMain:
            UserProxy.LanguageSubtitleMain = culture?.DisplayName;
            break;
          case LanguageMode.LangSubtitleSecondary:
            UserProxy.LanguageSubtitleSecondary = culture?.DisplayName;
            break;
          case LanguageMode.LangMenuMain:
            UserProxy.LanguageMenuMain = culture?.DisplayName;
            break;
          case LanguageMode.LangMenuSecondary:
            UserProxy.LanguageMenuSecondary = culture?.DisplayName;
            break;
        }
      }
    }

    public async Task SaveUser()
    {
      try
      {
        if (UserProxy.IsUserValid)
        {
          bool success = true;
          Dictionary<string, Dictionary<int, string>> langData = new Dictionary<string, Dictionary<int, string>>();
          IUserManagement userManagement = ServiceRegistration.Get<IUserManagement>();
          var userId = UserProxy.Id;
          if (userManagement.UserProfileDataManagement != null)
          {
            // If the current user is restricted to own profile, we skip all properties that would allow a "self unrestriction"
            if (!IsRestrictedToOwn)
            {
              RegionSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<RegionSettings>();
              var cultures = CultureInfo.GetCultures(CultureTypes.AllCultures);
              var culture = cultures.FirstOrDefault(c => c.DisplayName == UserProxy.LanguageAudioMain);
              langData.Add(UserDataKeysKnown.KEY_PREFERRED_AUDIO_LANGUAGE, new Dictionary<int, string>());
              langData[UserDataKeysKnown.KEY_PREFERRED_AUDIO_LANGUAGE].Add(0, string.IsNullOrEmpty(culture?.TwoLetterISOLanguageName) ? new CultureInfo(settings.Culture).TwoLetterISOLanguageName : culture.TwoLetterISOLanguageName);
              culture = cultures.FirstOrDefault(c => c.DisplayName == UserProxy.LanguageAudioSecondary);
              langData[UserDataKeysKnown.KEY_PREFERRED_AUDIO_LANGUAGE].Add(1, culture?.TwoLetterISOLanguageName ?? "");

              culture = cultures.FirstOrDefault(c => c.DisplayName == UserProxy.LanguageSubtitleMain);
              langData.Add(UserDataKeysKnown.KEY_PREFERRED_SUBTITLE_LANGUAGE, new Dictionary<int, string>());
              langData[UserDataKeysKnown.KEY_PREFERRED_SUBTITLE_LANGUAGE].Add(0, string.IsNullOrEmpty(culture?.TwoLetterISOLanguageName) ? new CultureInfo(settings.Culture).TwoLetterISOLanguageName : culture.TwoLetterISOLanguageName);
              culture = cultures.FirstOrDefault(c => c.DisplayName == UserProxy.LanguageSubtitleSecondary);
              langData[UserDataKeysKnown.KEY_PREFERRED_SUBTITLE_LANGUAGE].Add(1, culture?.TwoLetterISOLanguageName ?? "");

              culture = cultures.FirstOrDefault(c => c.DisplayName == UserProxy.LanguageMenuMain);
              langData.Add(UserDataKeysKnown.KEY_PREFERRED_MENU_LANGUAGE, new Dictionary<int, string>());
              langData[UserDataKeysKnown.KEY_PREFERRED_MENU_LANGUAGE].Add(0, string.IsNullOrEmpty(culture?.TwoLetterISOLanguageName) ? new CultureInfo(settings.Culture).TwoLetterISOLanguageName : culture.TwoLetterISOLanguageName);
              success &= await userManagement.UserProfileDataManagement.SetUserAdditionalDataAsync(userId, UserDataKeysKnown.KEY_PREFERRED_MENU_LANGUAGE, culture?.TwoLetterISOLanguageName, 0);
              culture = cultures.FirstOrDefault(c => c.DisplayName == UserProxy.LanguageMenuSecondary);
              langData[UserDataKeysKnown.KEY_PREFERRED_MENU_LANGUAGE].Add(1, culture?.TwoLetterISOLanguageName ?? "");

              foreach(var dataKey in langData.Keys)
              {
                foreach(var data in langData[dataKey])
                {
                  success &= await userManagement.UserProfileDataManagement.SetUserAdditionalDataAsync(userId, dataKey, data.Value, data.Key);
                }
              }
            }

            if (!success)
            {
              ServiceRegistration.Get<ILogger>().Error("UserLanguageModel: Problems saving languages for user '{0}'", UserProxy.Name);
              return;
            }
          }

          ListItem item = _userList.FirstOrDefault(i => i.Selected);
          if (item == null)
            return;

          UserProfile user = item.AdditionalProperties[Consts.KEY_USER] as UserProfile;
          if (!IsRestrictedToOwn)
          {
            foreach (var dataKey in langData.Keys)
            {
              foreach (var data in langData[dataKey])
              {
                if (user.AdditionalData.ContainsKey(dataKey))
                {
                  if (user.AdditionalData[dataKey].ContainsKey(data.Key))
                  {
                    user.AdditionalData[dataKey][data.Key] = data.Value;
                  }
                  else
                  {
                    user.AdditionalData[dataKey].Add(data.Key, data.Value);
                  }
                }
                else
                {
                  user.AdditionalData.Add(dataKey, new Dictionary<int, string>());
                  user.AdditionalData[dataKey].Add(data.Key, data.Value);
                }
              }
            }

            // Update current logged in user if the same
            if (userManagement.CurrentUser.ProfileId == user.ProfileId)
              userManagement.CurrentUser = user;

            item.SetLabel(Consts.KEY_NAME, user.Name);
            item.AdditionalProperties[Consts.KEY_USER] = user;
          }
          _userList.FireChange();

          SetUser(user);
        }
      }
      catch (NotConnectedException)
      {
        DisconnectedError();
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Error("UserLanguageModel: Problems saving user", e);
      }
    }

    #endregion

    #region Private and protected methods

    private void SetUser(UserProfile userProfile)
    {
      try
      {
        if (userProfile != null && UserProxy != null)
        {
          UserProxy.SetUserProfile(userProfile);
        }
        else
        {
          UserProxy?.Clear();
        }

        IsUserSelected = userProfile != null;
        IsRestrictedToOwn = CheckRestrictedToOwn();
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Error("UserLanguageModel: Error selecting user", e);
      }
    }

    private bool CheckRestrictedToOwn()
    {
      IUserManagement userManagement = ServiceRegistration.Get<IUserManagement>();
      if (userManagement == null || userManagement.UserProfileDataManagement == null)
        return true;

      var hasSettings = userManagement.CheckUserAccess(new AccessCheck { RestrictionGroup = "Settings.UserProfile" });
      var hasOwn = userManagement.CheckUserAccess(new AccessCheck { RestrictionGroup = "Settings.UserProfile.ManageOwn" });

      // The restriction to own profile is only valid if the global user management is prohibited.
      return !hasSettings && hasOwn;
    }

    protected internal async Task UpdateUserLists_NoLock(bool create, Guid? selectedUserId = null)
    {
      lock (_syncObj)
      {
        if (_updatingProperties)
          return;
        _updatingProperties = true;
        if (create)
          _userList = new ItemsList();
      }
      try
      {
        IServerConnectionManager scm = ServiceRegistration.Get<IServerConnectionManager>();
        if (scm == null || scm.ContentDirectory == null)
          return;

        SystemName homeServerSystem = scm.LastHomeServerSystem;
        IsHomeServerConnected = homeServerSystem != null;

        IUserManagement userManagement = ServiceRegistration.Get<IUserManagement>();
        if (userManagement == null || userManagement.UserProfileDataManagement == null)
          return;

        bool manageAllUsers = !CheckRestrictedToOwn();

        // add users to expose them
        var users = await userManagement.UserProfileDataManagement.GetProfilesAsync();
        _userList.Clear();
        bool selectedOnce = false;
        foreach (UserProfile user in users)
        {
          if (!manageAllUsers && user.ProfileId != userManagement.CurrentUser.ProfileId)
            continue;

          ListItem item = new ListItem();
          item.SetLabel(Consts.KEY_NAME, user.Name);
          item.AdditionalProperties[Consts.KEY_USER] = user;
          if (selectedUserId.HasValue)
            selectedOnce |= item.Selected = user.ProfileId == selectedUserId;
          item.SelectedProperty.Attach(OnUserItemSelectionChanged);
          lock (_syncObj)
            _userList.Add(item);
        }
        if (!selectedOnce && _userList.Count > 0)
        {
          _userList[0].Selected = true;
        }
        _userList.FireChange();
      }
      catch (NotConnectedException)
      {
        throw;
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Warn("Problems updating users", e);
      }
      finally
      {
        lock (_syncObj)
          _updatingProperties = false;
      }
    }

    private void OnUserItemSelectionChanged(AbstractProperty property, object oldValue)
    {
      // Only handle the event if new item got selected. The unselected event can be ignored.
      if (!(bool)property.GetValue())
        return;

      UserProfile userProfile = null;
      lock (_syncObj)
      {
        userProfile = _userList.Where(i => i.Selected).Select(i => (UserProfile)i.AdditionalProperties[Consts.KEY_USER]).FirstOrDefault();
      }
      SetUser(userProfile);
    }

    protected void ClearData()
    {
      lock (_syncObj)
      {
        _userList = null;
      }
    }

    protected void DisconnectedError()
    {
      // Called when a remote call crashes because the server was disconnected. We don't do anything here because
      // we automatically move to the overview state in the OnMessageReceived method when the server disconnects.
    }

    #endregion

    #region IWorkflowModel implementation

    public Guid ModelId
    {
      get { return MODEL_ID_USERLANGUAGE; }
    }

    public bool CanEnterState(NavigationContext oldContext, NavigationContext newContext)
    {
      return true;
    }

    public void EnterModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      SubscribeToMessages();
      ClearData();
      _ = UpdateUserLists_NoLock(true);
    }

    public void ExitModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      UnsubscribeFromMessages();
      ClearData();
    }

    public void ChangeModelContext(NavigationContext oldContext, NavigationContext newContext, bool push)
    {

    }

    public void Deactivate(NavigationContext oldContext, NavigationContext newContext)
    {
      // Nothing to do here
    }

    public void Reactivate(NavigationContext oldContext, NavigationContext newContext)
    {

    }

    public void UpdateMenuActions(NavigationContext context, IDictionary<Guid, WorkflowAction> actions)
    {
      // Perhaps we'll add menu actions later for different convenience procedures.
    }

    public ScreenUpdateMode UpdateScreen(NavigationContext context, ref string screen)
    {
      return ScreenUpdateMode.AutoWorkflowManager;
    }

    #endregion
  }
}
