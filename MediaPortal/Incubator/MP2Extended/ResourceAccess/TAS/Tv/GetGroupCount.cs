using MediaPortal.Common;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.MP2Extended.Exceptions;
using MediaPortal.Plugins.SlimTv.Interfaces;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.TAS.Tv
{
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, Summary = "")]
  internal class GetGroupCount : GetGroups
  {
    public WebIntResult Process()
    {
      if (!ServiceRegistration.IsRegistered<ITvProvider>())
        throw new BadRequestException("GetChannelsBasic: ITvProvider not found");

      return new WebIntResult { Result = Process(null, null).Count };
    }
  }
}
