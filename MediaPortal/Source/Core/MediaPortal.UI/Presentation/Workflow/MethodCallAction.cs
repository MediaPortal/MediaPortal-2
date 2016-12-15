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
using System.Reflection;
using MediaPortal.Common;
using MediaPortal.Common.Localization;
using MediaPortal.Common.Logging;

namespace MediaPortal.UI.Presentation.Workflow
{
  /// <summary>
  /// When invoked, this action calls a method in a model of a given model id.
  /// </summary>
  public class MethodCallAction : WorkflowAction
  {
    #region Protected fields

    protected Guid _modelId;
    protected string _methodName;

    #endregion

    public MethodCallAction(Guid actionId, string name, IEnumerable<Guid> sourceStateIds, IResourceString displayTitle, Guid modelId, string methodName) :
        this(actionId, name, sourceStateIds, displayTitle, null, modelId, methodName)
    {
    }

    public MethodCallAction(Guid actionId, string name, IEnumerable<Guid> sourceStateIds, IResourceString displayTitle, IResourceString helpText, Guid modelId, string methodName) :
        base(actionId, name, sourceStateIds, displayTitle, helpText)
    {
      _modelId = modelId;
      _methodName = methodName;
    }

    /// <summary>
    /// Returns the id of the model where the method of the given <see cref="MethodName"/> will be called when this
    /// action is invoked.
    /// </summary>
    public Guid ModelId
    {
      get { return _modelId; }
    }

    /// <summary>
    /// Returns the name of the method which will be called when this action is invoked.
    /// </summary>
    public string MethodName
    {
      get { return _methodName; }
    }

    public override bool IsVisible(NavigationContext context)
    {
      return true;
    }

    public override bool IsEnabled(NavigationContext context)
    {
      return true;
    }

    /// <summary>
    /// Executes the method of name <see cref="MethodName"/> in the model with id <see cref="ModelId"/>.
    /// </summary>
    public override void Execute()
    {
      IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
      object model = workflowManager.GetModel(ModelId);
      if (model == null)
      {
        ServiceRegistration.Get<ILogger>().Warn("MethodCallAction: Unable to load model with id '{0}'", ModelId);
        return;
      }

      MethodInfo mi = model.GetType().GetMethod(MethodName);
      if (mi == null)
      {
        ServiceRegistration.Get<ILogger>().Warn("MethodCallAction: Unable to find method of name '{0}' in model with id '{1}'",
            MethodName, ModelId);
        return;
      }
      try
      {
        mi.Invoke(model, new object[] {});
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Warn("MethodCallAction: Error calling method of name '{0}' in model with id '{1}'", e,
            MethodName, ModelId);
      }
    }
  }
}
