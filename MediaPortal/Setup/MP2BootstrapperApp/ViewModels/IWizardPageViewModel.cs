#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
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

namespace MP2BootstrapperApp.ViewModels
{
  /// <summary>
  /// Interface for a view model that displays an install wizard step.
  /// </summary>
  public interface IWizardPageViewModel
  {
    /// <summary>
    /// The header to display for the step.
    /// </summary>
    string Header { get; set; }

    /// <summary>
    /// The subheader to display for the step.
    /// </summary>
    string SubHeader { get; set; }

    /// <summary>
    /// The label to display in the Back button.
    /// </summary>
    string ButtonBackContent { get; set; }

    /// <summary>
    /// The label to display in the Cancel button.
    /// </summary>
    string ButtonCancelContent { get; set; }

    /// <summary>
    /// The label to display in the Next button.
    /// </summary>
    string ButtonNextContent { get; set; }

    /// <summary>
    /// Fired when the state of the Back, Cancel and/or Next buttons might have changed.
    /// </summary>
    event EventHandler ButtonStateChanged;

    /// <summary>
    /// Called when this view model is displayed. Implementations should attach any event handlers here. 
    /// </summary>
    void Attach();

    /// <summary>
    /// Called when this view model is removed from display. Implementations should detach any event handlers here. 
    /// </summary>
    void Detach();
  }
}
