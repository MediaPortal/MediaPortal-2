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

using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using MediaPortal.Frontend.ViewModels;
using MediaPortal.Frontend.Views;

namespace MediaPortal.Frontend
{
  /// <summary>
  /// The application object: it decides which screen is shown and which server that screen talks to, and does
  /// nothing else.
  /// </summary>
  public partial class App : Application
  {
    private readonly HomeViewModel _home;

    /// <summary>
    /// For the XAML tooling and for a client built without a server to talk to.
    /// </summary>
    public App()
        : this(new HomeViewModel())
    {
    }

    /// <summary>
    /// The constructor the program and the tests use. Handing in the view model rather than building it here is
    /// what lets a headless test show the real screen with a client pointed at a real server.
    /// </summary>
    public App(HomeViewModel home)
    {
      _home = home;
    }

    public override void Initialize()
    {
      AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
      if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
      {
        desktop.MainWindow = new HomeWindow(_home);
        // Reading straight away, so that the first thing a viewer sees after the splash is the library rather
        // than a screen waiting for a key. Failures land in the status line, not in a dialog.
        _home.RefreshCommand.Execute(null);
      }

      base.OnFrameworkInitializationCompleted();
    }
  }
}
