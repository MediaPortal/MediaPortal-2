#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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
using System.Collections.Generic;
using System.Linq;
using MediaPortal.Common;
using MediaPortal.Common.Commands;
using MediaPortal.Common.General;
using MediaPortal.Common.Logging;
using MediaPortal.Common.Messaging;
using MediaPortal.Common.PluginManager;
using MediaPortal.Common.PluginManager.Exceptions;
using MediaPortal.Common.Settings;
using MediaPortal.UI.Presentation;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Screens;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UI.SkinEngine.ScreenManagement;
using SkinSettings.Settings;

namespace SkinSettings
{
  public enum LayoutType
  {
    ListLayout,
    GridLayout,
    CoverLayout,
  }

  public class WorkflowStateViewModeModel : IObservable
  {
    #region Consts

    public const string VM_MODEL_ID_STR = "08BB1CFE-8AF3-4DD1-BB9C-582DD7EA8BBF";
    public static Guid VM_MODEL_ID = new Guid(VM_MODEL_ID_STR);
    public const string KEY_NAME = "Name";
    public const string KEY_LAYOUT_TYPE = "LayoutType";

    #endregion

    protected static readonly object _syncObj = new object();
    protected static IPluginItemStateTracker _pluginItemStateTracker;
    protected readonly AbstractProperty _layoutTypeProperty;
    protected readonly ItemsList _viewModeItemsList = new ItemsList();
    private static Dictionary<string, Dictionary<Guid, List<LayoutType>>> _viewModes;
    protected WeakEventMulticastDelegate _objectChanged = new WeakEventMulticastDelegate();
    protected AsynchronousMessageQueue _messageQueue;

    public static int GetNumberOfViewModes()
    {
      var skinName = ((ScreenManager)ServiceRegistration.Get<IScreenManager>()).SkinName;
      var wfState = ServiceRegistration.Get<IWorkflowManager>().CurrentNavigationContext.WorkflowState.StateId;

      Dictionary<Guid, List<LayoutType>> views;
      List<LayoutType> viewModes;
      if (!_viewModes.TryGetValue(skinName, out views) || !views.TryGetValue(wfState, out viewModes) || !viewModes.Any())
        return 0;
      return viewModes.Count;
    }

    static WorkflowStateViewModeModel()
    {
      InitRegisteredViewModes();
    }

    public WorkflowStateViewModeModel()
    {
      _layoutTypeProperty = new WProperty(typeof(LayoutType), LayoutType.ListLayout);

      _messageQueue = new AsynchronousMessageQueue(this, new string[] { WorkflowManagerMessaging.CHANNEL });
      _messageQueue.MessageReceived += OnMessageReceived;
      _messageQueue.Start();

      FireChange();
    }

    private void OnMessageReceived(AsynchronousMessageQueue queue, SystemMessage message)
    {
      if (message.ChannelName == WorkflowManagerMessaging.CHANNEL)
      {
        Update();
      }
    }

    protected static void InitRegisteredViewModes()
    {
      lock (_syncObj)
      {
        if (_pluginItemStateTracker == null)
          _pluginItemStateTracker = new FixedItemStateTracker("WorkflowStates - ViewModes registration");
        // Key: Skin name, Key 2: WorkflowStateId, Value: LayoutTypes
        var viewModes = new Dictionary<string, Dictionary<Guid, List<LayoutType>>>(StringComparer.InvariantCultureIgnoreCase);

        IPluginManager pluginManager = ServiceRegistration.Get<IPluginManager>();
        foreach (PluginItemMetadata itemMetadata in pluginManager.GetAllPluginItemMetadata(WorkflowStateViewModesBuilder.WF_VIEWMODES_PROVIDER_PATH))
        {
          try
          {
            WorkflowStateViewModesRegistration providerRegistration = pluginManager.RequestPluginItem<WorkflowStateViewModesRegistration>(
              WorkflowStateViewModesBuilder.WF_VIEWMODES_PROVIDER_PATH, itemMetadata.Id, _pluginItemStateTracker);
            if (providerRegistration == null)
              ServiceRegistration.Get<ILogger>().Warn("Could not instantiate WorkflowState ViewModes registration with id '{0}'", itemMetadata.Id);
            else
            {
              if (!viewModes.ContainsKey(providerRegistration.Skin))
                viewModes.Add(providerRegistration.Skin, new Dictionary<Guid, List<LayoutType>>());

              var skinDict = viewModes[providerRegistration.Skin];

              if (skinDict.ContainsKey(providerRegistration.StateId))
              {
                ServiceRegistration.Get<ILogger>().Warn("Could not add ViewModes for WorkflowState '{0}'. The ID is already in defined.", providerRegistration.StateId);
                continue;
              }

              skinDict[providerRegistration.StateId] = providerRegistration.ViewModes.Split(',').Select(v => (LayoutType)Enum.Parse(typeof(LayoutType), v)).ToList();
              ServiceRegistration.Get<ILogger>().Info("Successfully added ViewModes for Skin '{0}' for WorkflowState ID '{1}', Modes: {2}",
                providerRegistration.Skin, providerRegistration.StateId, providerRegistration.ViewModes);
            }
          }
          catch (PluginInvalidStateException e)
          {
            ServiceRegistration.Get<ILogger>().Warn("Cannot add Skin Settings registration with id '{0}'", e, itemMetadata.Id);
          }
        }

        _viewModes = viewModes;
      }
    }

