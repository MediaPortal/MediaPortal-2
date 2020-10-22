#region Copyright (C) 2007-2020 Team MediaPortal

/*
    Copyright (C) 2007-2020 Team MediaPortal
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
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaPortal.Common;
using MediaPortal.Common.General;
using MediaPortal.Common.Localization;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.TransientAspects;
using MediaPortal.Common.Messaging;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.SystemCommunication;
using MediaPortal.Common.UserManagement;
using MediaPortal.Common.UserProfileDataManagement;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Screens;
using MediaPortal.UI.ServerCommunication;

namespace MediaPortal.UiComponents.Media.Models
{
  /// <summary>
  /// Provides a workflow model for selecting matching media items.
  /// </summary>
  public class SubtitleMatchModel : IDisposable
  {
    #region Consts

    public const string STR_MODEL_ID_SUBMATCH = "0D8F57A1-3082-4C98-8522-6FC4512DF56A";
    public static readonly Guid MODEL_ID_SUBMATCH = new Guid(STR_MODEL_ID_SUBMATCH);

    public const string KEY_NAME = "Name";
    public const string KEY_PROVIDER = "Info";
    public const string KEY_MATCH = "Match";
    public const string KEY_LANG = "Language";
    public const string KEY_ASPECTS = "Aspects";
    public const string KEY_INDEXES = "Indexes";

    #endregion

    #region Protected fields

    protected object _syncObj = new object();
    protected ItemsList _matchList = null;
    protected ItemsList _editionList = null;
    protected IDictionary<Guid, IList<MediaItemAspect>> _searchAspects = null;
    protected bool _isVirtual = false;
    protected System.Timers.Timer _liveSearchTimer = new System.Timers.Timer(3000);
    protected DialogCloseWatcher _dialogCloseWatcher = null;
    protected Guid? _matchDialogHandle = null;
    protected IList<int> _selectedEditionIndexes = null;

    protected AbstractProperty _isSearchingProperty;
    protected AbstractProperty _isManualSearchProperty;
    protected AbstractProperty _isAutomaticSearchProperty;
    protected AbstractProperty _providerProperty;
    protected AbstractProperty _matchPercentProperty;
    protected AbstractProperty _languageProperty;
    protected AbstractProperty _focusedItemProperty;
    protected AbstractProperty _manualIdProperty;
    protected AsynchronousMessageQueue _messageQueue = null;

    protected IDictionary<Guid, IList<MediaItemAspect>> _matchedAspects = null;

    #endregion

    #region Ctor

    public SubtitleMatchModel()
    {
      _isSearchingProperty = new WProperty(typeof(bool), false);
      _isManualSearchProperty = new WProperty(typeof(bool), false);
      _isAutomaticSearchProperty = new WProperty(typeof(bool), true);
      _isAutomaticSearchProperty.Attach(OnAutomaticSearchChanged);
      _providerProperty = new WProperty(typeof(string), String.Empty);
      _matchPercentProperty = new WProperty(typeof(string), String.Empty);
      _languageProperty = new WProperty(typeof(string), String.Empty);
      _manualIdProperty = new WProperty(typeof(string), String.Empty);
      _manualIdProperty.Attach(OnManualIdChanged);
      _focusedItemProperty = new SProperty(typeof(object), null);
      _focusedItemProperty.Attach(OnItemFocusedChanged);

      _liveSearchTimer.AutoReset = false;
      _liveSearchTimer.Elapsed += LiveSearchTimeout_Elapsed;
      _matchList = new ItemsList();
      _editionList = new ItemsList();
    }

    private void LiveSearchTimeout_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
    {
      SetManualMatch();
    }

    public void Dispose()
    {
      _matchList = null;
      _editionList = null;
    }

    #endregion

    #region Public properties (Also accessed from the GUI)

    public ItemsList MatchList
    {
      get
      {
        lock (_syncObj)
          return _matchList;
      }
    }

    public ItemsList EditionList
    {
      get
      {
        lock (_syncObj)
          return _editionList;
      }
    }

    public AbstractProperty IsSearchingProperty
    {
      get { return _isSearchingProperty; }
    }

    public bool IsSearching
    {
      get { return (bool)_isSearchingProperty.GetValue(); }
      set { _isSearchingProperty.SetValue(value); }
    }

    public AbstractProperty IsManualSearchProperty
    {
      get { return _isManualSearchProperty; }
    }

    public bool IsManualSearch
    {
      get { return (bool)_isManualSearchProperty.GetValue(); }
      set { _isManualSearchProperty.SetValue(value); }
    }

    public AbstractProperty IsAutomaticSearchProperty
    {
      get { return _isAutomaticSearchProperty; }
    }

    public bool IsAutomaticSearch
    {
      get { return (bool)_isAutomaticSearchProperty.GetValue(); }
      set { _isAutomaticSearchProperty.SetValue(value); }
    }

    public AbstractProperty SelectedProviderProperty
    {
      get { return _providerProperty; }
    }

    public string SelectedProvider
    {
      get { return (string)_providerProperty.GetValue(); }
      set { _providerProperty.SetValue(value); }
    }

    public AbstractProperty SelectedPercentageProperty
    {
      get { return _matchPercentProperty; }
    }

    public string SelectedPercentage
    {
      get { return (string)_matchPercentProperty.GetValue(); }
      set { _matchPercentProperty.SetValue(value); }
    }
    public AbstractProperty SelectedLanguageProperty
    {
      get { return _languageProperty; }
    }

    public string SelectedLanguage
    {
      get { return (string)_languageProperty.GetValue(); }
      set { _languageProperty.SetValue(value); }
    }
    public AbstractProperty ManualIdProperty
    {
      get { return _manualIdProperty; }
    }

    public string ManualId
    {
      get { return (string)_manualIdProperty.GetValue(); }
      set { _manualIdProperty.SetValue(value); }
    }

    public AbstractProperty FocusedItemProperty
    {
      get { return _focusedItemProperty; }
    }

    public object FocusedItem
    {
      get { return _focusedItemProperty.GetValue(); }
      set { _focusedItemProperty.SetValue(value); }
    }

    #endregion

    #region Public methods

    public bool IsValidMediaItem(MediaItem mediaItem)
    {
      if (mediaItem == null)
        return false;

      if (mediaItem.IsStub)
        return false;

      if (!mediaItem.Aspects.ContainsKey(MovieAspect.ASPECT_ID) && !mediaItem.Aspects.ContainsKey(EpisodeAspect.ASPECT_ID))
        return false;

      return true;
    }

    public Task<bool> OpenSelectEditionDialogAsync(MediaItem mediaItem)
    {
      _selectedEditionIndexes = null;
      _editionList.Clear();
      if (!IsValidMediaItem(mediaItem))
      {
        ServiceRegistration.Get<ILogger>().Error("Error finding subtitles for media item '{0}'. No valid aspects found.", mediaItem.MediaItemId);
        return Task.FromResult<bool>(false);
      }
      if(!mediaItem.HasEditions)
      {
        return Task.FromResult<bool>(false);
      }

      foreach (var edition in mediaItem.Editions)
      {
        ListItem listItem = new ListItem();
        listItem.SetLabel(KEY_NAME, edition.Value.Name);
        listItem.AdditionalProperties[KEY_INDEXES] = edition.Value.PrimaryResourceIndexes;
        _editionList.Add(listItem);
      }

      var selectionComplete = new TaskCompletionSource<bool>();
      _matchDialogHandle = ServiceRegistration.Get<IScreenManager>().ShowDialog("DialogChooseSubtitleEdition", (s, g) =>
      {
        try
        {
          selectionComplete.SetResult(_selectedEditionIndexes != null);
        }
        catch (Exception ex)
        {
          ServiceRegistration.Get<ILogger>().Error("Error selecting subtitle edition for media item '{0}'", ex, mediaItem.MediaItemId);
          selectionComplete.TrySetResult(false);
        }
      });
      return selectionComplete.Task;
    }

    public Task<IEnumerable<MediaItemAspect>> OpenSelectMatchDialogAsync(MediaItem mediaItem)
    {
      ClearData();
      if (!IsValidMediaItem(mediaItem))
      {
        ServiceRegistration.Get<ILogger>().Error("Error finding subtitles for media item '{0}'. No valid aspects found.", mediaItem.MediaItemId);
        return Task.FromResult<IEnumerable<MediaItemAspect>>(null);
      }

      _searchAspects = mediaItem.Aspects;
      if(_selectedEditionIndexes != null)
      {
        FilterSelectedAspects(ProviderResourceAspect.Metadata, ProviderResourceAspect.ATTR_RESOURCE_INDEX);
        FilterSelectedAspects(VideoStreamAspect.Metadata, VideoStreamAspect.ATTR_RESOURCE_INDEX);
        FilterSelectedAspects(VideoAudioStreamAspect.Metadata, VideoAudioStreamAspect.ATTR_RESOURCE_INDEX);
        FilterSelectedAspects(SubtitleAspect.Metadata, SubtitleAspect.ATTR_RESOURCE_INDEX);
      }
      var subSearch = MediaItemAspect.GetOrCreateAspect(_searchAspects, TempSubtitleAspect.Metadata);

      IUserManagement userManagement = ServiceRegistration.Get<IUserManagement>();
      if (userManagement?.CurrentUser != null)
      {
        var subLangs = userManagement.CurrentUser.AdditionalData.Where(d => d.Key == UserDataKeysKnown.KEY_PREFERRED_SUBTITLE_LANGUAGE);
        if (subLangs.Any())
        {
          List<string> languages = new List<string>();
          var cultures = CultureInfo.GetCultures(CultureTypes.SpecificCultures);
          foreach (var subLang in subLangs)
          {
            foreach (var lang in subLang.Value.OrderBy(l => l.Key))
              languages.Add(cultures?.FirstOrDefault(c => c.TwoLetterISOLanguageName == lang.Value).Name);
          }
          subSearch.SetAttribute(TempSubtitleAspect.ATTR_LANGUAGE, string.Join(",", languages));
        }
        else
        {
          subSearch.SetAttribute(TempSubtitleAspect.ATTR_LANGUAGE, "en-US");
        }
      }

      _isVirtual = mediaItem.IsVirtual;

      _matchedAspects = null;
      var selectionComplete = new TaskCompletionSource<IEnumerable<MediaItemAspect>>();
      _matchDialogHandle = ServiceRegistration.Get<IScreenManager>().ShowDialog("DialogChooseSubtitleMatch", (s, g) =>
      {
        try
        {
          if (_matchedAspects != null)
          {
            selectionComplete.SetResult(MediaItemAspect.GetAspects(_matchedAspects));
          }
        }
        catch (Exception ex)
        {
          ServiceRegistration.Get<ILogger>().Error("Error selecting subtitle match for media item '{0}'", ex, mediaItem.MediaItemId);
          selectionComplete.TrySetResult(null);
        }
      });
      _ = DoSearchAsync();
      return selectionComplete.Task;
    }

    protected void FilterSelectedAspects(MultipleMediaItemAspectMetadata metadata, MultipleMediaItemAspectMetadata.MultipleAttributeSpecification indexAttribute)
    {
      IList<MultipleMediaItemAspect> aspects;
      if (MediaItemAspect.TryGetAspects(_searchAspects, metadata, out aspects))
      {
        var relevantAspects = aspects.Where(p => _selectedEditionIndexes.Contains(p.GetAttributeValue<int>(indexAttribute)));
        _searchAspects[metadata.AspectId].Clear();
        foreach (var aspect in relevantAspects)
          _searchAspects[metadata.AspectId].Add(aspect);
      }
    }

    protected async Task DoSearchAsync()
    {
      try
      {
        if (IsSearching)
          return;

        IsSearching = true;
        _matchList.Clear();
        SelectedProvider = "";
        SelectedPercentage = "";
        SelectedLanguage = "";

        //Update reimport aspect
        var reimportAspect = MediaItemAspect.GetOrCreateAspect(_searchAspects, ReimportAspect.Metadata);
        if (IsManualSearch)
        {
          reimportAspect.SetAttribute(ReimportAspect.ATTR_SEARCH, ManualId);
          ServiceRegistration.Get<ILogger>().Info("Subtitle search: Performing manual search on '{0}'", ManualId);
        }
        else
        {
          reimportAspect.SetAttribute(ReimportAspect.ATTR_SEARCH, null);
          ServiceRegistration.Get<ILogger>().Info("Subtitle search: Performing automatic search");
        }

        //Restrict possible MDEs by category if possible
        ICollection<string> validCategories = null;
        IServerConnectionManager scm = ServiceRegistration.Get<IServerConnectionManager>();
        IContentDirectory cd = scm?.ContentDirectory;
        var shares = await cd?.GetSharesAsync(null, SharesFilter.All);
        IList<MultipleMediaItemAspect> providerResourceAspects;
        if (shares != null && MediaItemAspect.TryGetAspects(_searchAspects, ProviderResourceAspect.Metadata, out providerResourceAspects))
        {
          var pra = providerResourceAspects.FirstOrDefault(p => p.GetAttributeValue<int>(ProviderResourceAspect.ATTR_TYPE) == ProviderResourceAspect.TYPE_PRIMARY);
          if (pra != null)
          {
            var resPath = ResourcePath.Deserialize(pra.GetAttributeValue<string>(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH));
            var share = shares.FirstOrDefault(s => s.BaseResourcePath.IsSameOrParentOf(resPath));
            validCategories = share?.MediaCategories;
          }
        }

        ServiceRegistration.Get<ILogger>().Info("Subtitle search: Valid search categories found: '{0}'", validCategories == null ? "Any" : string.Join(", ", validCategories));

        //Search for matches
        List<MediaItemSearchResult> searchResults = new List<MediaItemSearchResult>();
        IMediaAccessor mediaAccessor = ServiceRegistration.Get<IMediaAccessor>();
        foreach (IMetadataExtractor extractor in mediaAccessor.LocalMetadataExtractors.Values)
        {
          if (!extractor.Metadata.ExtractedAspectTypes.ContainsKey(SubtitleAspect.ASPECT_ID))
            continue;

          var results = await extractor.SearchForMatchesAsync(_searchAspects, validCategories).ConfigureAwait(false);
          if (results != null)
            searchResults.AddRange(results);
        }

        ServiceRegistration.Get<ILogger>().Info("Subtitle search: Search returned {0} matches", searchResults.Count);

        IsSearching = false;

        foreach (var result in searchResults)
        {
          var item = CreateItem(result);
          if (item != null)
            _matchList.Add(item);
        }
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error("Error searching for sutitles for media item '{0}'", ex, _searchAspects?.ToString() ?? "?");
      }
      finally
      {
        IsSearching = false;
        _matchList.FireChange();
      }
    }

    public void SetEdition(ListItem item)
    {
      var indexes = (IList<int>)item.AdditionalProperties[KEY_INDEXES];
      _selectedEditionIndexes = indexes;
      ServiceRegistration.Get<ILogger>().Info("Subtitle edition: Setting selected edition");
    }

    public void SetMatch(ListItem item)
    {
      if (item == null)
        return;

      var langs = _searchAspects[TempSubtitleAspect.ASPECT_ID].First().GetAttributeValue<string>(TempSubtitleAspect.ATTR_LANGUAGE);
      List<string> searchLangs = new List<string>();
      if (langs?.Count() > 0)
        searchLangs.AddRange(langs.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(l => new CultureInfo(l).TwoLetterISOLanguageName));

      bool externalSubsPresent = false;
      foreach(var sub in _searchAspects.Where(a => a.Key == SubtitleAspect.ASPECT_ID).SelectMany(a => a.Value))
      {
        var subLang = sub.GetAttributeValue<string>(SubtitleAspect.ATTR_SUBTITLE_LANGUAGE);
        if (!sub.GetAttributeValue<bool>(SubtitleAspect.ATTR_INTERNAL) && (searchLangs.Count == 0 || string.IsNullOrEmpty(subLang) || searchLangs.Contains(subLang)))
        {
          externalSubsPresent = true;
          break;
        }
      }

      //If external sub is present a match might ovewrite it so ask for confirmation
      if (externalSubsPresent)
      {
        IDialogManager dialogManager = ServiceRegistration.Get<IDialogManager>();
        string header = LocalizationHelper.Translate("[Media.ConfirmAction]");
        string text = LocalizationHelper.Translate("[Media.SubtitleReplace.Confirmation]");
        Guid handle = dialogManager.ShowDialog(header, text, DialogType.YesNoDialog, false, DialogButtonType.No);
        _dialogCloseWatcher = new DialogCloseWatcher(this, handle,
          dialogResult =>
          {
            if (dialogResult == DialogResult.Yes)
            {
              SetMatchedAspects(item);
            }
            if (_matchDialogHandle.HasValue)
              ServiceRegistration.Get<IScreenManager>().CloseDialog(_matchDialogHandle.Value);
            _dialogCloseWatcher?.Dispose();
            ClearData();
          });
      }
      else
      {
        SetMatchedAspects(item);
        if (_matchDialogHandle.HasValue)
          ServiceRegistration.Get<IScreenManager>().CloseDialog(_matchDialogHandle.Value);
        ClearData();
      }
    }

    public async void SetManualMatch()
    {
      if (string.IsNullOrWhiteSpace(ManualId))
        return;

      await DoSearchAsync();
    }

    #endregion

    #region Private and protected methods

    private void SetMatchedAspects(ListItem item)
    {
      var aspects = (IDictionary<Guid, IList<MediaItemAspect>>)item.AdditionalProperties[KEY_ASPECTS];
      aspects[ProviderResourceAspect.ASPECT_ID] = _searchAspects[ProviderResourceAspect.ASPECT_ID].Where(a => a.GetAttributeValue<int>(ProviderResourceAspect.ATTR_TYPE) == ProviderResourceAspect.TYPE_PRIMARY).ToList();

      _matchedAspects = aspects;
      ServiceRegistration.Get<ILogger>().Info("Subtitle search: Setting matched aspects");
    }

    protected ListItem CreateItem(MediaItemSearchResult searchResult)
    {
      if (searchResult.AspectData?.Count() > 0)
      {
        ListItem listItem = new ListItem();
        listItem.SetLabel(KEY_NAME, searchResult.Name);
        listItem.SetLabel(KEY_PROVIDER, string.Join(", ", searchResult.Providers));
        listItem.SetLabel(KEY_MATCH, $"{searchResult.MatchPercentage}%");
        listItem.SetLabel(KEY_LANG, $"{(searchResult.Language == null ? "" : $" {new CultureInfo(searchResult.Language).DisplayName}")}");
        var reimportAspect = MediaItemAspect.GetOrCreateAspect(searchResult.AspectData, ReimportAspect.Metadata);
        if (IsManualSearch)
          reimportAspect.SetAttribute(ReimportAspect.ATTR_SEARCH, ManualId);
        else
          reimportAspect.SetAttribute(ReimportAspect.ATTR_SEARCH, null);
        listItem.AdditionalProperties[KEY_ASPECTS] = searchResult.AspectData;
        return listItem;
      }
      return null;
    }

    async void OnAutomaticSearchChanged(AbstractProperty prop, object oldVal)
    {
      if (prop.HasValue() && (bool)prop.GetValue() && (bool)oldVal == false)
      {
        await DoSearchAsync();
      }
    }

    void OnManualIdChanged(AbstractProperty prop, object oldVal)
    {
      if (prop.HasValue() && !string.IsNullOrWhiteSpace((string)prop.GetValue()))
      {
        _liveSearchTimer.Stop();
        _liveSearchTimer.Start();
      }
    }

    void OnItemFocusedChanged(AbstractProperty prop, object oldVal)
    {
      if (prop.HasValue())
      {
        var item = (ListItem)prop.GetValue();
        SelectedProvider = item?.Labels[KEY_PROVIDER].Evaluate() ?? "";
        SelectedPercentage = item?.Labels[KEY_MATCH].Evaluate() ?? "";
        SelectedLanguage = item?.Labels[KEY_LANG].Evaluate() ?? "";
      }
    }

    protected void ClearData()
    {
      lock (_syncObj)
      {
        _matchList.Clear();
        _searchAspects = null;
        SelectedProvider = String.Empty;
        SelectedPercentage = String.Empty;
        SelectedLanguage = String.Empty;
        _matchDialogHandle = null;
      }
    }

    protected void DisconnectedError()
    {
      // Called when a remote call crashes because the server was disconnected. We don't do anything here because
      // we automatically move to the overview state in the OnMessageReceived method when the server disconnects.
    }

    #endregion
  }
}
