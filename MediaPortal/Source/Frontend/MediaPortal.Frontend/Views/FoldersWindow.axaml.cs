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
using System.Collections.Specialized;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using MediaPortal.Frontend.Input;
using MediaPortal.Frontend.ViewModels;

namespace MediaPortal.Frontend.Views
{
  /// <summary>
  /// The screen that says where the library comes from.
  /// </summary>
  /// <remarks>
  /// What only a screen can add, and what the view model deliberately does not know about: the folders are read
  /// when the screen opens rather than when somebody presses a button, the arrow keys walk the controls in the
  /// order they are drawn, Enter in the path field adds the folder, Enter on a row walks across to the buttons
  /// that act on it, and Back closes the screen and leaves the library where it was.
  /// </remarks>
  public partial class FoldersWindow : Window
  {
    private readonly FoldersViewModel _folders;
    private readonly List<Control> _ring = new List<Control>();
    private readonly List<Control> _categoryBoxes = new List<Control>();

    /// <summary>
    /// For the XAML loader and the designer: the screen a front end with no server shows.
    /// </summary>
    public FoldersWindow()
        : this(new FoldersViewModel())
    {
    }

    public FoldersWindow(FoldersViewModel folders)
    {
      DataContext = folders;
      _folders = folders;
      InitializeComponent();

      // In the order they are drawn: the list, the three things to do with a row, the field, the media types that
      // arrive later (see <see cref="RegisterCategoryBoxes"/>), and the button that finishes the job.
      _ring.Add(foldersList);
      _ring.Add(reimportButton);
      _ring.Add(removeButton);
      _ring.Add(refreshButton);
      _ring.Add(folderPathBox);
      _ring.Add(addButton);
      DpadNavigation.Attach(this, _ring);

      Opened += delegate
      {
        // The same command the button on the screen runs, so opening and pressing cannot drift apart.
        _folders.RefreshCommand.Execute(null);
        _folders.Categories.CollectionChanged += OnCategoriesChanged;
        RegisterCategoryBoxes();
      };

      Closed += delegate
      {
        _folders.Categories.CollectionChanged -= OnCategoriesChanged;
        // The screen owns the watching of the queue only while it is on screen.
        _folders.Dispose();
      };

      AddHandler(KeyDownEvent,
          delegate (object? sender, KeyEventArgs args)
          {
            if (args.Key == Key.Escape || args.Key == Key.Back)
            {
              Close();
              args.Handled = true;
              return;
            }

            if (args.Key != Key.Enter && args.Key != Key.Return)
              return;

            if (folderPathBox.IsFocused)
            {
              // Enter where the words were typed is the confirmation of those words, which is also what the
              // button underneath them does.
              _folders.AddCommand.Execute(null);
              args.Handled = true;
            }
            else if (foldersList.IsFocused)
            {
              // A folder is not opened, it is acted on, and the acting is across on the right. Walking there is
              // what the remote needs; taking the row's Enter away from it would leave the buttons unreachable
              // without a mouse.
              if (reimportButton.Focus())
                args.Handled = true;
            }
          },
          RoutingStrategies.Tunnel);

      // Up and down leave the path field. It keeps left and right for the caret - one wrong character should not
      // mean retyping a whole UNC path - but a vertical caret is useless and a remote has no Tab to fall back on.
      AddHandler(KeyDownEvent,
          delegate (object? sender, KeyEventArgs args)
          {
            if (!folderPathBox.IsFocused || (args.Key != Key.Up && args.Key != Key.Down))
              return;

            Control? next = DpadNavigation.NextReachableInRing(_ring, args.Key);
            if (next != null && next.Focus())
              args.Handled = true;
          },
          RoutingStrategies.Tunnel);
    }

    private void OnCategoriesChanged(object? sender, NotifyCollectionChangedEventArgs args)
    {
      // The boxes do not exist until the server has answered, so the ring is rebuilt after the layout has had a
      // turn; registering them during the collection callback would look for containers that were never made.
      Dispatcher.UIThread.Post(RegisterCategoryBoxes, DispatcherPriority.Loaded);
    }

    /// <summary>
    /// Puts the media-type boxes into the arrow ring, between the path field and the add button.
    /// </summary>
    /// <remarks>
    /// They are generated from the categories the server reported, so their number is not known when the screen
    /// is built, and a screen whose tick boxes cannot be reached with the arrow keys is a screen that cannot be
    /// used with a remote - which is the only thing this front end is used with.
    /// </remarks>
    private void RegisterCategoryBoxes()
    {
      foreach (Control stale in _categoryBoxes)
        _ring.Remove(stale);
      _categoryBoxes.Clear();

      int after = _ring.IndexOf(folderPathBox);
      int at = after < 0 ? _ring.Count : after + 1;
      // Walked from the window rather than from the list: the boxes are the only check boxes the screen has, and
      // the list's own containers are not realised until it has been measured.
      foreach (CheckBox box in this.GetVisualDescendants().OfType<CheckBox>())
      {
        _ring.Insert(at++, box);
        _categoryBoxes.Add(box);
      }
    }
  }
}
