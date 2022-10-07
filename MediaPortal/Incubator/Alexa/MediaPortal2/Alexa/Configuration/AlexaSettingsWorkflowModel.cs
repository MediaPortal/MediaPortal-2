namespace MediaPortal2.Alexa.Configuration
{
    using MediaPortal.Common;
    using MediaPortal.Common.PluginManager;
    using MediaPortal.Common.Settings;
    using MediaPortal.UI.Presentation.Models;
    using MediaPortal.UI.Presentation.Workflow;
    using MediaPortal2.Alexa;
    using System;
    using System.Collections.Generic;
    using System.Windows.Forms;

    public class AlexaSettingsWorkflowModel : IWorkflowModel
    {
        private readonly Guid AlexaPluginId = new Guid("D17E2C21-009A-48BF-B756-8E4B1D5F719B");
        private readonly Guid AlexaSettingsLoginDialogStateId = new Guid("7CC4E52F-A5AF-41A9-BEE8-E08AC4995919");
        private WebBrowserProxy browserForm;

        private void BrowserCompleted(WebBrowser wb)
        {
            if (wb.Url.LocalPath == "/Login.aspx")
            {
                string innerText;
                HtmlElement elementById = wb.Document.GetElementById("lblId");
                if (elementById != null)
                {
                    innerText = elementById.InnerText;
                }
                else
                {
                    HtmlElement local1 = elementById;
                    innerText = null;
                }
                string str = innerText;
                if (!string.IsNullOrWhiteSpace(str))
                {
                    ISettingsManager manager = ServiceRegistration.Get<ISettingsManager>();
                    AlexaSettings settingsObject = manager.Load<AlexaSettings>();
                    settingsObject.RegistrationId = str;
                    manager.Save(settingsObject);
                    this.browserForm.Dispose();
                    this.CallConnect();
                    IWorkflowManager manager2 = ServiceRegistration.Get<IWorkflowManager>();
                    if (manager2.CurrentNavigationContext.WorkflowState.StateId == this.AlexaSettingsLoginDialogStateId)
                    {
                        manager2.NavigatePop(1);
                    }
                }
            }
        }

        private void CallConnect()
        {
            PluginRuntime runtime;
            if (ServiceRegistration.Get<IPluginManager>().AvailablePlugins.TryGetValue(this.AlexaPluginId, out runtime))
            {
                AlexaPlugin stateTracker = runtime.StateTracker as AlexaPlugin;
                if (stateTracker == null)
                {
                    AlexaPlugin local1 = stateTracker;
                }
                else
                {
                    stateTracker.Connect();
                }
            }
        }

        public bool CanEnterState(NavigationContext oldContext, NavigationContext newContext) => 
            true;

        public void ChangeModelContext(NavigationContext oldContext, NavigationContext newContext, bool push)
        {
        }

        public void Deactivate(NavigationContext oldContext, NavigationContext newContext)
        {
        }

        public void EnterModelContext(NavigationContext oldContext, NavigationContext newContext)
        {
            if (newContext.WorkflowState.StateId == this.AlexaSettingsLoginDialogStateId)
            {
                this.Login();
            }
        }

        public void ExitModelContext(NavigationContext oldContext, NavigationContext newContext)
        {
            if (this.browserForm != null)
            {
                this.browserForm.Completed -= new WebBrowserProxy.CompletedCallback(this.BrowserCompleted);
                try
                {
                    this.browserForm.Dispose();
                }
                catch (Exception)
                {
                }
                this.browserForm = null;
            }
        }

        public void Login()
        {
            if (this.browserForm != null)
            {
                this.browserForm.Completed -= new WebBrowserProxy.CompletedCallback(this.BrowserCompleted);
                this.browserForm.Dispose();
                this.browserForm = null;
            }
            this.browserForm = new WebBrowserProxy();
            this.browserForm.Navigate(new Uri("https://deviceproxy.nocrosshair.de/Login.html"));
            this.browserForm.Completed += new WebBrowserProxy.CompletedCallback(this.BrowserCompleted);
        }

        public void Reactivate(NavigationContext oldContext, NavigationContext newContext)
        {
        }

        public void UpdateMenuActions(NavigationContext context, IDictionary<Guid, WorkflowAction> actions)
        {
        }

        public ScreenUpdateMode UpdateScreen(NavigationContext context, ref string screen) => 
            ScreenUpdateMode.AutoWorkflowManager;

        public Guid ModelId =>
            new Guid("EA7A41D6-53C2-4BED-A85E-6C1B49D9E403");
    }
}

