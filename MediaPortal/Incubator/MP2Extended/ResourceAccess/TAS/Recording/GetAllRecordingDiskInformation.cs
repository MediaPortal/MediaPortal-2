using System.Collections.Generic;
using System.Linq;
using HttpServer;
using HttpServer.Exceptions;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Plugins.MP2Extended.TAS.Misc.BaseClasses;
using MediaPortal.Plugins.MP2Extended.Utils;
using MediaPortal.Plugins.SlimTv.Interfaces;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.TAS.Recording
{
  internal class GetAllRecordingDiskInformation : BaseCard, IRequestMicroModuleHandler
  {
    public dynamic Process(IHttpRequest request)
    {
      if (!ServiceRegistration.IsRegistered<ITvProvider>())
        throw new BadRequestException("GetAllRecordingDiskInformation: ITvProvider not found");

      ITunerInfo tunerInfo = ServiceRegistration.Get<ITvProvider>() as ITunerInfo;

      if (tunerInfo == null)
        throw new BadRequestException("GetAllRecordingDiskInformation: ITunerInfo not present");

      List<ICard> cards;
      tunerInfo.GetCards(out cards);

      return cards.Select(card => Card(card)).Select(x => x.RecordingFolder).Distinct().AsQueryable()
                .Select(x => DiskSpaceInformation.GetSpaceInformation(x))
                .GroupBy(x => x.Disk, (key, list) => list.First())
                .ToList();
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}