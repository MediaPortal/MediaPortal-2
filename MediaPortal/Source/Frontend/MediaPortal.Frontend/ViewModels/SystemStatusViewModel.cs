/*
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

using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediaPortal.Frontend.Api;

namespace MediaPortal.Frontend.ViewModels
{
  /// <summary>
  /// What this box and the server behind it are made of - the reference's system state menu, in the front
  /// end's own shape, and the version line the plan owed to "About". Everything on the screen is read from
  /// the running thing it names: the runtime's own account of machine, system and culture, this assembly's
  /// own version attribute, the heap the front end actually stands on, and what the server said about itself
  /// at <c>api/serverinfo</c> - or its refusal, quoted. Nothing estimated, nothing invented.
  /// </summary>
  /// <remarks>
  /// The worn skin arrives as a sentence from the application rather than being read here, because knowing
  /// which skin is on means asking the Avalonia application, and a view model does not go asking the toolkit
  /// what it is wearing. A memory counter that named percentages the runtime cannot honestly give would be
  /// a decoration; the heap number is small, true, and says what it is.
  /// </remarks>
  public partial class SystemStatusViewModel : ObservableObject
  {
    private readonly Func<IServerInfoApi?> _clientProvider;
    private readonly Func<string> _skinDescriber;

    public SystemStatusViewModel(Func<IServerInfoApi?> clientProvider, Func<string> skinDescriber)
    {
      _clientProvider = clientProvider;
      _skinDescriber = skinDescriber;
    }

    /// <summary>Label and value per line, as the screen shows them.</summary>
    public sealed record Line(string Label, string Value);

    public ObservableCollection<Line> Lines { get; } = new ObservableCollection<Line>();

    public string Heading => "System status";

    public string Subtitle =>
        "What this box and the server behind it are made of - read from each running thing as it stands, "
        + "not remembered from a previous start.";

    [ObservableProperty]
    private string _status = "";

    /// <summary>This assembly's version, the same number the server reports when it is the same build.</summary>
    public static string FrontendVersion
    {
      get
      {
        Assembly assembly = typeof(SystemStatusViewModel).Assembly;
        string? informational = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion;
        Version? simple = assembly.GetName().Version;
        if (!string.IsNullOrEmpty(informational) && informational!.Length <= 40)
          return informational!;
        return simple?.ToString() ?? "unknown";
      }
    }

    /// <summary>Rebuilds every line. Called when the section opens and by the reload button.</summary>
    [RelayCommand]
    public async Task ReloadAsync()
    {
      Lines.Clear();

      Lines.Add(new Line("This machine", Environment.MachineName));
      Lines.Add(new Line("System", RuntimeInformation.OSDescription + " (" +
          RuntimeInformation.OSArchitecture + ")"));
      Lines.Add(new Line("Frontend version", FrontendVersion));
      Lines.Add(new Line("Wearing", _skinDescriber()));
      Lines.Add(new Line("Language", CultureInfo.CurrentUICulture.DisplayName));
      Lines.Add(new Line("Frontend memory", HeapText()));
      Lines.Add(new Line("Machine running for", UptimeText()));

      IServerInfoApi? client = _clientProvider();
      if (client == null)
      {
        Lines.Add(new Line("Server", "no server address set - see the \"Server address\" row"));
        Status = "The machine's own lines; the server will join them when one is set.";
        return;
      }

      try
      {
        ServerInfo info = await client.GetServerInfoAsync();
        Lines.Add(new Line("Server", info.Name + " " + info.Version));
        if (!string.IsNullOrEmpty(info.Framework))
          Lines.Add(new Line("Server runtime", info.Framework!));
        if (!string.IsNullOrEmpty(info.OperatingSystem))
        {
          Lines.Add(new Line("Server system", info.OperatingSystem! +
              (string.IsNullOrEmpty(info.OsArchitecture) ? "" : " (" + info.OsArchitecture + ")")));
        }
        Lines.Add(new Line("Server process", "id " + info.ProcessId));
        Status = info.Version == FrontendVersion
            ? "Both machines report the same version."
            : "Frontend " + FrontendVersion + ", server " + info.Version + " - they do not report the same version.";
      }
      catch (Exception e)
      {
        Lines.Add(new Line("Server", "did not answer: " + e.Message));
        Status = "The server did not answer about itself.";
      }
    }

    /// <summary>The heap this front end stands on, as the garbage collector counts it.</summary>
    private static string HeapText()
    {
      GCMemoryInfo heap = GC.GetGCMemoryInfo();
      double used = heap.MemoryLoadBytes / (1024.0 * 1024.0);
      double total = heap.TotalAvailableMemoryBytes / (1024.0 * 1024.0);
      if (total <= 0)
        return used.ToString("0", CultureInfo.InvariantCulture) + " MB";
      return used.ToString("0", CultureInfo.InvariantCulture) + " MB used of "
          + total.ToString("0", CultureInfo.InvariantCulture) + " MB the collector sees";
    }

    /// <summary>How long this machine has been up - the runtime counts from boot on every platform.</summary>
    private static string UptimeText()
    {
      TimeSpan up = TimeSpan.FromMilliseconds(Environment.TickCount64);
      return up.Days > 0
          ? string.Format(CultureInfo.InvariantCulture, "{0} days {1:00}:{2:00}", up.Days, up.Hours, up.Minutes)
          : string.Format(CultureInfo.InvariantCulture, "{0:00}:{1:00} hours", up.Hours, up.Minutes);
    }
  }
}
