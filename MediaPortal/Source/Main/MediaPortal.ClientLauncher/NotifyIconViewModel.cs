using MediaPortal.Utilities.SystemAPI;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using System.Windows.Input;
using Application = System.Windows.Application;

namespace MediaPortal.Client.Launcher
{
  /// <summary>
  /// Provides bindable properties and commands for the NotifyIcon. In this sample, the
  /// view model is assigned to the NotifyIcon in XAML. Alternatively, the startup routing
  /// in App.xaml.cs could have created this view model, and assigned it to the NotifyIcon.
  /// </summary>
  public class NotifyIconViewModel : INotifyPropertyChanged
  {
    #region Static fields

    private bool _isAutoStartEnabled;

    #endregion

    #region Properties

    private bool IsAutoStartEnabled
    {
      get => _isAutoStartEnabled;
      set
      {
        _isAutoStartEnabled = value;
        OnPropertyChanged();
      }
    }

    //private bool UseX64
    //{
    //  get { return ServiceRegistration.Get<ISettingsManager>().Load<ClientLauncherSettings>().UseX64; }
    //  set
    //  {
    //    var settingsManager = ServiceRegistration.Get<ISettingsManager>();
    //    var clientLauncherSettings = settingsManager.Load<ClientLauncherSettings>();
    //    clientLauncherSettings.UseX64 = value;
    //    settingsManager.Save(clientLauncherSettings);
    //    OnPropertyChanged();
    //  }
    //}

    #endregion

    #region Constructor

    public NotifyIconViewModel()
    {
      IsAutoStartEnabled = !string.IsNullOrEmpty(WindowsAPI.GetAutostartApplicationPath(ApplicationLauncher.AUTOSTART_REGISTER_NAME, true));
    }

    #endregion

    #region Commands to invoke from XAML

    public ICommand StartClientCommand => new DelegateCommand { CommandAction = ApplicationLauncher.StartClient };

    public ICommand RemoveFromAutoStartCommand
    {
      get
      {
        return new DelegateCommand
        {
          CanExecuteFunc = () => IsAutoStartEnabled,
          CommandAction = () =>
          {
            IsAutoStartEnabled = false;
            ApplicationLauncher.WriteAutostartAppEntryInRegistry(IsAutoStartEnabled);
          }
        };
      }
    }

    public ICommand AddToAutoStartCommand
    {
      get
      {
        return new DelegateCommand
        {
          CanExecuteFunc = () => !IsAutoStartEnabled,
          CommandAction = () =>
          {
            IsAutoStartEnabled = true;
            ApplicationLauncher.WriteAutostartAppEntryInRegistry(IsAutoStartEnabled);
          }
        };
      }
    }

    //public ICommand PreferX64Command
    //{
    //  get
    //  {
    //    return new DelegateCommand
    //    {
    //      CanExecuteFunc = () => SupportsX64,
    //      CommandAction = () => { UseX64 = !UseX64; }
    //    };
    //  }
    //}

    /// <summary>
    /// Shuts down the application.
    /// </summary>
    public ICommand ExitApplicationCommand
    {
      get
      {
        return new DelegateCommand
        {
          CommandAction = () =>
          {
            Application.Current.Shutdown();
          }
        };
      }
    }

    #endregion

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
  }
}
