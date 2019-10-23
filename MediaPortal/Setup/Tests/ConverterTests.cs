using System.Globalization;
using System.Windows;
using Microsoft.Tools.WindowsInstallerXml.Bootstrapper;
using MP2BootstrapperApp;
using Xunit;

namespace Tests
{
  public class ConverterTests
  {
    [Fact]
    public void ReturnHidden_When_PackageIsPresentAndConverterParameterIsAbsent()
    {
      // Arrange
      PackageStateToVisibilityConverter converter = new PackageStateToVisibilityConverter();
      
      // Act
      object result = converter.Convert(PackageState.Present, null, PackageState.Absent, CultureInfo.InvariantCulture);

      // Assert
      Assert.Equal(Visibility.Hidden, result);
    }
    
    [Fact]
    public void ReturnHidden_When_PackageIsAbsentAndConverterParameterIsPresent()
    {
      // Arrange
      PackageStateToVisibilityConverter converter = new PackageStateToVisibilityConverter();
      
      // Act
      object result = converter.Convert(PackageState.Absent, null, PackageState.Present, CultureInfo.InvariantCulture);

      // Assert
      Assert.Equal(Visibility.Hidden, result);
    }
    
    [Fact]
    public void ReturnVisible_When_PackageIsPresentAndConverterParameterIsPresent()
    {
      // Arrange
      PackageStateToVisibilityConverter converter = new PackageStateToVisibilityConverter();
      
      // Act
      object result = converter.Convert(PackageState.Present, null, PackageState.Present, CultureInfo.InvariantCulture);

      // Assert
      Assert.Equal(Visibility.Visible, result);
    }
    
    [Fact]
    public void ReturnVisible_When_PackageIsAbsentAndConverterParameterIsAbsent()
    {
      // Arrange
      PackageStateToVisibilityConverter converter = new PackageStateToVisibilityConverter();
      
      // Act
      object result = converter.Convert(PackageState.Absent, null, PackageState.Absent, CultureInfo.InvariantCulture);

      // Assert
      Assert.Equal(Visibility.Visible, result);
    }
  }
}
