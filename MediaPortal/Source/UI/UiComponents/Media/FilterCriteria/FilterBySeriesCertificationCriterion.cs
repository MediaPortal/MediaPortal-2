#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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

using MediaPortal.Common;
using MediaPortal.Common.Certifications;
using MediaPortal.Common.Exceptions;
using MediaPortal.Common.General;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Common.SystemCommunication;
using MediaPortal.UI.ServerCommunication;
using MediaPortal.UiComponents.Media.General;
using MediaPortal.UiComponents.Media.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using MediaPortal.Common.TaskScheduler;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace MediaPortal.UiComponents.Media.FilterCriteria
{
  public class FilterBySeriesCertificationCriterion : SimpleMLFilterCriterion
  {
    public FilterBySeriesCertificationCriterion(MediaItemAspectMetadata.AttributeSpecification attributeType)
      : base(attributeType)
    {
      _necessaryMIATypeIds = Consts.NECESSARY_SERIES_MIAS;
    }

    #region Base overrides

    public override async Task<ICollection<FilterValue>> GetAvailableValuesAsync(IEnumerable<Guid> necessaryMIATypeIds, IFilter selectAttributeFilter, IFilter filter)
    {
      IContentDirectory cd = ServiceRegistration.Get<IServerConnectionManager>().ContentDirectory;
      if (cd == null)
        throw new NotConnectedException("The MediaLibrary is not connected");

      bool showVirtual = VirtualMediaHelper.ShowVirtualMedia(necessaryMIATypeIds);

      if (string.IsNullOrEmpty(CertificationHelper.DisplaySeriesCertificationCountry))
      {
        HomogenousMap valueGroups = await cd.GetValueGroupsAsync(SeriesAspect.ATTR_CERTIFICATION, null, ProjectionFunction.None,
            necessaryMIATypeIds, filter, true, showVirtual);
        IList<FilterValue> result = new List<FilterValue>(valueGroups.Count);
        int numEmptyEntries = 0;
        foreach (KeyValuePair<object, object> group in valueGroups)
        {
          string certification = (string)group.Key;
          if (!string.IsNullOrEmpty(certification))
          {
            CertificationMapping cert;
            if (CertificationMapper.TryFindSeriesCertification(certification, out cert))
            {
              result.Add(new FilterValue(cert.CertificationId, cert.Name,
                  new RelationalFilter(SeriesAspect.ATTR_CERTIFICATION, RelationalOperator.EQ, certification), null, (int)group.Value, this));
            }
          }
          else
            numEmptyEntries += (int)group.Value;
        }
        if (numEmptyEntries > 0)
          result.Insert(0, new FilterValue("UR", Consts.RES_VALUE_UNRATED_TITLE, new EmptyFilter(SeriesAspect.ATTR_CERTIFICATION), null, numEmptyEntries, this));
        return result;
      }
      else
      {
        IList<FilterValue> result = new List<FilterValue>();
        IFilter emptyFilter = new EmptyFilter(SeriesAspect.ATTR_CERTIFICATION);
        int numEmptyItems = await cd.CountMediaItemsAsync(necessaryMIATypeIds, BooleanCombinationFilter.CombineFilters(BooleanOperator.And, filter, emptyFilter), true, showVirtual);
        if (numEmptyItems > 0)
          result.Add(new FilterValue("UR", Consts.RES_VALUE_UNRATED_TITLE, emptyFilter, null, numEmptyItems, this));
        List<string> usedFilters = new List<string>();
        foreach (var cert in CertificationMapper.GetSeriesCertificationsForCountry(CertificationHelper.DisplaySeriesCertificationCountry))
        {
          IEnumerable<CertificationMapping> certs = CertificationMapper.FindAllAllowedSeriesCertifications(cert.CertificationId);
          if (certs.Count() > 0)
          {
            List<string> certList = new List<string>(certs.Select(c => c.CertificationId).Except(usedFilters));
            usedFilters.AddRange(certList);
            IFilter certFilter = new InFilter(SeriesAspect.ATTR_CERTIFICATION, certList);
            int numItems = await cd.CountMediaItemsAsync(necessaryMIATypeIds, BooleanCombinationFilter.CombineFilters(BooleanOperator.And, filter, certFilter), true, showVirtual);
            result.Add(new FilterValue(cert.CertificationId, cert.Name, certFilter, null, numItems, this));
          }
        }
        return result;
      }
    }

    public override Task<ICollection<FilterValue>> GroupValuesAsync(ICollection<Guid> necessaryMIATypeIds, IFilter selectAttributeFilter, IFilter filter)
    {
      return Task.FromResult((ICollection<FilterValue>)null);
    }

    #endregion
  }
}
