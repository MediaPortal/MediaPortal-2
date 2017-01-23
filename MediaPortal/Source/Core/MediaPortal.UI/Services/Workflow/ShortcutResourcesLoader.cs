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
using MediaPortal.UI.Control.InputManager;
using MediaPortal.UI.Presentation.SkinResources;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.Utilities;

namespace MediaPortal.UI.Services.Workflow
{
  /// <summary>
  /// Class for loading MediaPortal 2 shortcut definitions from the current skin context. The shortcuts for WorkflowActions and WorkflowStates
  /// are loaded separately because the information is required to be loaded under different conditions.
  /// </summary>
  public class ShortcutResourcesLoader
  {
    #region Internal helper classes

    protected class KeyActionMapping
    {
      public List<Key> Keys = new List<Key>();
      public WorkflowAction Action;
    }

    protected class KeyStateMapping
    {
      public List<Key> Keys = new List<Key>();
      public WorkflowState State;
    }

    #endregion

    public const int SHORTCUT_RESOURCE_SPEC_VERSION_MAJOR = 1;
    public const int MIN_SHORTCUT_RESOURCE_SPEC_VERSION_MINOR = 0;

    public const string SHORTCUTS_DIRECTORY = "shortcuts";

    protected IDictionary<Key, WorkflowAction> _workflowActionShortcuts = new Dictionary<Key, WorkflowAction>();
    protected IDictionary<Key, WorkflowState> _workflowStateShortcuts = new Dictionary<Key, WorkflowState>();

    public IDictionary<Key, WorkflowAction> WorkflowActionShortcuts
    {
      get { return _workflowActionShortcuts; }
    }
    public IDictionary<Key, WorkflowState> WorkflowStateShortcuts
    {
      get { return _workflowStateShortcuts; }
    }

    /// <summary>
    /// Loads the shortcut definitions for workflow states from files contained in the current skin context.
    /// </summary>
    public void LoadWorkflowStateShortcuts()
    {
      _workflowStateShortcuts.Clear();
      Load(true);
    }

    /// <summary>
    /// Loads the shortcut definitions for workflow actions from files contained in the current skin context.
    /// </summary>
    public void LoadWorkflowActionShortcuts()
    {
      _workflowActionShortcuts.Clear();
      Load(false);
    }

    protected void Load(bool isWorkflowSate)
    {
      IDictionary<string, string> shortcutResources = ServiceRegistration.Get<ISkinResourceManager>().
        SkinResourceContext.GetResourceFilePaths("^" + SHORTCUTS_DIRECTORY + "\\\\.*\\.xml$");
      foreach (string resourceFilePath in shortcutResources.Values)
        LoadShortcutResourceFile(resourceFilePath, isWorkflowSate);
    }

    protected static ICollection<Key> ParseKeys(string keyStr)
    {
      if (string.IsNullOrEmpty(keyStr))
        return new List<Key>();
      if (keyStr == "*")
        return null;
      string[] keyStrs = keyStr.Split(',');
      return keyStrs.Select(str => Key.DeserializeKey(str, true)).ToList();
    }

    protected void LoadShortcutResourceFile(string filePath, bool isWorkflowSate)
    {
      try
      {
        XPathDocument doc = new XPathDocument(filePath);
        XPathNavigator nav = doc.CreateNavigator();
        nav.MoveToChild(XPathNodeType.Element);
        if (nav.LocalName != "Shortcut")
          throw new ArgumentException(
              "File is no shortcut descriptor file (document element must be 'Shortcut')");

        bool versionOk = false;
        XPathNavigator attrNav = nav.Clone();
        if (attrNav.MoveToFirstAttribute())
          do
          {
            switch (attrNav.Name)
            {
              case "DescriptorVersion":
                Versions.CheckVersionCompatible(attrNav.Value, SHORTCUT_RESOURCE_SPEC_VERSION_MAJOR, MIN_SHORTCUT_RESOURCE_SPEC_VERSION_MINOR);
                //string specVersion = attr.Value; <- if needed
                versionOk = true;
                break;
              default:
                throw new ArgumentException("'Shortcut' element doesn't support an attribute '" + attrNav.Name + "'");
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
              case "WorkflowActions":
                if (!isWorkflowSate)
                  foreach (KeyActionMapping actionMapping in LoadActionShortcuts(childNav))
                  {
                    foreach (Key key in actionMapping.Keys)
                    {
                      AvoidDuplicateKeys(key);
                      _workflowActionShortcuts.Add(key, actionMapping.Action);
                    }
                  }
                break;
              case "WorkflowStates":
                if (isWorkflowSate)
                  foreach (KeyStateMapping actionMapping in LoadStateShortcuts(childNav))
                  {
                    foreach (Key key in actionMapping.Keys)
                    {
                      AvoidDuplicateKeys(key);
                      _workflowStateShortcuts.Add(key, actionMapping.State);
                    }
                  }
                break;
              default:
                throw new ArgumentException("'Shortcut' element doesn't support a child element '" + childNav.Name + "'");
            }
          } while (childNav.MoveToNext(XPathNodeType.Element));
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Error("Error loading shortcut resource file '" + filePath + "'", e);
      }
    }

