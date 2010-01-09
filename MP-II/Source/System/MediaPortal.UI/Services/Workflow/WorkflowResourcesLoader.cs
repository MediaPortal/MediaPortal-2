#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal II

    MediaPortal II is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal II is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Collections.Generic;
using System.Xml.XPath;
using MediaPortal.Core;
using MediaPortal.Core.Logging;
using MediaPortal.Core.Localization;
using MediaPortal.UI.Presentation.SkinResources;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.Utilities;

namespace MediaPortal.UI.Services.Workflow
{
  /// <summary>
  /// Class for loading MediaPortal-II workflow resources from the current skin context.
  /// </summary>
  public class WorkflowResourcesLoader
  {
    public const int WORKFLOW_RESOURCE_SPEC_VERSION_MAJOR = 1;
    public const int MIN_WORKFLOW_RESOURCE_SPEC_VERSION_MINOR = 0;

    public const string WORKFLOW_DIRECTORY = "workflow";

    protected IDictionary<Guid, WorkflowState> _states = new Dictionary<Guid, WorkflowState>();
    protected IDictionary<Guid, WorkflowAction> _menuActions = new Dictionary<Guid, WorkflowAction>();

    public IDictionary<Guid, WorkflowState> States
    {
      get { return _states; }
    }

    public IDictionary<Guid, WorkflowAction> MenuActions
    {
      get { return _menuActions; }
    }

    /// <summary>
    /// Loads workflow resources from files contained in the current skin context.
    /// </summary>
    /// be added.</param>
    public void Load()
    {
      _states.Clear();
      _menuActions.Clear();
      IDictionary<string, string> workflowResources = ServiceScope.Get<ISkinResourceManager>().
          SkinResourceContext.GetResourceFilePaths("^" + WORKFLOW_DIRECTORY + "\\\\.*\\.xml$");
      foreach (string workflowResourceFilePath in workflowResources.Values)
        LoadWorkflowResourceFile(workflowResourceFilePath);
    }

    protected void LoadWorkflowResourceFile(string filePath)
    {
      try
      {
        XPathDocument doc = new XPathDocument(filePath);
        XPathNavigator nav = doc.CreateNavigator();
        nav.MoveToChild(XPathNodeType.Element);
        if (nav.LocalName != "Workflow")
          throw new ArgumentException(
              "File is no workflow descriptor file (document element must be 'Workflow')");

        bool versionOk = false;
        XPathNavigator attrNav = nav.Clone();
        if (attrNav.MoveToFirstAttribute())
          do
          {
            switch (attrNav.Name)
            {
              case "DescriptorVersion":
                Versions.CheckVersionCompatible(attrNav.Value, WORKFLOW_RESOURCE_SPEC_VERSION_MAJOR, MIN_WORKFLOW_RESOURCE_SPEC_VERSION_MINOR);
                //string specVersion = attr.Value; <- if needed
                versionOk = true;
                break;
              default:
                throw new ArgumentException("'Workflow' element doesn't support an attribute '" + attrNav.Name + "'");
            }
          } while (attrNav.MoveToNextAttribute());
        if (!versionOk)
          throw new ArgumentException("'DescriptorVersion' attribute expected");

        XPathNavigator childNav = nav.Clone();
        if (childNav.MoveToChild(XPathNodeType.Element))
          do
          {
            switch (childNav.LocalName)
            {
              case "States":
                LoadStates(childNav.Clone());
                break;
              case "MenuActions":
                foreach (WorkflowAction action in LoadActions(childNav.Clone()))
                {
                  if (_menuActions.ContainsKey(action.ActionId))
                    throw new ArgumentException(string.Format(
                        "A menu action with id '{0}' was already registered with action name '{1}' (name of duplicate action is '{2}') -> Forgot to create a new GUID?",
                        action.ActionId, _menuActions[action.ActionId].Name, action.Name));
                  _menuActions.Add(action.ActionId, action);
                }
                break;
              default:
                throw new ArgumentException("'Workflow' element doesn't support a child element '" + childNav.Name + "'");
            }
          } while (childNav.MoveToNext(XPathNodeType.Element));
      }
      catch (Exception e)
      {
        ServiceScope.Get<ILogger>().Error("Error loading workflow resource file '" + filePath + "'", e);
      }
    }

