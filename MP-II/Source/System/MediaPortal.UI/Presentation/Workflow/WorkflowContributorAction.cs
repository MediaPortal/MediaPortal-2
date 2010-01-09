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
using MediaPortal.Core;
using MediaPortal.Core.Logging;
using MediaPortal.Core.PluginManager;
using MediaPortal.Core.Localization;

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

    #region Classes

    protected class ModelItemStateTracker : IPluginItemStateTracker
    {
      #region Protected fields

      protected WorkflowContributorAction _parent;

      #endregion

      #region Ctor

      public ModelItemStateTracker(WorkflowContributorAction parent)
      {
        _parent = parent;
      }

      #endregion

      #region IPluginItemStateTracker implementation

      public string UsageDescription
      {
        get { return "WorkflowContributorAction: Usage of workflow action contributor model"; }
      }

      public bool RequestEnd(PluginItemRegistration itemRegistration)
      {
        return true;
      }

      public void Stop(PluginItemRegistration itemRegistration)
      {
        _parent.Unbind();
      }

      public void Continue(PluginItemRegistration itemRegistration) { }

      #endregion
    }

    #endregion

    #region Protected fields

    protected IWorkflowContributor _contributor = null;
    protected int usages = 0;
    protected Guid _contributorModelId;
    protected ModelItemStateTracker _modelItemStateTracker;

    #endregion

    public WorkflowContributorAction(Guid actionId, string name, Guid? sourceStateId, Guid contributorModelId,
        IResourceString displayTitle) :
        base(actionId, name, sourceStateId, displayTitle)
    {
      _modelItemStateTracker = new ModelItemStateTracker(this);
      _contributorModelId = contributorModelId;
    }

    #region Protected members

    protected void OnContributorStateChanged()
    {
      FireStateChanged();
    }

    protected void Bind()
    {
      object model = ServiceScope.Get<IPluginManager>().RequestPluginItem<object>(
          MODELS_REGISTRATION_LOCATION, _contributorModelId.ToString(), _modelItemStateTracker);
      if (model == null)
      {
        ServiceScope.Get<ILogger>().Warn(string.Format("WorkflowContributorAction: Workflow contributor model with id '{0}' is not available", _contributorModelId));
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
      ServiceScope.Get<IPluginManager>().RevokePluginItem(MODELS_REGISTRATION_LOCATION, _contributorModelId.ToString(),
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

    public override bool IsVisible
    {
      get { return _contributor != null && _contributor.IsActionVisible; }
    }

    public override bool IsEnabled
    {
      get { return _contributor != null && _contributor.IsActionEnabled; }
    }

    public override IResourceString DisplayTitle
    {
      get { return _contributor == null ? base.DisplayTitle : _contributor.DisplayTitle; }
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