    protected void AvoidDuplicateKeys(Key key)
    {
      if (_workflowActionShortcuts.ContainsKey(key))
        throw new ArgumentException(string.Format("A global shortcut for Key '{0}' was already registered with action '{1}'", key, _workflowActionShortcuts[key].Name));
      if (_workflowStateShortcuts.ContainsKey(key))
        throw new ArgumentException(string.Format("A global shortcut for Key '{0}' was already registered with state '{1}'", key, _workflowStateShortcuts[key].Name));
    }

    protected static IEnumerable<KeyActionMapping> LoadActionShortcuts(XPathNavigator actionsNav)
    {
      XPathNavigator childNav = actionsNav.Clone();
      if (childNav.MoveToChild(XPathNodeType.Element))
        do
        {
          switch (childNav.LocalName)
          {
            case "Shortcut":
              yield return LoadWorkflowActionShortcut(childNav.Clone());
              break;
            default:
              throw new ArgumentException("'" + actionsNav.Name + "' element doesn't support a child element '" + childNav.Name + "'");
          }
        } while (childNav.MoveToNext(XPathNodeType.Element));
    }

    protected static KeyActionMapping LoadWorkflowActionShortcut(XPathNavigator shortcutNav)
    {
      string key = null;
      string workflowAction = null;
      XPathNavigator attrNav = shortcutNav.Clone();
      if (attrNav.MoveToFirstAttribute())
        do
        {
          switch (attrNav.Name)
          {
            case "Key":
              key = attrNav.Value;
              break;
            case "WorkflowAction":
              workflowAction = attrNav.Value;
              break;
            default:
              throw new ArgumentException("'" + shortcutNav.Name + "' element doesn't support an attribute '" + attrNav.Name + "'");
          }
        } while (attrNav.MoveToNextAttribute());
      if (string.IsNullOrEmpty(key))
        throw new ArgumentException(string.Format("{0}: 'Key' attribute is missing", shortcutNav.Name));
      if (string.IsNullOrEmpty(workflowAction))
        throw new ArgumentException(string.Format("{0}: 'WorkflowAction' attribute missing", shortcutNav.Name));

      IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
      WorkflowAction action;
      // We can only create shortcuts for MenuActions that have been defined before
      if (!workflowManager.MenuStateActions.TryGetValue(new Guid(workflowAction), out action))
        throw new ArgumentException(string.Format("{0} WorkflowAction with ID {1} does not exists, skipping shortcut!", shortcutNav.Name, workflowAction));

      KeyActionMapping mapping = new KeyActionMapping();
      mapping.Keys.AddRange(ParseKeys(key));
      mapping.Action = action;
      
      return mapping;
    }

    protected static IEnumerable<KeyStateMapping> LoadStateShortcuts(XPathNavigator actionsNav)
    {
      XPathNavigator childNav = actionsNav.Clone();
      if (childNav.MoveToChild(XPathNodeType.Element))
        do
        {
          switch (childNav.LocalName)
          {
            case "Shortcut":
              yield return LoadWorkflowStateShortcut(childNav.Clone());
              break;
            default:
              throw new ArgumentException("'" + actionsNav.Name + "' element doesn't support a child element '" + childNav.Name + "'");
          }
        } while (childNav.MoveToNext(XPathNodeType.Element));
    }

    protected static KeyStateMapping LoadWorkflowStateShortcut(XPathNavigator shortcutNav)
    {
      string key = null;
      string workflowState = null;
      XPathNavigator attrNav = shortcutNav.Clone();
      if (attrNav.MoveToFirstAttribute())
        do
        {
          switch (attrNav.Name)
          {
            case "Key":
              key = attrNav.Value;
              break;
            case "WorkflowState":
              workflowState = attrNav.Value;
              break;
            default:
              throw new ArgumentException("'" + shortcutNav.Name + "' element doesn't support an attribute '" + attrNav.Name + "'");
          }
        } while (attrNav.MoveToNextAttribute());
      if (string.IsNullOrEmpty(key))
        throw new ArgumentException(string.Format("{0}: 'Key' attribute is missing", shortcutNav.Name));
      if (string.IsNullOrEmpty(workflowState))
        throw new ArgumentException(string.Format("{0}: 'WorkflowState' attribute missing", shortcutNav.Name));

      IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
      WorkflowState state;
      // We can only create shortcuts for workflow states that have been defined before
      if (!workflowManager.States.TryGetValue(new Guid(workflowState), out state))
        throw new ArgumentException(string.Format("{0} WorkflowState with ID {1} does not exists, skipping shortcut!", shortcutNav.Name, workflowState));

      KeyStateMapping mapping = new KeyStateMapping();
      mapping.Keys.AddRange(ParseKeys(key));
      mapping.State = state;
      
      return mapping;
    }
  }
}
