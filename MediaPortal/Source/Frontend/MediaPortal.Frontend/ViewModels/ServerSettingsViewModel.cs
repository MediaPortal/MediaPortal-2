using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediaPortal.Frontend.Api;

namespace MediaPortal.Frontend.ViewModels
{
    /// <summary>
    /// The server's own settings, edited from the front end and stored on the server:
    /// whether it demands a login, when the importer re-scans by itself, and which
    /// address it listens on.
    /// </summary>
    /// <remarks>
    /// The row loads itself when it is opened - these values live on the other machine,
    /// so unlike the frontend sections there is nothing local to show first. If the
    /// server refuses (it demands a login this client doesn't have, or is an older build
    /// without this endpoint) the row says exactly that rather than showing an empty
    /// form that would save nonsense over whatever is really configured.
    /// <para>
    /// Saving sends every field back, not only the touched ones: the server overwrites
    /// its stored settings with what arrives. That also means two people editing at the
    /// same time overwrite each other - last save wins, with no merge attempted. Fine
    /// for a home media server, worth knowing regardless.
    /// </para>
    /// </remarks>
    public sealed partial class ServerSettingsViewModel : ObservableObject
    {
        private readonly Func<IServerSettingsApi?> _clientProvider;
        private bool _loaded;

        private bool _webAuth;
        private bool _importer;
        private int _importerHours = 2;
        private int _port = 55555;
        private bool _useIPv4 = true;
        private bool _useIPv6 = true;
        private bool _limitEndpoints;
        private string _bindings = "";
        private string _status = "";

        public ServerSettingsViewModel(Func<IServerSettingsApi?> clientProvider)
        {
            _clientProvider = clientProvider ?? throw new ArgumentNullException(nameof(clientProvider));
        }

        /// <summary>
        /// Whether the server wants a login for every call. Turning it on without having
        /// set up users is how people lock themselves out - the box says so, and the
        /// status line after a save repeats the warning.
        /// </summary>
        public bool WebAuth
        {
            get => _webAuth;
            set => SetProperty(ref _webAuth, value);
        }

        /// <summary>Whether the importer re-scans the shares on its own.</summary>
        public bool Importer
        {
            get => _importer;
            set => SetProperty(ref _importer, value);
        }

        /// <summary>Hours between automatic import passes; the server accepts 0-24.</summary>
        public int ImporterHours
        {
            get => _importerHours;
            set => SetProperty(ref _importerHours, value);
        }

        /// <summary>
        /// The port stored on the server. This build always listens on the shared port
        /// 55555, so a stored change here changes nothing - the view says so where the
        /// box is, not only in the status line.
        /// </summary>
        public int Port
        {
            get => _port;
            set => SetProperty(ref _port, value);
        }

        /// <summary>Answer on IPv4. Restart.</summary>
        public bool UseIPv4
        {
            get => _useIPv4;
            set => SetProperty(ref _useIPv4, value);
        }

        /// <summary>Answer on IPv6. Restart.</summary>
        public bool UseIPv6
        {
            get => _useIPv6;
            set => SetProperty(ref _useIPv6, value);
        }

        /// <summary>Bind only the addresses listed below. Restart.</summary>
        public bool LimitEndpoints
        {
            get => _limitEndpoints;
            set => SetProperty(ref _limitEndpoints, value);
        }

        /// <summary>Comma separated addresses to bind when limited. Restart.</summary>
        public string Bindings
        {
            get => _bindings;
            set => SetProperty(ref _bindings, value ?? "");
        }

        /// <summary>What the last load or save did.</summary>
        public string Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        /// <summary>
        /// The fields the server stored but could not apply. Filled after a save that
        /// changed any of them; the view shows it as one sentence naming them.
        /// </summary>
        public string RestartNote { get; private set; } = "";

        /// <summary>Read the server's settings. Called when the row is opened, and by ReloadCommand.</summary>
        [RelayCommand]
        public async Task ReloadAsync()
        {
            IServerSettingsApi? client = _clientProvider();
            if (client == null)
            {
                Status = "No server address yet - set one under \"Server address\" first.";
                return;
            }

            try
            {
                ServerSettingsAnswer answer = await client.GetServerSettingsAsync();
                Apply(answer);
                _loaded = true;
                Status = "Loaded from the server.";
            }
            catch (ServerApiException e)
            {
                Status = e.StatusCode == 404 || e.StatusCode == 405
                    ? "This server does not offer its settings over the API - it is probably an older build."
                    : "The server refused: " + e.Message;
            }
            catch (Exception e)
            {
                Status = "The server could not be reached: " + e.Message;
            }
        }

        /// <summary>Mark the row as already read without contacting the server (tests).</summary>
        public void LoadFrom(ServerSettingsAnswer answer)
        {
            Apply(answer);
            _loaded = true;
            Status = "Loaded from the server.";
        }

        /// <summary>Send everything back and have the server store it.</summary>
        /// <remarks>
        /// Refuses to save a row that never loaded: an all-defaults form sent to a server
        /// whose settings were never read would silently rewrite them.
        /// </remarks>
        [RelayCommand]
        public async Task SaveAsync()
        {
            if (!_loaded)
            {
                Status = "Nothing loaded from the server yet - reload first, then save.";
                return;
            }

            IServerSettingsApi? client = _clientProvider();
            if (client == null)
            {
                Status = "No server address yet.";
                return;
            }

            try
            {
                ServerSettingsAnswer answer = await client.PutServerSettingsAsync(new ServerSettingsUpdate
                {
                    WebAuthorizationEnabled = _webAuth,
                    HttpServerPort = _port,
                    UseIPv4 = _useIPv4,
                    UseIPv6 = _useIPv6,
                    LimitIpEndpoints = _limitEndpoints,
                    IpAddressBindings = _bindings,
                    ImporterEnabled = _importer,
                    ImporterStartTime = _importerHours
                });
                Apply(answer);

                RestartNote = answer.RestartNeededFor.Count == 0
                    ? ""
                    : "Stored, but only takes effect after the server restarts: "
                      + string.Join(", ", answer.RestartNeededFor) + ".";
                Status = RestartNote == ""
                    ? "Saved on the server."
                    : RestartNote;
                if (_webAuth)
                    Status += "  Warning: with login required, servers without configured users lock everyone out.";
            }
            catch (ServerApiException e)
            {
                Status = e.StatusCode == 400
                    ? "The server rejected the values: " + e.Message
                    : "The server refused: " + e.Message;
            }
            catch (Exception e)
            {
                Status = "The server could not be reached: " + e.Message;
            }
        }

        private void Apply(ServerSettingsAnswer answer)
        {
            _webAuth = answer.WebAuthorizationEnabled;
            OnPropertyChanged(nameof(WebAuth));
            _importer = answer.ImporterEnabled;
            OnPropertyChanged(nameof(Importer));
            // The server stores fractions of an hour; the row offers whole hours, so
            // show what the server's value rounds to rather than a false precision.
            _importerHours = (int)Math.Round(answer.ImporterStartTime, MidpointRounding.AwayFromZero);
            OnPropertyChanged(nameof(ImporterHours));
            _port = answer.HttpServerPort;
            OnPropertyChanged(nameof(Port));
            _useIPv4 = answer.UseIPv4;
            OnPropertyChanged(nameof(UseIPv4));
            _useIPv6 = answer.UseIPv6;
            OnPropertyChanged(nameof(UseIPv6));
            _limitEndpoints = answer.LimitIpEndpoints;
            OnPropertyChanged(nameof(LimitEndpoints));
            _bindings = answer.IpAddressBindings;
            OnPropertyChanged(nameof(Bindings));
        }
    }
}
