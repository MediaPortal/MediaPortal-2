using System.Collections.Generic;
using System.Linq;
using HttpServer;
using HttpServer.Sessions;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.Exceptions;
using MediaPortal.Plugins.MP2Extended.TAS.Misc.BaseClasses;
using MediaPortal.Plugins.MP2Extended.TAS.Tv;
using MediaPortal.Plugins.MP2Extended.Utils;
using MediaPortal.Plugins.SlimTv.Interfaces;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.TAS.Recording
{
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, Summary = "")]
  internal class GetAllRecordingDiskInformation : BaseCard
  {
    public IList<WebDiskSpaceInformation> Process()
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