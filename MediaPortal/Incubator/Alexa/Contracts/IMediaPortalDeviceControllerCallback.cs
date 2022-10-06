namespace MediaPortal.Alexa.Contracts
{
    using System;
    using System.ServiceModel;

    [ServiceContract]
    public interface IMediaPortalDeviceControllerCallback
    {
        [OperationContract]
        int AdjustVolume(int change, bool explicitly);
        [OperationContract]
        string ChangeChannel(string channelName);
        [OperationContract]
        bool FastForward();
        [OperationContract]
        bool? GetPlaybackState();
        [OperationContract]
        bool Next();
        [OperationContract]
        bool Pause();
        [OperationContract]
        bool Play();
        [OperationContract]
        bool Previous();
        [OperationContract]
        bool Rewind();
        [OperationContract]
        bool SetMute(bool mute);
        [OperationContract]
        bool SetVolume(int level);
        [OperationContract]
        bool Stop();
    }
}

