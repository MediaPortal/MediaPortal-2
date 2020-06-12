using Emulators.Common.Emulators;
using Emulators.Common.TheGamesDb;
using Emulators.Emulator;
using MediaPortal.Common;
using MediaPortal.Common.General;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Utilities;
using MediaPortal.UiComponents.SkinBase.General;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Emulators.Models
{
  public enum EmulatorType
  {
    Emulator,
    Native,
    LibRetro
  }

  public class EmulatorProxy
  {
    #region Protected Members
    protected AbstractProperty _emulatorTypeProperty;
    protected AbstractProperty _nameProperty;
    protected AbstractProperty _argumentsProperty;
    protected AbstractProperty _workingDirectoryProperty;
    protected AbstractProperty _useQuotesProperty;
    protected AbstractProperty _fileExtensionsProperty;
    protected AbstractProperty _exitsOnEscapeKeyProperty;
    protected AbstractProperty _isEmulatorNameValidProperty;
    protected AbstractProperty _selectedGameCategories;
    protected AbstractProperty _isGameCategoriesSelected;
    protected ItemsList _fileExtensionItems;
    protected ItemsList _gameCategories;
    protected PathBrowser _pathBrowser;
    protected PathBrowserCloseWatcher _pathBrowserCloseWatcher;
    protected EmulatorConfiguration _configuration;
    protected LibRetroProxy _libRetroProxy;
    #endregion

    #region Constructor
    public EmulatorProxy(EmulatorType emulatorType) 
      : this(null)
    {
      EmulatorType = emulatorType;
    }

    public EmulatorProxy(EmulatorConfiguration configuration)
    {
      _emulatorTypeProperty = new WProperty(typeof(EmulatorType), EmulatorType.Emulator);
      _nameProperty = new WProperty(typeof(string), null);
      _argumentsProperty = new WProperty(typeof(string), null);
      _workingDirectoryProperty = new WProperty(typeof(string), null);
      _useQuotesProperty = new WProperty(typeof(bool), true);
      _fileExtensionsProperty = new WProperty(typeof(HashSet<string>), new HashSet<string>());
      _exitsOnEscapeKeyProperty = new WProperty(typeof(bool), false);
      _nameProperty.Attach(OnEmulatorNameChanged);
      _isEmulatorNameValidProperty = new WProperty(typeof(bool), false);
      _selectedGameCategories = new WProperty(typeof(IList<string>), new List<string>());
      _isGameCategoriesSelected = new WProperty(typeof(bool), false);
      _fileExtensionItems = new ItemsList();
      _gameCategories = new ItemsList();
      _pathBrowser = new PathBrowser();
      _configuration = configuration;
      InitialiseProperties();
    }
    #endregion

    #region GUI Properties
    public EmulatorConfiguration Configuration
    {
      get { return _configuration; }
    }

    public string ConfigEmulatorTitle
    {
      get { return _configuration == null ? EmulatorsConsts.RES_ADD_EMULATOR_CONFIGURATION_TITLE : EmulatorsConsts.RES_EDIT_EMULATOR_CONFIGURATION_TITLE; }
    }

    public AbstractProperty EmulatorTypeProperty { get { return _emulatorTypeProperty; } }
    public EmulatorType EmulatorType
    {
      get { return (EmulatorType)_emulatorTypeProperty.GetValue(); }
      set { _emulatorTypeProperty.SetValue(value); }
    }

    public AbstractProperty NameProperty { get { return _nameProperty; } }
    public string Name
    {
      get { return (string)_nameProperty.GetValue(); }
      set { _nameProperty.SetValue(value); }
    }

    public AbstractProperty ArgumentsProperty { get { return _argumentsProperty; } }
    public string Arguments
    {
      get { return (string)_argumentsProperty.GetValue(); }
      set { _argumentsProperty.SetValue(value); }
    }

    public AbstractProperty WorkingDirectoryProperty { get { return _workingDirectoryProperty; } }
    public string WorkingDirectory
    {
      get { return (string)_workingDirectoryProperty.GetValue(); }
      set { _workingDirectoryProperty.SetValue(value); }
    }

    public AbstractProperty UseQuotesProperty { get { return _useQuotesProperty; } }
    public bool UseQuotes
    {
      get { return (bool)_useQuotesProperty.GetValue(); }
      set { _useQuotesProperty.SetValue(value); }
    }

    public AbstractProperty FileExtensionsProperty { get { return _fileExtensionsProperty; } }
    public HashSet<string> FileExtensions
    {
      get { return (HashSet<string>)_fileExtensionsProperty.GetValue(); }
      set { _fileExtensionsProperty.SetValue(value); }
    }

    public AbstractProperty ExitsOnEscapeKeyProperty { get { return _exitsOnEscapeKeyProperty; } }
    public bool ExitsOnEscapeKey
    {
      get { return (bool)_exitsOnEscapeKeyProperty.GetValue(); }
      set { _exitsOnEscapeKeyProperty.SetValue(value); }
    }

    public AbstractProperty IsEmulatorNameValidProperty { get { return _isEmulatorNameValidProperty; } }
    public bool IsEmulatorNameValid
    {
      get { return (bool)_isEmulatorNameValidProperty.GetValue(); }
      set { _isEmulatorNameValidProperty.SetValue(value); }
    }

    public AbstractProperty SelectedGameCategoriesProperty { get { return _selectedGameCategories; } }
    public IList<string> SelectedGameCategories
    {
      get { return (IList<string>)_selectedGameCategories.GetValue(); }
      set { _selectedGameCategories.SetValue(value); }
    }

    public AbstractProperty IsGameCategoriesSelectedProperty { get { return _isGameCategoriesSelected; } }
    public bool IsGameCategoriesSelected
    {
      get { return (bool)_isGameCategoriesSelected.GetValue(); }
      set { _isGameCategoriesSelected.SetValue(value); }
    }

    public ItemsList FileExtensionItems
    {
      get { return _fileExtensionItems; }
    }

    public ItemsList GameCategories
    {
      get { return _gameCategories; }
    }

    public PathBrowser PathBrowser
    {
      get { return _pathBrowser; }
    }

    public LibRetroProxy LibRetroProxy
    {
      get { return _libRetroProxy; }
    }
    #endregion

    #region Public Methods
    public void SetSuggestedSettings(bool force)
    {
      if (!force && _configuration != null)
        return;
      if (EmulatorType == EmulatorType.Native)
        SetDefaultNativeSettings();
      else if (EmulatorType == EmulatorType.LibRetro)
        SetDefaultLibRetroSettings();
      else if (!string.IsNullOrEmpty(PathBrowser.ChoosenResourcePathDisplayName))
        SetDefaultEmulatorSettings();
    }

    public void UpdateGameCategories()
    {
      IList<string> selectedCategories = SelectedGameCategories;
      _gameCategories.Clear();
      foreach (string category in GetGameMediaCategories())
      {
        ListItem listItem = new ListItem(Consts.KEY_NAME, category);
        listItem.Selected = selectedCategories.Contains(category);
        listItem.SelectedProperty.Attach(OnCategorySelectionChanged);
        _gameCategories.Add(listItem);
      }
      _gameCategories.FireChange();
    }

    public void UpdateFileExtensionItems()
    {
      _fileExtensionItems.Clear();
      foreach (string extension in FileExtensions)
        _fileExtensionItems.Add(new ListItem(Consts.KEY_NAME, extension));
      _fileExtensionItems.FireChange();
    }

    public void AddFileExtension(string extension)
    {
      if (string.IsNullOrEmpty(extension))
        return;
      FileExtensions.Add(extension);
      UpdateFileExtensionItems();
    }

    public void RemoveFileExtension(ListItem item)
    {
      if (item == null)
        return;
      FileExtensions.Remove(item[Consts.KEY_NAME]);
      UpdateFileExtensionItems();
    }

    public void InsertWildcard(string wildcard)
    {
      Arguments = Arguments + wildcard;
    }

    public void ShowWorkingDirectoryDialog()
    {
      string workingDirectory = WorkingDirectory;
      ResourcePath initialPath = string.IsNullOrEmpty(workingDirectory) ? null : LocalFsResourceProviderBase.ToResourcePath(workingDirectory);
      Guid dialogHandle = ServiceRegistration.Get<IPathBrowser>().ShowPathBrowser("[Emulators.Config.WorkingDirectory.Label]", false, false, initialPath,
          path =>
          {
            string chosenPath = LocalFsResourceProviderBase.ToDosPath(path.LastPathSegment.Path);
            return !string.IsNullOrEmpty(chosenPath);
          });

      if (_pathBrowserCloseWatcher != null)
        _pathBrowserCloseWatcher.Dispose();
      _pathBrowserCloseWatcher = new PathBrowserCloseWatcher(this, dialogHandle, chosenPath => WorkingDirectory = LocalFsResourceProviderBase.ToDosPath(chosenPath), null);
    }

    public void UpdateLibRetroSettings()
    {
      if (_libRetroProxy == null)
      {
        _libRetroProxy = new LibRetroProxy(LocalFsResourceProviderBase.ToDosPath(PathBrowser.ChoosenResourcePath));
        _libRetroProxy.Init();
      }
    }

    #endregion

    #region Protected Methods
    protected void InitialiseProperties()
    {
      if (_configuration == null)
        return;
      if (_configuration.IsNative)
        EmulatorType = EmulatorType.Native;
      else if (_configuration.IsLibRetro)
        EmulatorType = EmulatorType.LibRetro;
      else
        EmulatorType = EmulatorType.Emulator;
      Name = _configuration.Name;
      if (!string.IsNullOrEmpty(_configuration.Path))
        PathBrowser.ChoosenResourcePath = LocalFsResourceProviderBase.ToResourcePath(_configuration.Path);
      Arguments = _configuration.Arguments;
      WorkingDirectory = _configuration.WorkingDirectory;
      UseQuotes = _configuration.UseQuotes;
      ExitsOnEscapeKey = _configuration.ExitsOnEscapeKey;
      FileExtensions = new HashSet<string>(_configuration.FileExtensions);
      SelectedGameCategories = new List<string>(_configuration.Platforms);
      UpdateIsGameCategoriesSelected();
    }

    protected void OnEmulatorNameChanged(AbstractProperty property, object oldValue)
    {
      UpdateIsEmulatorNameValid();
    }

    protected void OnCategorySelectionChanged(AbstractProperty property, object oldValue)
    {
      UpdateSelectedGameCategories();
      UpdateIsGameCategoriesSelected();
    }

    protected void SetDefaultNativeSettings()
    {
      EmulatorConfiguration native = DefaultConfigurations.NativeConfiguration;
      Name = native.Name;
      FileExtensions = new HashSet<string>(native.FileExtensions);
      SelectedGameCategories = new List<string>(native.Platforms);
      UpdateIsGameCategoriesSelected();
    }

    protected void SetDefaultEmulatorSettings()
    {
      EmulatorConfiguration defaultConfiguration;
      if (!DefaultConfigurations.TryMatch(PathBrowser.ChoosenResourcePathDisplayName, out defaultConfiguration))
      {
        Name = DosPathHelper.GetFileNameWithoutExtension(PathBrowser.ChoosenResourcePathDisplayName);
        return;
      }
      Name = defaultConfiguration.Name;
      Arguments = defaultConfiguration.Arguments;
      WorkingDirectory = defaultConfiguration.WorkingDirectory;
      UseQuotes = defaultConfiguration.UseQuotes;
      ExitsOnEscapeKey = defaultConfiguration.ExitsOnEscapeKey;
      FileExtensions = new HashSet<string>(defaultConfiguration.FileExtensions);
      SelectedGameCategories = new List<string>(defaultConfiguration.Platforms);
      UpdateIsGameCategoriesSelected();
    }

    protected void SetDefaultLibRetroSettings()
    {
      _libRetroProxy = new LibRetroProxy(LocalFsResourceProviderBase.ToDosPath(PathBrowser.ChoosenResourcePath));
      if (_libRetroProxy.Init())
      {
        Name = _libRetroProxy.Name;
        FileExtensions = new HashSet<string>(_libRetroProxy.Extensions);
      }
    }
    
    protected void UpdateIsEmulatorNameValid()
    {
      IsEmulatorNameValid = !string.IsNullOrEmpty(Name);
    }

    protected IEnumerable<string> GetGameMediaCategories()
    {
      return TheGamesDbWrapperV2.Platforms.Select(p => p.Name);
    }

    protected void UpdateSelectedGameCategories()
    {
      SelectedGameCategories = _gameCategories.Where(l => l.Selected).Select(l => l[Consts.KEY_NAME]).ToList();
    }

    protected void UpdateIsGameCategoriesSelected()
    {
      IsGameCategoriesSelected = SelectedGameCategories.Count > 0;
    }
    #endregion
  }
}