    public event ObjectChangedDlgt ObjectChanged
    {
      add { _objectChanged.Attach(value); }
      remove { _objectChanged.Detach(value); }
    }

    public void FireChange()
    {
      _objectChanged.Fire(new object[] { this });
    }

    public void Update()
    {
      var skinName = ((ScreenManager)ServiceRegistration.Get<IScreenManager>()).SkinName;
      var wfState = ServiceRegistration.Get<IWorkflowManager>().CurrentNavigationContext.WorkflowState.StateId;

      _viewModeItemsList.Clear();
      Dictionary<Guid, List<LayoutType>> views;
      List<LayoutType> viewModes;
      if (!_viewModes.TryGetValue(skinName, out views) || !views.TryGetValue(wfState, out viewModes) || !viewModes.Any())
        return;

      foreach (LayoutType layoutType in viewModes)
      {
        ListItem smallList = new ListItem(KEY_NAME, "[SkinSettings.ViewModes.LayoutType." + layoutType + "]")
        {
          Command = new MethodDelegateCommand(() => SetViewMode(layoutType)),
        };
        smallList.AdditionalProperties[KEY_LAYOUT_TYPE] = layoutType;
        _viewModeItemsList.Add(smallList);
      }

      var viewSettings = ServiceRegistration.Get<ISettingsManager>().Load<ViewSettings>();

      ViewModeDictionary<Guid, LayoutType> layouts;
      LayoutType layout;
      if (viewSettings.WorkflowLayouts.TryGetValue(skinName, out layouts) && layouts.TryGetValue(wfState, out layout))
        LayoutType = layout;
      else
        LayoutType = viewModes.First();
    }

    protected void SetViewMode(LayoutType layoutType)
    {
      LayoutType = layoutType;

      var skinName = ((ScreenManager)ServiceRegistration.Get<IScreenManager>()).SkinName;
      var wfState = ServiceRegistration.Get<IWorkflowManager>().CurrentNavigationContext.WorkflowState.StateId;
      var settingsManager = ServiceRegistration.Get<ISettingsManager>();
      var viewSettings = settingsManager.Load<ViewSettings>();
      if (!viewSettings.WorkflowLayouts.ContainsKey(skinName))
        viewSettings.WorkflowLayouts[skinName] = new ViewModeDictionary<Guid, LayoutType>();
      viewSettings.WorkflowLayouts[skinName][wfState] = layoutType;
      settingsManager.Save(viewSettings);
    }

    protected void UpdateSelectedFlag(ItemsList itemsList)
    {
      foreach (ListItem item in itemsList)
      {
        object layout;
        if (item.AdditionalProperties.TryGetValue(KEY_LAYOUT_TYPE, out layout))
          item.Selected = LayoutType.Equals(layout);
      }
    }

    #region Members to be accessed from the GUI

    public ItemsList ViewModeItemsList
    {
      get
      {
        UpdateSelectedFlag(_viewModeItemsList);
        return _viewModeItemsList;
      }
    }

    public AbstractProperty LayoutTypeProperty
    {
      get { return _layoutTypeProperty; }
    }

    public LayoutType LayoutType
    {
      get { return (LayoutType)_layoutTypeProperty.GetValue(); }
      set { _layoutTypeProperty.SetValue(value); }
    }

    #endregion
  }
}
