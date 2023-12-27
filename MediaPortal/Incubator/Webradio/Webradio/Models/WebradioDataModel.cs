#region Copyright (C) 2007-2023 Team MediaPortal

/*
    Copyright (C) 2007-2023 Team MediaPortal
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
using MediaPortal.UI.Presentation.Models;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.Common.General;
using Webradio.Helper;

namespace Webradio.Models
{
  internal class WebradioDataModel : IWorkflowModel 
  {
    public const string DATA_ID_STR = "BD1BA004-1BC0-49F5-9107-AD8FFD07BAAE";

    public WebradioDataModel()
    {
    }

    #region WebradioDlgShowMessage

    protected static AbstractProperty _dialogMessageProperty = new WProperty(typeof(string), "OK");

    public AbstractProperty DialogMessageProperty => _dialogMessageProperty;

    public static string DialogMessage
    {
      get => (string)_dialogMessageProperty.GetValue();
      set => _dialogMessageProperty.SetValue(value);
    }

    #endregion

    #region RadiostationsCount

    protected static AbstractProperty _radiostationsCountProperty = new WProperty(typeof(int));

    public AbstractProperty RadiostationsCountProperty => _radiostationsCountProperty;

    public static int RadiostationsCount
    {
      get => (int)_radiostationsCountProperty.GetValue();
      set => _radiostationsCountProperty.SetValue(value);
    }

    #endregion

    #region StreamList

    protected static AbstractProperty _streamListCountProperty = new WProperty(typeof(int), 0);

    public AbstractProperty StreamListCountProperty => _streamListCountProperty;

    public static int StreamListCount
    {
      get => (int)_streamListCountProperty.GetValue();
      set => _streamListCountProperty.SetValue(value);
    }

    #endregion

    #region Stream

    #region SelectedStream

    protected static AbstractProperty _selectedStreamProperty = new WProperty(typeof(RadioStation), new RadioStation());

    public AbstractProperty SelectedStreamProperty => _selectedStreamProperty;

    public static RadioStation SelectedStream
    {
      get => (RadioStation)_selectedStreamProperty.GetValue();
      set
      {
        WebradioFavoritesModel.IsFavorite = Webradio.Settings.Favorites.IsFavorite(value);
        _selectedStreamProperty.SetValue(value);
      }
    }

    #endregion

    #region CurrentStreamLogo

    protected static AbstractProperty _currentStreamLogoProperty = new WProperty(typeof(string), string.Empty);

    public AbstractProperty CurrentStreamLogoProperty => _currentStreamLogoProperty;

    public static string CurrentStreamLogo
    {
      get => (string)_currentStreamLogoProperty.GetValue();
      set => _currentStreamLogoProperty.SetValue(value);
    }

    #endregion

    #region DefaultStreamLogo

    protected static AbstractProperty _defaultStreamLogoProperty = new WProperty(typeof(string), string.Empty);

    public AbstractProperty DefaultStreamLogoProperty => _defaultStreamLogoProperty;

    public static string DefaultStreamLogo
    {
      get => (string)_defaultStreamLogoProperty.GetValue();
      set => _defaultStreamLogoProperty.SetValue(value);
    }

    #endregion

    #region StreamBitrate

    protected static AbstractProperty _streamBitrateProperty = new WProperty(typeof(string), string.Empty);

    public AbstractProperty StreamBitrateProperty => _streamBitrateProperty;

    public static string StreamBitrate
    {
      get => (string)_streamBitrateProperty.GetValue();
      set => _streamBitrateProperty.SetValue(value);
    }

    #endregion

    #region StreamTyp

    protected static AbstractProperty _streamTypProperty = new WProperty(typeof(string), string.Empty);

    public AbstractProperty StreamTypProperty => _streamTypProperty;

    public static string StreamTyp
    {
      get => (string)_streamTypProperty.GetValue();
      set => _streamTypProperty.SetValue(value);
    }

    #endregion

    #endregion

    #region Radiostation

    #region StationName

    protected static AbstractProperty _stationNameProperty = new WProperty(typeof(string), string.Empty);

    public AbstractProperty StationNameProperty => _stationNameProperty;

    public static string StationName
    {
      get => (string)_stationNameProperty.GetValue();
      set => _stationNameProperty.SetValue(value);
    }

    #endregion

    #region StationCountry

    protected static AbstractProperty _stationCountryProperty = new WProperty(typeof(string), string.Empty);

    public AbstractProperty StationCountryProperty => _stationCountryProperty;

    public static string StationCountry
    {
      get => (string)_stationCountryProperty.GetValue();
      set => _stationCountryProperty.SetValue(value);
    }

    #endregion

    #region StationCity

    protected static AbstractProperty _stationCityProperty = new WProperty(typeof(string), string.Empty);
    public AbstractProperty StationCityProperty => _stationCityProperty;

    public static string StationCity
    {
      get => (string)_stationCityProperty.GetValue();
      set => _stationCityProperty.SetValue(value);
    }

    #endregion

    #region StationInfo

    protected static AbstractProperty _stationInfoProperty = new WProperty(typeof(string), string.Empty);

    public AbstractProperty StationInfoProperty => _stationInfoProperty;

    public static string StationInfo
    {
      get => (string)_stationInfoProperty.GetValue();
      set => _stationInfoProperty.SetValue(value);
    }

    #endregion

    #region StationLanguage

    protected static AbstractProperty _stationLanguageProperty = new WProperty(typeof(string), string.Empty);

    public AbstractProperty StationLanguageProperty => _stationLanguageProperty;

    public static string StationLanguage
    {
      get => (string)_stationLanguageProperty.GetValue();
      set => _stationLanguageProperty.SetValue(value);
    }

    #endregion

    #region StationName

    protected static AbstractProperty _activeFilterProperty = new WProperty(typeof(string), string.Empty);

    public AbstractProperty ActiveFilterProperty => _activeFilterProperty;

    public static string ActiveFilter
    {
      get => (string)_activeFilterProperty.GetValue();
      set => _activeFilterProperty.SetValue(value);
    }

    #endregion

    #endregion

    #region Track

    #region TrackArtist

    protected static AbstractProperty _trackArtistProperty = new WProperty(typeof(string), string.Empty);

    public AbstractProperty TrackArtistProperty => _trackArtistProperty;

    public static string TrackArtist
    {
      get => (string)_trackArtistProperty.GetValue();
      set => _trackArtistProperty.SetValue(value);
    }

    #endregion

    #region TrackName

    protected static AbstractProperty _trackNameProperty = new WProperty(typeof(string), string.Empty);

    public AbstractProperty TrackNameProperty => _trackNameProperty;

    public static string TrackName
    {
      get => (string)_trackNameProperty.GetValue();
      set => _trackNameProperty.SetValue(value);
    }

    #endregion

    #endregion

    #region Update

    #region UpdateChecked

    protected static AbstractProperty _updateCheckedProperty = new WProperty(typeof(bool), false);

    public AbstractProperty UpdateCheckedProperty => _updateCheckedProperty;

    public static bool UpdateChecked
    {
      get => (bool)_updateCheckedProperty.GetValue();
      set => _updateCheckedProperty.SetValue(value);
    }

    #endregion

    #region UpdateProgress

    protected static AbstractProperty _updateProgressProperty = new WProperty(typeof(int), 0);

    public AbstractProperty UpdateProgressProperty => _updateProgressProperty;

    public static int UpdateProgress
    {
      get => (int)_updateProgressProperty.GetValue();
      set => _updateProgressProperty.SetValue(value);
    }


    #endregion

    #region UpdateInfo

    protected static AbstractProperty _updateInfoProperty = new WProperty(typeof(string), string.Empty);

    public AbstractProperty UpdateInfoProperty => _updateInfoProperty;

    public static string UpdateInfo
    {
      get => (string)_updateInfoProperty.GetValue();
      set => _updateInfoProperty.SetValue(value);
    }

    #endregion

    #region OfflineVersion

    protected static AbstractProperty _offlineVersionProperty = new WProperty(typeof(int));

    public AbstractProperty OfflineVersionProperty => _offlineVersionProperty;

    public static int OfflineVersion
    {
      get => (int)_offlineVersionProperty.GetValue();
      set => _offlineVersionProperty.SetValue(value);
    }

    #endregion

    #region OnlineVersion

    protected static AbstractProperty _onlineVersionProperty = new WProperty(typeof(int));

    public AbstractProperty OnlineVersionProperty => _onlineVersionProperty;

    public static int OnlineVersion
    {
      get => (int)_onlineVersionProperty.GetValue();
      set => _onlineVersionProperty.SetValue(value);
    }

    #endregion

    #endregion

    #region FavoritesContextMenu
    
    #region SelectedStreamId

    protected static AbstractProperty _selectedStreamIdProperty = new WProperty(typeof(int), 0);

    public AbstractProperty SelectedStreamIdProperty => _selectedStreamIdProperty;

    public static int SelectedStreamId
    {
      get => (int)_selectedStreamIdProperty.GetValue();
      set => _selectedStreamIdProperty.SetValue(value);
    }

    #endregion

    //#region FavoritesContextMenuButtonText

    //protected static AbstractProperty _trackNameProperty = new WProperty(typeof(string), string.Empty);

    //public AbstractProperty TrackNameProperty => _trackNameProperty;

    //public static string TrackName
    //{
    //  get => (string)_trackNameProperty.GetValue();
    //  set => _trackNameProperty.SetValue(value);
    //}

    //#endregion

    #endregion

    #region IWorkflowModel implementation

    public Guid ModelId
    {
      get { return new Guid(DATA_ID_STR); }
    }

    public bool CanEnterState(NavigationContext oldContext, NavigationContext newContext)
    {
      return true;
    }

    public void EnterModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
    }

    public void ExitModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
    }

    public void ChangeModelContext(NavigationContext oldContext, NavigationContext newContext, bool push)
    {
      // We could initialize some data here when changing the media navigation state
    }

    public void Deactivate(NavigationContext oldContext, NavigationContext newContext)
    {
    }

    public void Reactivate(NavigationContext oldContext, NavigationContext newContext)
    {
    }

    public void UpdateMenuActions(NavigationContext context, IDictionary<Guid, WorkflowAction> actions)
    {
    }

    public ScreenUpdateMode UpdateScreen(NavigationContext context, ref string screen)
    {
      return ScreenUpdateMode.AutoWorkflowManager;
    }

    #endregion
  }
}
