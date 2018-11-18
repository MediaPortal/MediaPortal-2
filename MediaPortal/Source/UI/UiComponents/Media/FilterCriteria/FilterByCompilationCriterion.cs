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

using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.UiComponents.Media.General;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MediaPortal.UiComponents.Media.FilterCriteria
{
  public class FilterByCompilationCriterion : SimpleMLFilterCriterion
  {
    public FilterByCompilationCriterion(MediaItemAspectMetadata.AttributeSpecification attributeType)
      : base(attributeType)
    {
    }

    #region Base overrides

    public override Task<ICollection<FilterValue>> GroupValuesAsync(ICollection<Guid> necessaryMIATypeIds, IFilter selectAttributeFilter, IFilter filter)
    {
      return Task.FromResult((ICollection<FilterValue>)null);
    }

    protected override string GetDisplayName(object groupKey)
    {
      bool? isCompilation = groupKey as bool?;
      if (!isCompilation.HasValue)
        return string.Empty;
      return isCompilation.Value ? Consts.RES_COMPILATION_FILTER_COMPILED : Consts.RES_COMPILATION_FILTER_UNCOMPILED;
    }

    #endregion
  }
}
