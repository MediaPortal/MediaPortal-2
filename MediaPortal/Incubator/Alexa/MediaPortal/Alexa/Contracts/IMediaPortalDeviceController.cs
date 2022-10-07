namespace MediaPortal.Alexa.Contracts
{
    using System;
    using System.ServiceModel;

    [ServiceContract(CallbackContract=typeof(IMediaPortalDeviceControllerCallback))]
    public interface IMediaPortalDeviceController
    {
        [OperationContract]
        bool Subscribe(Guid registrationId);
        [OperationContract]
        void Unsubscribe(Guid registrationId);
    }
}

