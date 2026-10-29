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

using System.Collections.Generic;
using Avalonia.Controls;
using MediaPortal.Frontend.Api;
using MediaPortal.Frontend.Input;
using MediaPortal.Frontend.Settings;
using MediaPortal.Frontend.ViewModels;

namespace MediaPortal.Frontend.Views
{
  /// <summary>
  /// The weather's place list as a settings page: the watched places, the search that adds one, the key that
  /// takes one away, and the viewer's own API key. The places are read from their file as the page opens -
  /// not when the hub opens - so a hand-edited <c>weather-places.json</c> that was changed while the hub
  /// stood there is still what one sees.
  /// </summary>
  public partial class WeatherSettingsView : UserControl
  {
    private readonly List<Control> _ring = new List<Control>();
    private readonly WeatherPlacesEditorViewModel? _model;

    /// <summary>For the XAML loader and the designer.</summary>
    public WeatherSettingsView()
        : this(null)
    {
    }

    /// <summary>
    /// Builds the page. The client may be left out, which is the same as saying "ask the endpoint as the
    /// saved file's key allows" - the way the settings hub builds every page it is asked for.
    /// </summary>
    public WeatherSettingsView(WeatherApiClient? client)
    {
      // The page hands its model the file to keep its answers in - the user's own - and the client to ask
      // the geocoder with. The key typed below rewrites the file right away; the client answering THIS page
      // keeps the key it was built with, which the page says out loud on the status line.
      WeatherPlacesEditorViewModel model = new WeatherPlacesEditorViewModel(
          client ?? new WeatherApiClient(userApiKey: WeatherPlacesSettings.Load(null, out _).ApiKey), null);
      _model = model;
      DataContext = model;

      InitializeComponent();

      _ring.Add(searchBox);
      _ring.Add(searchButton);
      _ring.Add(placesList);
      _ring.Add(removeButton);
      _ring.Add(foundList);
      _ring.Add(addButton);
      _ring.Add(apiKeyBox);
      DpadNavigation.Attach(this, _ring);
    }
  }
}
