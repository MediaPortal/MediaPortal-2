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
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using MediaPortal.Frontend.Input;
using MediaPortal.Frontend.ViewModels;

namespace MediaPortal.Frontend.Views
{
  /// <summary>
  /// The server's users as a screen: the list of names, the editor beside it - PIN, picture, age ceiling,
  /// groups, folders - and the house rules the server keeps for every television. Built for the settings hub
  /// the way the other server screens are - the hub embeds it, and every change is written to the server
  /// where it is answered.
  /// </summary>
  public partial class UsersSettingsView : UserControl
  {
    private readonly List<Control> _ring = new List<Control>();

    /// <summary>For the XAML loader, the designer and the hub, which sets the real view model as DataContext.</summary>
    public UsersSettingsView()
    {
      InitializeComponent();

      _ring.Add(usersList);
      _ring.Add(nameBox);
      _ring.Add(passwordBox);
      _ring.Add(kindBox);
      _ring.Add(addButton);
      _ring.Add(saveButton);
      _ring.Add(removePasswordButton);
      _ring.Add(removeButton);
      _ring.Add(pickPictureButton);
      _ring.Add(removePictureButton);
      _ring.Add(restrictAgesBox);
      _ring.Add(ageBox);
      _ring.Add(parentGuidedBox);
      _ring.Add(unratedBox);
      _ring.Add(restrictGroupsBox);
      _ring.Add(restrictSharesBox);
      _ring.Add(askLoginBox);
      _ring.Add(showLoginScreenBox);
      _ring.Add(autoLoginBox);
      _ring.Add(autoLogoutBox);
      _ring.Add(timeoutBox);
      _ring.Add(savePolicyButton);
      _ring.Add(askAgainButton);
      _ring.Add(reloadButton);
      DpadNavigation.Attach(this, _ring);

      // Opening the section reads the list and the house rules, exactly like the server-settings row asks
      // when it is opened - there is nothing local to show before that. The model is read from the
      // DataContext at that moment because it is the hub that hands it over.
      Loaded += delegate
      {
        UsersSettingsViewModel? model = DataContext as UsersSettingsViewModel;
        if (model != null && model.Users.Count == 0)
          _ = model.LoadAsync();
      };

      usersList.KeyDown += OnListKeyDown;
    }

    /// <summary>Enter on a name works the row it stands on; on a button it presses it, which the ring does.</summary>
    private void OnListKeyDown(object? sender, KeyEventArgs e)
    {
      if (e.Key == Key.Enter || e.Key == Key.Return)
      {
        UsersSettingsViewModel? model = DataContext as UsersSettingsViewModel;
        if (usersList.SelectedItem == null)
          _ = model?.LoadAsync();
        e.Handled = true;
      }
    }

    /// <summary>
    /// Asks the operating system for a picture file and hands the path to the model, which shrinks it the
    /// reference's way and waits for the save press before the server ever sees it. The dialog is the
    /// window's business, which is why this press is the one on this screen that is not a command.
    /// </summary>
    private async void OnPickPicture(object? sender, RoutedEventArgs e)
    {
      UsersSettingsViewModel? model = DataContext as UsersSettingsViewModel;
      TopLevel? top = TopLevel.GetTopLevel(this);
      if (model == null || top == null)
        return;

      IReadOnlyList<IStorageFile> picked = await top.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
      {
        Title = "Choose a picture for this user",
        AllowMultiple = false,
        FileTypeFilter = new[]
        {
          new FilePickerFileType("Pictures") { Patterns = new[] { "*.png", "*.jpg", "*.jpeg", "*.bmp", "*.gif", "*.webp" } }
        }
      });
      if (picked.Count == 0)
        return;

      string? path = picked[0].Path?.LocalPath;
      if (!string.IsNullOrEmpty(path))
        model.PictureChosen(path);
    }
  }
}
