using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.MP2Extended.Exceptions;
using MediaPortal.Plugins.SlimTv.Interfaces;
using MP2Extended.TAS.Extensions;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.TAS.Radio
{
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, Summary = "")]
  internal class GetRadioGroupCount
  {
    public WebIntResult Process()
    {
      if (!ServiceRegistration.IsRegistered<ITvProvider>())
        throw new BadRequestException("GetRadioGroupCount: ITvProvider not found");
      
      IChannelAndGroupInfo channelAndGroupInfo = ServiceRegistration.Get<ITvProvider>() as IChannelAndGroupInfo;

      return new WebIntResult { Result = channelAndGroupInfo.GetRadioGroups().Count };
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