    protected void LoadStates(XPathNavigator statesNav)
    {
      XPathNavigator childNav = statesNav.Clone();
      if (childNav.MoveToChild(XPathNodeType.Element))
        do
        {
          switch (childNav.LocalName)
          {
            case "WorkflowState":
              WorkflowState workflowState = LoadWorkflowState(childNav.Clone());
              if (_states.ContainsKey(workflowState.StateId))
                throw new ArgumentException(string.Format(
                    "A workflow or dialog state with id '{0}' was already declared with name '{1}' (name of duplicate state is '{2}') -> Forgot to create a new GUID?",
                    workflowState.StateId, _states[workflowState.StateId].Name, workflowState.Name));
              _states.Add(workflowState.StateId, workflowState);
              break;
            case "DialogState":
              WorkflowState dialogState = LoadDialogState(childNav);
              if (_states.ContainsKey(dialogState.StateId))
                throw new ArgumentException(string.Format(
                    "A workflow or dialog state with id '{0}' was already declared with name '{1}' (name of duplicate state is '{2}') -> Forgot to create a new GUID?",
                    dialogState.StateId, _states[dialogState.StateId].Name, dialogState.Name));
              _states.Add(dialogState.StateId, dialogState);
              break;
            default:
              throw new ArgumentException("'" + statesNav.Name + "' element doesn't support a child element '" + childNav.Name + "'");
          }
        } while (childNav.MoveToNext(XPathNodeType.Element));
    }

    protected static IEnumerable<WorkflowAction> LoadActions(XPathNavigator actionsNav)
    {
      XPathNavigator childNav = actionsNav.Clone();
      if (childNav.MoveToChild(XPathNodeType.Element))
        do
        {
          switch (childNav.LocalName)
          {
            case "PushNavigationTransition":
              yield return LoadPushNavigationTransition(childNav.Clone());
              break;
            case "PopNavigationTransition":
              yield return LoadPopNavigationTransition(childNav.Clone());
              break;
            case "WorkflowContributorAction":
              yield return LoadWorkflowContributorAction(childNav.Clone());
              break;
            // TODO: More actions - show screen, show dialog, call model method, ...
            default:
              throw new ArgumentException("'" + actionsNav.Name + "' element doesn't support a child element '" + childNav.Name + "'");
          }
        } while (childNav.MoveToNext(XPathNodeType.Element));
    }

    protected static WorkflowState LoadDialogState(XPathNavigator stateNav)
    {
      string id = null;
      string name = null;
      string dialogScreen = null;
      string workflowModelId = null;
      XPathNavigator attrNav = stateNav.Clone();
      if (attrNav.MoveToFirstAttribute())
        do
        {
          switch (attrNav.Name)
          {
            case "Id":
              id = attrNav.Value;
              break;
            case "Name":
              name = attrNav.Value;
              break;
            case "DialogScreen":
              dialogScreen = attrNav.Value;
              break;
            case "WorkflowModel":
              workflowModelId = attrNav.Value;
              break;
            default:
              throw new ArgumentException("'" + stateNav.Name + "' element doesn't support an attribute '" + attrNav.Name + "'");
          }
        } while (attrNav.MoveToNextAttribute());
      if (string.IsNullOrEmpty(id))
        throw new ArgumentException(string.Format("{0} '{1}': State must be specified", stateNav.Name, name));
      if (string.IsNullOrEmpty(name))
        throw new ArgumentException(string.Format("{0} with id '{1}': 'Name' attribute missing", stateNav.Name, id));
      if (string.IsNullOrEmpty(dialogScreen) && string.IsNullOrEmpty(workflowModelId))
        throw new ArgumentException(string.Format("{0} '{1}': Either 'WorkflowModel' or 'DialogScreen' atrribute must be specified", stateNav.Name, name));
      return new WorkflowState(new Guid(id), name, dialogScreen, false, false,
          workflowModelId == null ? (Guid?) null : new Guid(workflowModelId), WorkflowType.Dialog);
    }

