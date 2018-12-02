#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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
using System.Text;
using System.Threading.Tasks;
using MediaPortal.Common;
using MediaPortal.Common.General;
using MediaPortal.Common.Localization;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.Messaging;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.SystemCommunication;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Screens;
using MediaPortal.UI.ServerCommunication;

namespace MediaPortal.UiComponents.Media.Models
{
  /// <summary>
  /// Provides a workflow model for selecting matching media items.
  /// </summary>
  public class MediaItemMatchModel : IDisposable
  {
    #region Consts

    public const string STR_MODEL_ID_MIMATCH = "692FA8C3-41A5-43DD-8C12-C857C9C75E72";
    public static readonly Guid MODEL_ID_MIMATCH = new Guid(STR_MODEL_ID_MIMATCH);

    public const string KEY_NAME = "Name";
    public const string KEY_INFO = "Info";
    public const string KEY_ASPECTS = "Aspects";

    #endregion

    #region Protected fields

    protected object _syncObj = new object();
    protected ItemsList _matchList = null;
    protected IDictionary<Guid, IList<MediaItemAspect>> _searchAspects = null;
    protected bool _isVirtual = false;
    protected System.Timers.Timer _liveSearchTimer = new System.Timers.Timer(3000);
    protected DialogCloseWatcher _dialogCloseWatcher = null;
    protected Guid? _matchDialogHandle = null;

    protected AbstractProperty _isSearchingProperty;
    protected AbstractProperty _isManualSearchProperty;
    protected AbstractProperty _isAutomaticSearchProperty;
    protected AbstractProperty _selectedInfoProperty;
    protected AbstractProperty _focusedItemProperty;
    protected AbstractProperty _manualIdProperty;
    protected AsynchronousMessageQueue _messageQueue = null;

    protected IDictionary<Guid, IList<MediaItemAspect>> _matchedAspects = null;

    #endregion

    #region Ctor

    public MediaItemMatchModel()
    {
      _isSearchingProperty = new WProperty(typeof(bool), false);
      _isManualSearchProperty = new WProperty(typeof(bool), false);
      _isAutomaticSearchProperty = new WProperty(typeof(bool), true);
      _isAutomaticSearchProperty.Attach(OnAutomaticSearchChanged);
      _selectedInfoProperty = new WProperty(typeof(string), String.Empty);
      _manualIdProperty = new WProperty(typeof(string), String.Empty);
      _manualIdProperty.Attach(OnManualIdChanged);
      _focusedItemProperty = new SProperty(typeof(object), null);
      _focusedItemProperty.Attach(OnItemFocusedChanged);

      _liveSearchTimer.AutoReset = false;
      _liveSearchTimer.Elapsed += LiveSearchTimeout_Elapsed;
      _matchList = new ItemsList();
    }

    private void LiveSearchTimeout_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
    {
      SetManualMatch();
    }

