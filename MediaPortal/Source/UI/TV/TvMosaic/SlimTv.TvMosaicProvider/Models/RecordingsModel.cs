#region Copyright (C) 2007-2021 Team MediaPortal

/*
    Copyright (C) 2007-2021 Team MediaPortal
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

using MediaPortal.UI.Presentation.Models;
using MediaPortal.UI.Presentation.Workflow;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MediaPortal.Common.General;
using MediaPortal.UI.Presentation.DataObjects;

namespace SlimTv.TvMosaicProvider.Models
{
  /// <summary>
  /// <see cref="RecordingsModel"/> exposes recordings information provided by TvMosaic provider.
  /// </summary>
  public class RecordingsModel : BaseMessageControlledModel, IWorkflowModel
  {
    protected ItemsList _items = new ItemsList();

    public RecordingsModel()
    {
    }

    public ItemsList Items
    {
      get { return _items; }
    }

    public Guid ModelId { get; } = new Guid("AE53B8DA-FEA8-4CDD-82CF-2FF161AFF038");

    private async Task LoadRecordings()
    {
      var tvMosaic = new TvMosaicProvider();
      if (!tvMosaic.IsInitialized)
        tvMosaic.Init();

      var recs = await tvMosaic.GetRecordings();
      Items.Clear();
      foreach (var rec in recs.Items)
      {
        var recItem = new ListItem("Name", rec.VideoInfo.Name);
        var mi = tvMosaic.CreateMediaItem(0, rec.Url, null);

        Items.Add(recItem);
      }
      Items.FireChange();
    }

    public virtual bool CanEnterState(NavigationContext oldContext, NavigationContext newContext)
    {
      return true;
    }

    public virtual void EnterModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      LoadRecordings().Wait();
      Attach();
    }

    public virtual void ExitModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      Detach();
    }

    public virtual void ChangeModelContext(NavigationContext oldContext, NavigationContext newContext, bool push)
    {
    }

    public virtual void Deactivate(NavigationContext oldContext, NavigationContext newContext)
    {
      Detach();
    }

    public virtual void Reactivate(NavigationContext oldContext, NavigationContext newContext)
    {
      Attach();
    }

    private void Attach()
    {
    }

    private void Detach()
    {
    }

    public virtual void UpdateMenuActions(NavigationContext context, IDictionary<Guid, WorkflowAction> actions)
    {
    }

    public ScreenUpdateMode UpdateScreen(NavigationContext context, ref string screen)
    {
      return ScreenUpdateMode.AutoWorkflowManager;
    }
  }
}