    protected static WorkflowState LoadWorkflowState(XPathNavigator stateNav)
    {
      string id = null;
      string name = null;
      string mainScreen = null;
      bool inheritMenu = false;
      string workflowModelId = null;
      XPathNavigator attrNav = stateNav.Clone();
      if (attrNav.MoveToFirstAttribute())
        do
        {
          switch (attrNav.Name)
          {
            case "Id":
              id = attrNav.Value;
              break;
            case "Name":
              name = attrNav.Value;
              break;
            case "MainScreen":
              mainScreen = attrNav.Value;
              break;
            case "InheritMenu":
              if (!bool.TryParse(attrNav.Value, out inheritMenu))
                throw new ArgumentException("'InheritMenu' attribute has to be of type bool");
              break;
            case "WorkflowModel":
              workflowModelId = attrNav.Value;
              break;
            default:
              throw new ArgumentException("'" + stateNav.Name + "' element doesn't support an attribute '" + attrNav.Name + "'");
          }
        } while (attrNav.MoveToNextAttribute());
      if (string.IsNullOrEmpty(id))
        throw new ArgumentException(string.Format("{0} '{1}': State must be specified", stateNav.Name, name));
      if (string.IsNullOrEmpty(name))
        throw new ArgumentException(string.Format("{0} with id '{1}': 'Name' attribute missing", stateNav.Name, id));
      if (string.IsNullOrEmpty(mainScreen) && string.IsNullOrEmpty(workflowModelId))
        throw new ArgumentException(string.Format("{0} '{1}': Either 'WorkflowModel' or 'MainScreen' atrribute must be specified", stateNav.Name, name));
      return new WorkflowState(new Guid(id), name, mainScreen, inheritMenu, false,
          string.IsNullOrEmpty(workflowModelId) ? null : new Guid?(new Guid(workflowModelId)), WorkflowType.Workflow);
    }

    protected static WorkflowAction LoadPushNavigationTransition(XPathNavigator actionNav)
    {
      string id = null;
      string name = null;
      string displayCategory = null;
      string sortOrder = null;
      string sourceState = null;
      string targetState = null;
      string displayTitle = null;
      XPathNavigator attrNav = actionNav.Clone();
      if (attrNav.MoveToFirstAttribute())
        do
        {
          switch (attrNav.Name)
          {
            case "Id":
              id = attrNav.Value;
              break;
            case "Name":
              name = attrNav.Value;
              break;
            case "DisplayCategory":
              displayCategory = attrNav.Value;
              break;
            case "SortOrder":
              sortOrder = attrNav.Value;
              break;
            case "SourceState":
              sourceState = attrNav.Value;
              break;
            case "TargetState":
              targetState = attrNav.Value;
              break;
            case "DisplayTitle":
              displayTitle = attrNav.Value;
              break;
            default:
              throw new ArgumentException("'" + actionNav.Name + "' element doesn't support an attribute '" + attrNav.Name + "'");
          }
        } while (attrNav.MoveToNextAttribute());
      if (string.IsNullOrEmpty(id))
        throw new ArgumentException(string.Format("{0} '{1}': Id attribute is missing", actionNav.Name, name));
      if (string.IsNullOrEmpty(name))
        throw new ArgumentException(string.Format("{0} with id '{1}': 'Name' attribute missing", actionNav.Name, id));
      if (string.IsNullOrEmpty(sourceState))
        throw new ArgumentException(string.Format("{0} '{1}': 'SourceState' attribute missing", actionNav.Name, name));
      if (string.IsNullOrEmpty(targetState))
        throw new ArgumentException(string.Format("{0} '{1}': 'TargetState' attribute missing", actionNav.Name, name));
      PushNavigationTransition result = new PushNavigationTransition(new Guid(id), name, sourceState == "*" ? new Guid?() : new Guid(sourceState),
          new Guid(targetState), LocalizationHelper.CreateResourceString(displayTitle))
        {
            DisplayCategory = displayCategory,
            SortOrder = sortOrder
        };
      return result;
    }