    public void Dispose()
    {
      _matchList = null;
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

    public AbstractProperty SelectedInformationProperty
    {
      get { return _selectedInfoProperty; }
    }

    public string SelectedInformation
    {
      get { return (string)_selectedInfoProperty.GetValue(); }
      set { _selectedInfoProperty.SetValue(value); }
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

      IMediaItemAspectTypeRegistration miatr = ServiceRegistration.Get<IMediaItemAspectTypeRegistration>();
      if (mediaItem.Aspects.Any(a => miatr.LocallySupportedReimportMediaItemAspectTypes.ContainsKey(a.Key)))
        return true; //Aspect supported for reimport

      return false;
    }

    public Task<IEnumerable<MediaItemAspect>> OpenSelectMatchDialogAsync(MediaItem mediaItem)
    {
      ClearData();
      if (!IsValidMediaItem(mediaItem))
      {
        ServiceRegistration.Get<ILogger>().Error("Error reimporting media item '{0}'. No valid aspects found.", mediaItem.MediaItemId);
        return null;
      }

      _searchAspects = mediaItem.Aspects;
      _isVirtual = mediaItem.IsVirtual;

      _matchedAspects = null;
      var selectionComplete = new TaskCompletionSource<IEnumerable<MediaItemAspect>>();
      _matchDialogHandle = ServiceRegistration.Get<IScreenManager>().ShowDialog("DialogChooseMatch", async(s, g) =>
      {
        try
        {
          if (_matchedAspects != null)
          {
            //Download detailed information if possible. The server might no be allowed to use online sources.
            IMediaAccessor mediaAccessor = ServiceRegistration.Get<IMediaAccessor>();
            foreach (IMetadataExtractor extractor in mediaAccessor.LocalMetadataExtractors.Values)
            {
              await extractor.AddMatchedAspectDetailsAsync(_matchedAspects);
            }
            selectionComplete.SetResult(MediaItemAspect.GetAspects(_matchedAspects));
          }
        }
        catch (Exception ex)
        {
          ServiceRegistration.Get<ILogger>().Error("Error reimporting media item '{0}'. Adding details failed.", ex, mediaItem.MediaItemId);
          selectionComplete.TrySetResult(null);
        }
      });
      _ = DoSearchAsync();
      return selectionComplete.Task;
    }

    protected async Task DoSearchAsync()
    {
      try
      {
        if (IsSearching)
          return;

        IsSearching = true;
        _matchList.Clear();
        SelectedInformation = "";

        //Update reimport aspect
        var reimportAspect = MediaItemAspect.GetOrCreateAspect(_searchAspects, ReimportAspect.Metadata);
        if (IsManualSearch)
        {
          reimportAspect.SetAttribute(ReimportAspect.ATTR_SEARCH, ManualId);
          ServiceRegistration.Get<ILogger>().Info("Re-import: Performing manual search on '{0}'", ManualId);
        }
        else
        {
          reimportAspect.SetAttribute(ReimportAspect.ATTR_SEARCH, null);
          ServiceRegistration.Get<ILogger>().Info("Re-import: Performing automatic search");
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

        ServiceRegistration.Get<ILogger>().Info("Re-import: Valid search categories found: '{0}'", validCategories == null ? "Any" : string.Join(", ", validCategories));

        //Search for matches
        List<MediaItemSearchResult> searchResults = new List<MediaItemSearchResult>();
        IMediaAccessor mediaAccessor = ServiceRegistration.Get<IMediaAccessor>();
        foreach (IMetadataExtractor extractor in mediaAccessor.LocalMetadataExtractors.Values)
        {
          var results = await extractor.SearchForMatchesAsync(_searchAspects, validCategories).ConfigureAwait(false);
          if (results != null)
            searchResults.AddRange(results);
        }

        ServiceRegistration.Get<ILogger>().Info("Re-import: Search returned {0} matches", searchResults.Count);

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
        ServiceRegistration.Get<ILogger>().Error("Error reimporting media item '{0}'", ex, _searchAspects?.ToString() ?? "?");
      }
      finally
      {
        IsSearching = false;
        _matchList.FireChange();
      }
    }

    public void SetMatch(ListItem item)
    {
      if (item == null)
        return;

      IDialogManager dialogManager = ServiceRegistration.Get<IDialogManager>();
      string header = LocalizationHelper.Translate("[Media.ConfirmAction]");
      string text = LocalizationHelper.Translate("[Media.ReimportMediaItem.Confirmation]");
      Guid handle = dialogManager.ShowDialog(header, text, DialogType.YesNoDialog, false, DialogButtonType.No);
      _dialogCloseWatcher = new DialogCloseWatcher(this, handle,
        dialogResult =>
        {
          if (dialogResult == DialogResult.Yes)
          {
            _matchedAspects = (IDictionary<Guid, IList<MediaItemAspect>>)item.AdditionalProperties[KEY_ASPECTS];

            ServiceRegistration.Get<ILogger>().Info("Re-import: Setting matched aspects");
          }
          if(_matchDialogHandle.HasValue)
            ServiceRegistration.Get<IScreenManager>().CloseDialog(_matchDialogHandle.Value);
          _dialogCloseWatcher?.Dispose();
          ClearData();
        });
    }

    public async void SetManualMatch()
    {
      if (string.IsNullOrWhiteSpace(ManualId))
        return;

      await DoSearchAsync();
    }

    #endregion

    #region Private and protected methods

    protected ListItem CreateItem(MediaItemSearchResult searchResult)
    {
      if (searchResult.AspectData?.ContainsKey(ExternalIdentifierAspect.ASPECT_ID) ?? false)
      {
        ListItem listItem = new ListItem();
        listItem.SetLabel(KEY_NAME, searchResult.Name);
        StringBuilder infoText = new StringBuilder();
        foreach (var externaId in searchResult.ExternalIds)
          infoText.Append($"{externaId.Key}: {externaId.Value}\n");
        if (!string.IsNullOrEmpty(searchResult.Description))
          infoText.Append(searchResult.Description);
        listItem.SetLabel(KEY_INFO, infoText.ToString());
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
        SelectedInformation = ((ListItem)prop.GetValue())?.Labels[KEY_INFO].Evaluate() ?? "";
    }

    protected void ClearData()
    {
      lock (_syncObj)
      {
        _matchList.Clear();
        _searchAspects = null;
        SelectedInformation = String.Empty;
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
