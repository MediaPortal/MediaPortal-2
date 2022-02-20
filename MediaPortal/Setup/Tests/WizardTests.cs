#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
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

using MP2BootstrapperApp.ChainPackages;
using MP2BootstrapperApp.Models;
using MP2BootstrapperApp.WizardSteps;
using NSubstitute;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Tests.Mocks;
using Xunit;

namespace Tests
{
  public class WizardTests
  {
    [Fact]
    void Should_IncludeOptionalPackagesInCustomStep_If_Not_Installed()
    {
      IList<IBundlePackage> packages = MockBundlePackages.Create();
      IBootstrapperApplicationModel applicationModel = Substitute.For<IBootstrapperApplicationModel>();
      applicationModel.BundlePackages.Returns(new ReadOnlyCollection<IBundlePackage>(packages));

      InstallCustomStep customStep = new InstallCustomStep(applicationModel);

      IBundlePackage availablePackage = customStep.AvailablePackages.FirstOrDefault(p => p.GetId() == PackageId.LAVFilters);

      Assert.NotNull(availablePackage);
    }

    [Fact]
    void Should_Not_IncludeOptionalPackagesInCustomStep_If_Installed()
    {
      IList<IBundlePackage> packages = MockBundlePackages.Create(new[] { PackageId.LAVFilters });
      IBootstrapperApplicationModel applicationModel = Substitute.For<IBootstrapperApplicationModel>();
      applicationModel.BundlePackages.Returns(new ReadOnlyCollection<IBundlePackage>(packages));
      
      InstallCustomStep customStep = new InstallCustomStep(applicationModel);

      IBundlePackage availablePackage = customStep.AvailablePackages.FirstOrDefault(p => p.GetId() == PackageId.LAVFilters);

      Assert.Null(availablePackage);
    }
  }
}
