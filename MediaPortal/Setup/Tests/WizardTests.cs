using Microsoft.Tools.WindowsInstallerXml.Bootstrapper;
using MP2BootstrapperApp;
using MP2BootstrapperApp.BootstrapperWrapper;
using MP2BootstrapperApp.Models;
using MP2BootstrapperApp.ViewModels;
using MP2BootstrapperApp.WizardSteps;
using NSubstitute;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Xunit;

namespace Tests
{
  public class WizardTests
  {
    [Fact] //TODO: mock bundle packages 
    public void Should_GoToNewTypeStep_When_PreviousStepIsWelcomeAndGoForward()
    {
      // Arrange
      IBootstrapperApplicationModel applicationModel = Substitute.For<IBootstrapperApplicationModel>();
      IBootstrapperApp bootstrapperApp = Substitute.For<IBootstrapperApp>();
      applicationModel.BootstrapperApplication.Returns(bootstrapperApp);
      applicationModel.BundlePackages.Returns(new List<BundlePackage>().AsReadOnly());
      //IDispatcher dispatcher = Substitute.For<IDispatcher>();
      //InstallWizardViewModel viewModel = new InstallWizardViewModel(applicationModel, dispatcher);

      Wizard wizard = new Wizard(new InstallWelcomeStep(applicationModel));

      // Act
      wizard.GoNext();

      // Assert
      Assert.True(wizard.Step is InstallNewTypeStep);
      //Assert.True(viewModel.CurrentPage is InstallNewTypePageViewModel);
    }

    [Fact] //TODO: mock bundle packages
    public void Should_GoToOverviewStep_When_PreviousStepIsNewTypeAndGoForward()
    {
      // Arrange
      IBootstrapperApplicationModel applicationModel = Substitute.For<IBootstrapperApplicationModel>();
      applicationModel.BundlePackages.Returns(new List<BundlePackage>().AsReadOnly());
      //IDispatcher dispatcher = Substitute.For<IDispatcher>();
      //InstallWizardViewModel viewModelMain = new InstallWizardViewModel(applicationModel, dispatcher);
      //InstallNewTypePageViewModel viewModelNewInstall = new InstallNewTypePageViewModel(viewModelMain);
      //viewModelMain.CurrentPage = viewModelNewInstall;
      //viewModelNewInstall.InstallType = InstallType.Client;
      Wizard wizard = new Wizard(new InstallNewTypeStep(applicationModel));

      // Act
      wizard.GoNext();

      // Assert
      Assert.True(wizard.Step is InstallOverviewStep);
      //Assert.True(viewModelMain.CurrentPage is InstallOverviewPageViewModel);
    }

    public void TestsWithBurnEngine()
    {
      IBootstrapperApp bootstrapperApp = Substitute.For<IBootstrapperApp>();
      bootstrapperApp.WrapperDetectRelatedBundle += Raise.EventWith(new DetectRelatedBundleEventArgs("", RelationType.Update, "", true, 2, RelatedOperation.Install));
    }
  }
}
