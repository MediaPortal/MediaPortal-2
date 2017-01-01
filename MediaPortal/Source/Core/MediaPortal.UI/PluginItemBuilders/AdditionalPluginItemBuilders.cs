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

using MediaPortal.Common;
using MediaPortal.Common.PluginManager;
using MediaPortal.UI.Services.Players.Builders;
using MediaPortal.UI.Services.Workflow;

namespace MediaPortal.UI.PluginItemBuilders
{
  /// <summary>
  /// This class registers additional plugin item builders at the <see cref="IPluginManager">plugin manager</see> which
  /// are provided by the <c>MediaPortal.UI</c> project.
  /// </summary>
  public class AdditionalPluginItemBuilders
  {
    public const string BACKGROUND_BUILDER_NAME = "Background";
    public const string PLAYERBUILDER_BUILDER_NAME = "PlayerBuilder";
    public const string MODEL_BUILDER_NAME = "Model";
    public const string WORKFLOW_STATE_BUILDER_NAME = "WorkflowState";
    public const string DIALOG_STATE_BUILDER_NAME = "DialogState";

    public static void Register()
    {
      IPluginManager pluginManager = ServiceRegistration.Get<IPluginManager>();
      pluginManager.RegisterSystemPluginItemBuilder(BACKGROUND_BUILDER_NAME, new BackgroundBuilder());

      pluginManager.RegisterSystemPluginItemBuilder(PLAYERBUILDER_BUILDER_NAME, new PlayerBuilder());

      pluginManager.RegisterSystemPluginItemBuilder(MODEL_BUILDER_NAME, new ModelBuilder());

      pluginManager.RegisterSystemPluginItemBuilder(WORKFLOW_STATE_BUILDER_NAME, new WorkflowStateBuilder());

      pluginManager.RegisterSystemPluginItemBuilder(DIALOG_STATE_BUILDER_NAME, new WorkflowStateBuilder());
    }
  }
}