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

using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using MediaPortal.Frontend.ViewModels;
using MediaPortal.Frontend.Workflow;

namespace MediaPortal.Frontend.Views
{
  /// <summary>
  /// Which watched place the weather wall speaks for, asked on the skin's own dialog frame over the wall
  /// that stood there. It is a dialog of the same making as the household's wall of faces - it wears the
  /// worn skin's own dialog frame and the dialog ground that washes the screen behind it, and the shell
  /// throws that screen out of focus for the length of the question - because which place the weather means
  /// is a question about the wall, not a place the viewer walked to.
  /// </summary>
  /// <remarks>
  /// <para>
  /// The dialog answers to the weather screen's own model, which the frame carries: the picked place is
  /// handed straight to that model, whose wall changes under the dialog on its way out. Adding, removing and
  /// the API key are no part of this question - they stand behind the settings door now - so the dialog
  /// shows the watched places as they are and nothing else.
  /// </para>
  /// <para>
  /// The keyboard belongs to the places while the dialog stands, which is why <see cref="OnScreenActivated"/>
  /// stands on the first of them: a list that opens with nothing stood on answers no Enter. The back key is
  /// answered here rather than left to the shell so that the wall keeps its place and hears that nobody was
  /// picked.
  /// </para>
  /// </remarks>
  public partial class WeatherPlacePickerView : UserControl, IWorkflowScreen, IDialogScreen
  {
    private readonly WeatherViewModel? _model;
    private readonly WorkflowManager? _workflow;

    /// <summary>For the XAML loader and the designer.</summary>
    public WeatherPlacePickerView()
        : this(null, null)
    {
    }

    public WeatherPlacePickerView(WeatherViewModel? model, WorkflowManager? workflow)
    {
      _model = model;
      _workflow = workflow;
      DataContext = model;
      InitializeComponent();

      // Enter takes the place stood on, like the face wall above the wake; the dialog goes first and the
      // wall hears second, so the read of the new place starts with no question left on the stack.
      placesList.AddHandler(KeyDownEvent,
          delegate (object? sender, KeyEventArgs args)
          {
            if (args.Key != Key.Enter && args.Key != Key.Return)
              return;
            if (placesList.SelectedItem is WeatherPlaceRow place)
            {
              args.Handled = true;
              Take(place);
            }
          },
          RoutingStrategies.Tunnel);
      placesList.DoubleTapped += delegate
      {
        if (placesList.SelectedItem is WeatherPlaceRow place)
          Take(place);
      };

      // The button is walkable from the list and back: Down at the last place steps onto it, Up on it steps
      // back to the places. Without that the button would be furniture only the mouse knows.
      placesList.AddHandler(KeyDownEvent,
          delegate (object? sender, KeyEventArgs args)
          {
            if (args.Key == Key.Down && placesList.SelectedIndex >= placesList.ItemCount - 1)
            {
              args.Handled = true;
              okButton.Focus();
            }
          },
          RoutingStrategies.Tunnel);
      okButton.AddHandler(KeyDownEvent,
          delegate (object? sender, KeyEventArgs args)
          {
            if (args.Key != Key.Up)
              return;
            args.Handled = true;
            placesList.Focus();
          },
          RoutingStrategies.Tunnel);

      // The back key has an honest meaning over a list of places: nobody was picked, and the wall keeps the
      // place it had. It is answered on the way in, before the list - which would only walk a selection
      // about - and before the shell, which would pop the dialog without ever telling the wall.
      AddHandler(KeyDownEvent,
          delegate (object? sender, KeyEventArgs args)
          {
            if (!Settings.KeyBindings.Current.Is(Settings.KeyAction.Back, args.Key)
                && args.Key != Key.Escape && args.Key != Key.Back)
              return;

            args.Handled = true;
            Close();
          },
          RoutingStrategies.Tunnel);
    }

    /// <summary>
    /// Where the dialog's keys start: the first watched place, stood on and looked at. Asked again once the
    /// layout has settled, because the places come out of the skin's dialog frame, which arrives a pass
    /// after the call.
    /// </summary>
    public void OnScreenActivated()
    {
      void Ask()
      {
        if (placesList.ItemCount == 0)
        {
          okButton.Focus();
          return;
        }

        if (placesList.SelectedIndex < 0)
          placesList.SelectedIndex = 0;
        placesList.Focus();
      }

      Ask();
      Dispatcher.UIThread.Post(Ask, DispatcherPriority.Loaded);
    }

    /// <summary>
    /// Runs once, when the frame leaves the workflow for good. The model belongs to the weather screen this
    /// dialog was raised over - it survives the dialog, so nothing here disposes it.  See IWorkflowScreen.
    /// </summary>
    public void OnScreenClosed()
    {
    }

    /// <summary>The button's answer: the stood-on place, or the first one if nothing stands.</summary>
    private void OnOkPress(object? sender, RoutedEventArgs args)
    {
      WeatherPlaceRow? place = placesList.SelectedItem as WeatherPlaceRow
          ?? placesList.Items.OfType<WeatherPlaceRow>().FirstOrDefault();
      if (place != null)
        Take(place);
    }

    /// <summary>A place was picked: the dialog steps aside and the wall starts speaking its weather.</summary>
    private void Take(WeatherPlaceRow place)
    {
      Close();
      _model?.ChoosePlace(place);
    }

    /// <summary>
    /// Takes the dialog away. Pops only its own stop, because the screen it was raised over stays where it
    /// is and a pop asked for later must not walk past it.
    /// </summary>
    private void Close()
    {
      WorkflowFrame? current = _workflow?.Current;
      if (current != null && current.StateId == "weatherplaces")
        _workflow?.Pop();
    }
  }
}
