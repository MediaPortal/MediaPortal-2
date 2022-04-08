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

using MP2BootstrapperApp.BundlePackages.Plugins;
using MP2BootstrapperApp.Models;
using MP2BootstrapperApp.WizardSteps;
using System.Collections.Generic;
using System.Linq;

namespace MP2BootstrapperApp.ViewModels
{
  public class InstallCustomPluginsPageViewModel : AbstractSelectionViewModel<InstallCustomPluginsStep, SelectablePluginViewModel>
  {
    public InstallCustomPluginsPageViewModel(InstallCustomPluginsStep step)
      : base(step)
    {
      Header = "[InstallCustomPluginsPageView.Header]";
      SubHeader = "[InstallCustomPluginsPageView.SubHeader]";
    }

    protected override IEnumerable<SelectablePluginViewModel> GetItems()
    {
      return _step.AvailablePlugins.Select(p =>
      new SelectablePluginViewModel
      {
        Item = new PluginModel
        {
          Id = p.Id,
          DisplayName = p.Name,
          LocalizedDescription = $"[PluginDescription.{p.Id}]",
          ImagePath = $@"..\resources\{p.Id}.png"
        },
        Selected = _step.SelectedPlugins.Contains(p)
      });
    }

    protected override void OnItemSelectedChanged(SelectablePluginViewModel item, bool selected)
    {
      PluginBase plugin = _step.AvailablePlugins.FirstOrDefault(p => p.Id == item.Item.Id);
      if (plugin != null)
      {
        // If a new plugin has been selected, unselect any incompatible plugins
        if (selected)
          foreach (SelectablePluginViewModel incompatiblePlugin in Items.Where(p => p.Selected && plugin.ConflictsWith(p.Item.Id)))
            incompatiblePlugin.Selected = false;

        UpdateSelectedItems(plugin, _step.SelectedPlugins, selected);
      }

      base.OnItemSelectedChanged(item, selected);
    }
  }
}
