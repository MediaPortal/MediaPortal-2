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

using MediaPortal.Alexa.Contracts;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.PluginManager;
using MediaPortal.UI.Presentation.Players;
using System.ServiceModel;

    public class AlexaPlugin : IPluginStateTracker
    {
        private const string DeviceControllerServiceUrl = "https://deviceproxy.nocrosshair.de/MediaPortalDeviceControllerService.svc";
        private ILogger log;
        private DuplexChannelFactory<IMediaPortalDeviceController> factory;
        private IMediaPortalDeviceController channel;
        private Guid registrationId;

        public void Activated(PluginRuntime pluginRuntime)
        {
            this.log = ServiceRegistration.Get<ILogger>();
            NetHttpsBinding binding = new NetHttpsBinding(BasicHttpsSecurityMode.Transport);
            binding.SendTimeout = TimeSpan.FromSeconds(10.0);
            binding.OpenTimeout = TimeSpan.FromSeconds(10.0);
            binding.CloseTimeout = TimeSpan.FromSeconds(5.0);
            this.factory = new DuplexChannelFactory<IMediaPortalDeviceController>(typeof(MediaPortalDeviceControllerCallback), binding);
            this.factory.Faulted += new EventHandler(this.HandleDeviceControllerConnectionFaulted);
            this.Connect();
        }

        public void Connect()
        {
            if (!Guid.TryParse(ServiceRegistration.Get<ISettingsManager>().Load<AlexaSettings>().RegistrationId, out this.registrationId))
            {
                this.log.Info("Alexa found no registration id - please use the plugin configuration to register!", Array.Empty<object>());
            }
            else
            {
                IPlayerContextManager playerCtxMng = ServiceRegistration.Get<IPlayerContextManager>();
                this.channel = this.factory.CreateChannel(new InstanceContext(new MediaPortalDeviceControllerCallback(this.log, playerCtxMng, ServiceRegistration.Get<IPlayerManager>())), new EndpointAddress("https://deviceproxy.nocrosshair.de/MediaPortalDeviceControllerService.svc"));
                this.channel.Subscribe(this.registrationId);
                this.log.Info("Alexa successfully connected!", Array.Empty<object>());
            }
        }

        public void Continue()
        {
        }

        private void HandleDeviceControllerConnectionFaulted(object sender, EventArgs e)
        {
            this.log.Info("Alexa channel faulted - reconnecting...", Array.Empty<object>());
            this.Connect();
        }

        public bool RequestEnd() => 
            true;

        public void Shutdown()
        {
        }

        public void Stop()
        {
            try
            {
                this.factory.Faulted -= new EventHandler(this.HandleDeviceControllerConnectionFaulted);
                if (this.channel == null)
                {
                    IMediaPortalDeviceController channel = this.channel;
                }
                else
                {
                    this.channel.Unsubscribe(this.registrationId);
                }
                ((IClientChannel) this.channel).Close();
            }
            catch (Exception)
            {
            }
        }
    }
