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
using System.Collections.Generic;
using System.Linq;
using System.Xml.XPath;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.Localization;
using MediaPortal.UI.Presentation.SkinResources;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.Utilities;

namespace MediaPortal.UI.Services.Workflow
{
  /// <summary>
  /// Class for loading MediaPortal 2 workflow resources from the current skin context.
  /// </summary>
  public class WorkflowResourcesLoader
  {
    public const int WORKFLOW_RESOURCE_SPEC_VERSION_MAJOR = 1;
    public const int MIN_WORKFLOW_RESOURCE_SPEC_VERSION_MINOR = 0;

    public const string WORKFLOW_DIRECTORY = "workflow";

    protected IDictionary<Guid, WorkflowAction> _menuActions = new Dictionary<Guid, WorkflowAction>();

    public IDictionary<Guid, WorkflowAction> MenuActions
    {
      get { return _menuActions; }
    }

    /// <summary>
    /// Loads workflow resources from files contained in the current skin context.
    /// </summary>
    public void Load()
    {
      _menuActions.Clear();
      IDictionary<string, string> workflowResources = ServiceRegistration.Get<ISkinResourceManager>().
          SkinResourceContext.GetResourceFilePaths("^" + WORKFLOW_DIRECTORY + "\\\\.*\\.xml$");
      foreach (string workflowResourceFilePath in workflowResources.Values)
        LoadWorkflowResourceFile(workflowResourceFilePath);
    }

