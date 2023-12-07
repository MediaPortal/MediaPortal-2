#region Copyright (C) 2007-2021 Team MediaPortal

/*
    Copyright (C) 2007-2021 Team MediaPortal
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

namespace MP2BootstrapperApp.WizardSteps
{
  /// <summary>
  /// Interface for a class that represents a single step in a wizard.
  /// </summary>
  public interface IStep
  {
    /// <summary>
    /// Gets the step that should be shown after this step.
    /// </summary>
    /// <returns>The next step to be shown.</returns>
    IStep Next();

    /// <summary>
    /// Gets whether this step can proceed to the next step.
    /// </summary>
    /// <returns><c>true</c> if the next step can be proceeded to; else <c>false</c>.</returns>
    bool CanGoNext();

    /// <summary>
    /// Gets whether this step can go back to the previous step.
    /// </summary>
    /// <returns><c>true</c> if the previous step can be reverted to; else <c>false</c>.</returns>
    bool CanGoBack();
  }

  /// <summary>
  /// Interface for a class that represents the final step in a wizard.
  /// </summary>
  public interface IFinalStep : IStep
  {
  }

  /// <summary>
  /// Interface for a transient step in a wizard, this step will not remain in the navigation stack when the next step is shown.
  /// </summary>
  public interface ITransientStep : IStep
  {
  }
}
