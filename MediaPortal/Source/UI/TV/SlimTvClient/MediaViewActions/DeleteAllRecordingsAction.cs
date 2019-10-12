using MediaPortal.Common;
using MediaPortal.Common.Async;
using MediaPortal.Common.Localization;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.UserProfileDataManagement;
using MediaPortal.Plugins.SlimTv.Client.Actions;
using MediaPortal.Plugins.SlimTv.Client.Models.Navigation;
using MediaPortal.Plugins.SlimTv.Client.TvHandler;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UiComponents.Media.Extensions;
using MediaPortal.UiComponents.Media.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaPortal.Plugins.SlimTv.Client.MediaViewActions
{
  public class DeleteAllRecordingsAction : IMediaViewActionConfirmation, IUserRestriction
  {
    public string ConfirmationMessage(View view)
    {
      return LocalizationHelper.Translate("[SlimTvClient.DeleteAllRecordings.Confirmation]", view.AllMediaItems.Count());
    }

    public Task<bool> IsAvailableAsync(View view)
    {
      IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
      return Task.FromResult<bool>(workflowManager.CurrentNavigationContext.WorkflowState.StateId == SlimTvConsts.WF_MEDIA_NAVIGATION_ROOT_STATE
        && !view.IsEmpty);
    }

    public async Task<bool> ProcessAsync(View view)
    {
      await new DeleteAllRecordings().DeleteList(view.AllMediaItems.ToList());
      return true;
    }

    public string RestrictionGroup { get; set; }
  }
}
