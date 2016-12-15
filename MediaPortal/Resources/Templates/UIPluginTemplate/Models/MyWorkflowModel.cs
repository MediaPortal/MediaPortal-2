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
using MediaPortal.Core.General;
using MediaPortal.UI.Presentation.Models;
using MediaPortal.UI.Presentation.Workflow;

namespace MyPlugin.Models
{
  /// <summary>
  /// Template for a workflow model.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Workflow models act like simple models (see <see cref="MySimpleModel"/>), but additionally, they can attend a workflow.
  /// The workflow manager automatically calls their <see cref="IWorkflowModel"/> methods.
  /// </para>
  /// <para>
  /// Workflow models must also be registered in the <c>plugin.xml</c> file.
  /// </para>
  /// </remarks>
  public class MyWorkflowModel : IDisposable, IWorkflowModel
  {
    #region Consts

    // Use the same string as in the registration of this model in the plugin.xml file
    public const string MODEL_ID_STR = "XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX";
    public static readonly Guid MODEL_ID = new Guid(MODEL_ID_STR);

    #endregion

    #region Protected fields

    protected readonly AbstractProperty _otherValueProperty;

    #endregion

    #region Ctor & maintainance

    public MyWorkflowModel()
    {
      _otherValueProperty = new WProperty(typeof(string), string.Empty);
    }

    public void Dispose()
    {
      // Optional disposal method.
    }

    #endregion

    #region Public members

    public string OtherValue
    {
      get { return (string) _otherValueProperty.GetValue(); }
      set { _otherValueProperty.SetValue(value); }
    }

    public AbstractProperty OtherValueProperty
    {
      get { return _otherValueProperty; }
    }

    public void PublicMethod()
    {
      // Do something
    }

    #endregion

    #region IWorkflowModel implementation

    public Guid ModelId
    {
      get { return MODEL_ID; }
    }

    public bool CanEnterState(NavigationContext oldContext, NavigationContext newContext)
    {
      // TODO
      return true;
    }

    public void EnterModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      // TODO
    }

    public void ExitModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      // TODO
    }

    public void ChangeModelContext(NavigationContext oldContext, NavigationContext newContext, bool push)
    {
      // TODO
    }

    public void Deactivate(NavigationContext oldContext, NavigationContext newContext)
    {
      // TODO
    }

    public void Reactivate(NavigationContext oldContext, NavigationContext newContext)
    {
      // TODO
    }

    public void UpdateMenuActions(NavigationContext context, IDictionary<Guid, WorkflowAction> actions)
    {
      // TODO
    }

    public ScreenUpdateMode UpdateScreen(NavigationContext context, ref string screen)
    {
      // TODO
      return ScreenUpdateMode.AutoWorkflowManager;
    }

    #endregion
  }
}
