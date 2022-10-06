namespace MediaPortal2.Alexa.Configuration
{
    using MediaPortal.Common.Settings;
    using System;
    using System.Runtime.CompilerServices;

    public class AlexaSettings
    {
        [Setting(SettingScope.User, "")]
        public string RegistrationId { get; set; }
    }
}

