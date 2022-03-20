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

using MP2BootstrapperApp.WizardSteps;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MP2BootstrapperApp.ViewModels
{
  /// <summary>
  /// Uses reflection to determine the appropriate view model to use for a given <see cref="IStep"/>.
  /// </summary>
  public class WizardStepViewModelBuilder
  {
    IDictionary<Type, Type> _wizardStepToViewModelMap;

    public WizardStepViewModelBuilder()
    {
      LoadViewModelTypes();
    }

    void LoadViewModelTypes()
    {
      _wizardStepToViewModelMap = Assembly.GetExecutingAssembly().GetTypes()
        // All view models that inherit from InstallWizardPageViewModelBase
        .Where(t => typeof(IWizardPageViewModel).IsAssignableFrom(t) && !t.IsAbstract)
        // With a constructor that accepts a single parameter that implements IStep
        .Select(t => t.GetConstructors().FirstOrDefault(c => c.GetParameters().Length == 1 && typeof(IStep).IsAssignableFrom(c.GetParameters().Single().ParameterType)))
        .Where(c=>c != null)
        // To a dictionary that maps the IStep implementation type to the view model type
        .ToDictionary(c => c.GetParameters().First(p => typeof(IStep).IsAssignableFrom(p.ParameterType)).ParameterType, c => c.DeclaringType);
    }

    /// <summary>
    /// Gets a new instance of the view model that handles the specified <see cref="IStep"/>.
    /// </summary>
    /// <param name="step">The wizard step to get the view model for.</param>
    /// <returns>Implementation of <see cref="InstallWizardPageViewModelBase"/> that handles the specified step or <c>null</c> if no view model was found.</returns>
    /// <exception cref="ArgumentNullException">Thrown if the <paramref name="step"/> is <c>null</c>.</exception>
    /// <exception cref="InvalidOperationException">Thrown if no view model was found for the specified step.</exception>
    public IWizardPageViewModel GetViewModel(IStep step)
    {
      if (step == null)
        throw new ArgumentNullException(nameof(step));

      if (!_wizardStepToViewModelMap.TryGetValue(step.GetType(), out Type viewModelType))
        throw new InvalidOperationException($"No view model found for step {step.GetType()}");

      return Activator.CreateInstance(viewModelType, step) as IWizardPageViewModel;
    }
  }
}
