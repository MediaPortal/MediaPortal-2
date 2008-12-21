#region Copyright (C) 2007-2008 Team MediaPortal

/*
    Copyright (C) 2007-2008 Team MediaPortal
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
using MediaPortal.Presentation.Workflow;
using MediaPortal.SkinEngine.Xaml.Exceptions;
using MediaPortal.SkinEngine.Xaml.Interfaces;

namespace MediaPortal.SkinEngine.MarkupExtensions
{
  public class GetModelMarkupExtension: IEvaluableMarkupExtension
  {

    #region Protected fields

    protected string _id;

    #endregion

    public GetModelMarkupExtension() { }

    #region Properties

    public string Id
    {
      get { return _id; }
      set { _id = value; }
    }

    #endregion

    #region IEvaluableMarkupExtension implementation

    object IEvaluableMarkupExtension.Evaluate(IParserContext context)
    {
      if (Id == null)
        throw new XamlBindingException("GetModelMarkupExtension: Property Id has to be given");
      IWorkflowManager workflowManager = ServiceScope.Get<IWorkflowManager>();
      Guid modelId = new Guid(Id);
      NavigationContext currentContext = workflowManager.CurrentNavigationContext;
      if (currentContext == null)
        throw new XamlBindingException("Navigation context is not initialized - Workflow manager might not be initialized");
      object model;
      if (!currentContext.Models.TryGetValue(modelId, out model))
        throw new XamlBindingException(
            "GetModelMarkupExtension: Model with id '{0}' is not present in current navigation context (state='{1}')",
            modelId, currentContext.WorkflowState.StateId);
      return model;
    }

    #endregion
  }
}