    protected static ICollection<Guid> ParseActionSourceStates(string sourceStatesStr)
    {
      if (string.IsNullOrEmpty(sourceStatesStr))
        return new List<Guid>();
      if (sourceStatesStr == "*")
        return null;
      string[] stateStrs = sourceStatesStr.Split(',');
      return stateStrs.Select(str => new Guid(str)).ToList();
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
              case "MenuActions":
                foreach (WorkflowAction action in LoadActions(childNav))
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
        ServiceRegistration.Get<ILogger>().Error("Error loading workflow resource file '" + filePath + "'", e);
      }
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
            case "MethodCallAction":
              yield return LoadMethodCallAction(childNav.Clone());
              break;
            // TODO: More actions - show screen, show dialog, ...
            default:
              throw new ArgumentException("'" + actionsNav.Name + "' element doesn't support a child element '" + childNav.Name + "'");
          }
        } while (childNav.MoveToNext(XPathNodeType.Element));
    }

    protected static WorkflowAction LoadPushNavigationTransition(XPathNavigator actionNav)
    {
      string id = null;
      string name = null;
      string displayTitle = null;
      string displayCategory = null;
      string helpText = null;
      string sortOrder = null;
      string sourceStates = null;
      string targetState = null;
      string navigationContextDisplayLabel = null;
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
            case "NavigationContextDisplayLabel":
              navigationContextDisplayLabel = attrNav.Value;
              break;
            case "DisplayCategory":
              displayCategory = attrNav.Value;
              break;
            case "SortOrder":
              sortOrder = attrNav.Value;
              break;
            case "SourceStates":
              sourceStates = attrNav.Value;
              break;
            case "TargetState":
              targetState = attrNav.Value;
              break;
            case "DisplayTitle":
              displayTitle = attrNav.Value;
              break;
            case "HelpText":
              helpText = attrNav.Value;
              break;
            default:
              throw new ArgumentException("'" + actionNav.Name + "' element doesn't support an attribute '" + attrNav.Name + "'");
          }
        } while (attrNav.MoveToNextAttribute());
      if (string.IsNullOrEmpty(id))
        throw new ArgumentException(string.Format("{0} '{1}': Id attribute is missing", actionNav.Name, name));
      if (string.IsNullOrEmpty(name))
        throw new ArgumentException(string.Format("{0} with id '{1}': 'Name' attribute missing", actionNav.Name, id));
      if (string.IsNullOrEmpty(sourceStates))
        throw new ArgumentException(string.Format("{0} '{1}': 'SourceStates' attribute missing", actionNav.Name, name));
      if (string.IsNullOrEmpty(targetState))
        throw new ArgumentException(string.Format("{0} '{1}': 'TargetState' attribute missing", actionNav.Name, name));
      PushNavigationTransition result = new PushNavigationTransition(new Guid(id), name, ParseActionSourceStates(sourceStates),
          LocalizationHelper.CreateResourceString(displayTitle), LocalizationHelper.CreateResourceString(helpText),
          new Guid(targetState), navigationContextDisplayLabel)
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
      string displayTitle = null;
      string displayCategory = null;
      string helpText = null;
      string sortOrder = null;
      string sourceStates = null;
      int numPop = -1;
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
            case "SourceStates":
              sourceStates = attrNav.Value;
              break;
            case "NumPop":
              if (!Int32.TryParse(attrNav.Value, out numPop))
                throw new ArgumentException("'NumPop' attribute value must be a positive integer");
              break;
            case "DisplayTitle":
              displayTitle = attrNav.Value;
              break;
            case "HelpText":
              helpText = attrNav.Value;
              break;
            default:
              throw new ArgumentException("'" + actionNav.Name + "' element doesn't support an attribute '" + attrNav.Name + "'");
          }
        } while (attrNav.MoveToNextAttribute());
      if (string.IsNullOrEmpty(id))
        throw new ArgumentException(string.Format("{0} '{1}': Id attribute is missing", actionNav.Name, name));
      if (string.IsNullOrEmpty(name))
        throw new ArgumentException(string.Format("{0} with id '{1}': 'Name' attribute missing", actionNav.Name, id));
      if (string.IsNullOrEmpty(sourceStates))
        throw new ArgumentException(string.Format("{0} '{1}': 'SourceStates' attribute missing", actionNav.Name, name));
      if (numPop == -1)
        throw new ArgumentException(string.Format("{0} '{1}': 'NumPop' attribute missing", actionNav.Name, name));
      PopNavigationTransition result = new PopNavigationTransition(new Guid(id), name, ParseActionSourceStates(sourceStates),
          LocalizationHelper.CreateResourceString(displayTitle), LocalizationHelper.CreateResourceString(helpText), numPop)
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
      string displayTitle = null;
      string displayCategory = null;
      string helpText = null;
      string sortOrder = null;
      string sourceStates = null;
      string contributorModel = null;
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
            case "SourceStates":
              sourceStates = attrNav.Value;
              break;
            case "ContributorModelId":
              contributorModel = attrNav.Value;
              break;
            case "DisplayTitle":
              displayTitle = attrNav.Value;
              break;
            case "HelpText":
              helpText = attrNav.Value;
              break;
            default:
              throw new ArgumentException("'" + actionNav.Name + "' element doesn't support an attribute '" + attrNav.Name + "'");
          }
        } while (attrNav.MoveToNextAttribute());
      if (string.IsNullOrEmpty(id))
        throw new ArgumentException(string.Format("{0} '{1}': Id attribute is missing", actionNav.Name, name));
      if (string.IsNullOrEmpty(name))
        throw new ArgumentException(string.Format("{0} with id '{1}': 'Name' attribute missing", actionNav.Name, id));
      if (string.IsNullOrEmpty(sourceStates))
        throw new ArgumentException(string.Format("{0} '{1}': 'SourceStates' attribute missing", actionNav.Name, name));
      if (string.IsNullOrEmpty(contributorModel))
        throw new ArgumentException(string.Format("{0} '{1}': 'ContributorModelId' attribute missing", actionNav.Name, name));
      WorkflowContributorAction result = new WorkflowContributorAction(new Guid(id), name, ParseActionSourceStates(sourceStates),
          LocalizationHelper.CreateResourceString(displayTitle), LocalizationHelper.CreateResourceString(helpText), new Guid(contributorModel))
        {
            DisplayCategory = displayCategory,
            SortOrder = sortOrder
        };
      return result;
    }

    protected static WorkflowAction LoadMethodCallAction(XPathNavigator actionNav)
    {
      string id = null;
      string name = null;
      string displayTitle = null;
      string displayCategory = null;
      string helpText = null;
      string sortOrder = null;
      string sourceStates = null;
      string modelId = null;
      string methodName = null;
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
            case "DisplayTitle":
              displayTitle = attrNav.Value;
              break;
            case "DisplayCategory":
              displayCategory = attrNav.Value;
              break;
            case "HelpText":
              helpText = attrNav.Value;
              break;
            case "SortOrder":
              sortOrder = attrNav.Value;
              break;
            case "SourceStates":
              sourceStates = attrNav.Value;
              break;
            case "ModelId":
              modelId = attrNav.Value;
              break;
            case "MethodName":
              methodName = attrNav.Value;
              break;
            default:
              throw new ArgumentException("'" + actionNav.Name + "' element doesn't support an attribute '" + attrNav.Name + "'");
          }
        } while (attrNav.MoveToNextAttribute());
      if (string.IsNullOrEmpty(id))
        throw new ArgumentException(string.Format("{0} '{1}': Id attribute is missing", actionNav.Name, name));
      if (string.IsNullOrEmpty(name))
        throw new ArgumentException(string.Format("{0} with id '{1}': 'Name' attribute missing", actionNav.Name, id));
      if (string.IsNullOrEmpty(sourceStates))
        throw new ArgumentException(string.Format("{0} '{1}': 'SourceStates' attribute missing", actionNav.Name, name));
      if (string.IsNullOrEmpty(modelId))
        throw new ArgumentException(string.Format("{0} '{1}': 'ModelId' attribute missing", actionNav.Name, name));
      if (string.IsNullOrEmpty(methodName))
        throw new ArgumentException(string.Format("{0} '{1}': 'MethodName' attribute missing", actionNav.Name, name));
      MethodCallAction result = new MethodCallAction(new Guid(id), name, ParseActionSourceStates(sourceStates),
          LocalizationHelper.CreateResourceString(displayTitle), LocalizationHelper.CreateResourceString(helpText),
          new Guid(modelId), methodName)
        {
            DisplayCategory = displayCategory,
            SortOrder = sortOrder
        };
      return result;
    }
  }
}
