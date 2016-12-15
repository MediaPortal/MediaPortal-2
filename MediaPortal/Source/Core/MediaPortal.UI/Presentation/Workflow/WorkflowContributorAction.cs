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
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.PluginManager;
using MediaPortal.Common.Localization;
using MediaPortal.Common.PluginManager.Exceptions;

namespace MediaPortal.UI.Presentation.Workflow
{
  /// <summary>
  /// The <see cref="WorkflowContributorAction"/> enables plugins to add a workflow action which is backed by
  /// a model instance implementing the <see cref="IWorkflowContributor"/> interface. Actions of that type can
  /// dynamically be enabled/disabled and made visible/invisible. They also can execute arbitrary code when invoked.
  /// </summary>
  public class WorkflowContributorAction : WorkflowAction
  {
    #region Consts

    public const string MODELS_REGISTRATION_LOCATION = "/Models";

    #endregion

    #region Protected fields

    protected IWorkflowContributor _contributor = null;
    protected int usages = 0;
    protected Guid _contributorModelId;
    protected IPluginItemStateTracker _modelItemStateTracker;

    #endregion

    public WorkflowContributorAction(Guid actionId, string name, IEnumerable<Guid> sourceStateIds, IResourceString displayTitle,
        Guid contributorModelId) : this(actionId, name, sourceStateIds, displayTitle, null, contributorModelId)
    { }

    public WorkflowContributorAction(Guid actionId, string name, IEnumerable<Guid> sourceStateIds, IResourceString displayTitle, IResourceString helpText,
        Guid contributorModelId) : base(actionId, name, sourceStateIds, displayTitle, helpText)
    {
      _modelItemStateTracker = new DefaultItemStateTracker("WorkflowContributorAction: Usage of workflow action contributor model")
        {
            Stopped = registration => Unbind()
        };
      _contributorModelId = contributorModelId;
    }

    #region Protected members

    protected void OnContributorStateChanged()
    {
      FireStateChanged();
    }

    protected void Bind()
    {
      object model = null;
      try
      {
        model = ServiceRegistration.Get<IPluginManager>().RequestPluginItem<object>(
            MODELS_REGISTRATION_LOCATION, _contributorModelId.ToString(), _modelItemStateTracker);
      }
      catch (PluginInvalidStateException e)
      {
        ServiceRegistration.Get<ILogger>().Warn("Cannot add workflow contributor model for model id '{0}'", e, _contributorModelId);
      }
      if (model == null)
      {
        ServiceRegistration.Get<ILogger>().Warn(string.Format("WorkflowContributorAction: Workflow contributor model with id '{0}' is not available", _contributorModelId));
        return;
      }
      _contributor = (IWorkflowContributor) model;
      _contributor.Initialize();
      _contributor.StateChanged += OnContributorStateChanged;
      FireStateChanged();
    }

    protected void Unbind()
    {
      if (_contributor == null)
        return;
      ServiceRegistration.Get<IPluginManager>().RevokePluginItem(MODELS_REGISTRATION_LOCATION, _contributorModelId.ToString(),
          _modelItemStateTracker);
      _contributor.Uninitialize();
      _contributor = null;
      FireStateChanged();
    }

    #endregion

    #region Public members

    public IWorkflowContributor Contributor
    {
      get { return _contributor; }
    }

    #endregion

    #region Base overrides

    public override IResourceString DisplayTitle
    {
      get
      {
        IResourceString result = null;
        if (_contributor != null)
          result = _contributor.DisplayTitle;
        return result ?? base.DisplayTitle;
      }
    }

    public override void AddRef()
    {
      base.AddRef();
      if (usages == 0)
        Bind();
      usages++;
    }

    public override void RemoveRef()
    {
      base.RemoveRef();
      usages--;
      if (usages == 0)
        Unbind();
    }

    public override bool IsVisible(NavigationContext context)
    {
      return _contributor != null && _contributor.IsActionVisible(context);
    }

    public override bool IsEnabled(NavigationContext context)
    {
      return _contributor != null && _contributor.IsActionEnabled(context);
    }

    /// <summary>
    /// Executes the <see cref="Contributor"/>'s <see cref="IWorkflowContributor.Execute"/> method.
    /// </summary>
    public override void Execute()
    {
      if (_contributor != null)
        _contributor.Execute();
    }

    #endregion
  }
}