    protected static WorkflowAction LoadPopNavigationTransition(XPathNavigator actionNav)
    {
      string id = null;
      string name = null;
      string displayCategory = null;
      string sortOrder = null;
      string sourceState = null;
      int numPop = -1;
      string displayTitle = null;
      XPathNavigator attrNav = actionNav.Clone();
      if (attrNav.MoveToFirstAttribute())
        do
        {
          switch (attrNav.Name)
          {
            case "Id":
              id = attrNav.Value;
              break;
            case "Name":
              name = attrNav.Value;
              break;
            case "DisplayCategory":
              displayCategory = attrNav.Value;
              break;
            case "SortOrder":
              sortOrder = attrNav.Value;
              break;
            case "SourceState":
              sourceState = attrNav.Value;
              break;
            case "NumPop":
              if (!Int32.TryParse(attrNav.Value, out numPop))
                throw new ArgumentException("'NumPop' attribute value must be a positive integer");
              break;
            case "DisplayTitle":
              displayTitle = attrNav.Value;
              break;
            default:
              throw new ArgumentException("'" + actionNav.Name + "' element doesn't support an attribute '" + attrNav.Name + "'");
          }
        } while (attrNav.MoveToNextAttribute());
      if (string.IsNullOrEmpty(id))
        throw new ArgumentException(string.Format("{0} '{1}': Id attribute is missing", actionNav.Name, name));
      if (string.IsNullOrEmpty(name))
        throw new ArgumentException(string.Format("{0} with id '{1}': 'Name' attribute missing", actionNav.Name, id));
      if (string.IsNullOrEmpty(sourceState))
        throw new ArgumentException(string.Format("{0} '{1}': 'SourceState' attribute missing", actionNav.Name, name));
      if (numPop == -1)
        throw new ArgumentException(string.Format("{0} '{1}': 'NumPop' attribute missing", actionNav.Name, name));
      PopNavigationTransition result = new PopNavigationTransition(new Guid(id), name, sourceState == "*" ? new Guid?() : new Guid(sourceState),
          numPop, LocalizationHelper.CreateResourceString(displayTitle))
        {
            DisplayCategory = displayCategory,
            SortOrder = sortOrder
        };
      return result;
    }

    protected static WorkflowAction LoadWorkflowContributorAction(XPathNavigator actionNav)
    {
      string id = null;
      string name = null;
      string displayCategory = null;
      string sortOrder = null;
      string sourceState = null;
      string contributorModel = null;
      string displayTitle = null;
      XPathNavigator attrNav = actionNav.Clone();
      if (attrNav.MoveToFirstAttribute())
        do
        {
          switch (attrNav.Name)
          {
            case "Id":
              id = attrNav.Value;
              break;
            case "Name":
              name = attrNav.Value;
              break;
            case "DisplayCategory":
              displayCategory = attrNav.Value;
              break;
            case "SortOrder":
              sortOrder = attrNav.Value;
              break;
            case "SourceState":
              sourceState = attrNav.Value;
              break;
            case "ContributorModelId":
              contributorModel = attrNav.Value;
              break;
            case "DisplayTitle":
              displayTitle = attrNav.Value;
              break;
            default:
              throw new ArgumentException("'" + actionNav.Name + "' element doesn't support an attribute '" + attrNav.Name + "'");
          }
        } while (attrNav.MoveToNextAttribute());
      if (string.IsNullOrEmpty(id))
        throw new ArgumentException(string.Format("{0} '{1}': Id attribute is missing", actionNav.Name, name));
      if (string.IsNullOrEmpty(name))
        throw new ArgumentException(string.Format("{0} with id '{1}': 'Name' attribute missing", actionNav.Name, id));
      if (string.IsNullOrEmpty(sourceState))
        throw new ArgumentException(string.Format("{0} '{1}': 'SourceState' attribute missing", actionNav.Name, name));
      if (string.IsNullOrEmpty(contributorModel))
        throw new ArgumentException(string.Format("{0} '{1}': 'ContributorModelId' attribute missing", actionNav.Name, name));
      WorkflowContributorAction result = new WorkflowContributorAction(new Guid(id), name, sourceState == "*" ? new Guid?() : new Guid(sourceState),
          new Guid(contributorModel), LocalizationHelper.CreateResourceString(displayTitle))
        {
            DisplayCategory = displayCategory,
            SortOrder = sortOrder
        };
      return result;
    }
  }
}
