using MediaPortal.Common;
using MediaPortal.Common.Messaging;
using MediaPortal.UI.Presentation.Screens;
using MediaPortal.UI.Presentation.Workflow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.Models
{
  public class QueryCreateConfigurationModel : IDisposable
  {
    public static readonly Guid MODEL_ID = new Guid("ED801805-42FC-47A2-B978-64B61BD48FE9");

    public static QueryCreateConfigurationModel Instance()
    {
      return (QueryCreateConfigurationModel)ServiceRegistration.Get<IWorkflowManager>().GetModel(MODEL_ID);
    }

    protected readonly object _syncRoot = new object();
    protected Guid _doConfigureDialogHandle = Guid.Empty;
    protected AsynchronousMessageQueue _messageQueue;

    public QueryCreateConfigurationModel()
    {
      _messageQueue = new AsynchronousMessageQueue(this, new[] { DialogManagerMessaging.CHANNEL });
      _messageQueue.MessageReceived += OnMessageReceived;
      _messageQueue.Start();
    }

    protected void OnMessageReceived(AsynchronousMessageQueue queue, SystemMessage message)
    {
      if (message.ChannelName == DialogManagerMessaging.CHANNEL)
      {
        DialogManagerMessaging.MessageType messageType = (DialogManagerMessaging.MessageType)message.MessageType;
        if (messageType == DialogManagerMessaging.MessageType.DialogClosed)
        {
          Guid dialogHandle = (Guid)message.MessageData[DialogManagerMessaging.DIALOG_HANDLE];
          bool doConfigure = false;
          lock (_syncRoot)
            if (_doConfigureDialogHandle == dialogHandle)
            {
              _doConfigureDialogHandle = Guid.Empty;
              DialogResult dialogResult = (DialogResult)message.MessageData[DialogManagerMessaging.DIALOG_RESULT];
              if (dialogResult == DialogResult.Yes)
                doConfigure = true;
            }
          if (doConfigure)
            DoCreateConfiguration();
        }
      }
    }

    public void QueryCreateConfiguration()
    {
      Guid doConfigureHandle = ServiceRegistration.Get<IDialogManager>().ShowDialog("[Emulators.ConfigureEmulatorNow.Header]", "[Emulators.ConfigureEmulatorNow.Label]", DialogType.YesNoDialog, false, DialogButtonType.Yes);
      lock (_syncRoot)
        _doConfigureDialogHandle = doConfigureHandle;
    }

    protected void DoCreateConfiguration()
    {
      ServiceRegistration.Get<IWorkflowManager>().NavigatePush(EmulatorConfigurationModel.STATE_OVERVIEW);
    }

    public void Dispose()
    {
      if (_messageQueue != null)
      {
        _messageQueue.Dispose();
        _messageQueue = null;
      }
    }
  }
}
