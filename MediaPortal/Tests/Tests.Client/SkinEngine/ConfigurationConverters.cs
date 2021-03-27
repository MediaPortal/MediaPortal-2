using MediaPortal.Common.Commands;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UI.SkinEngine.MpfElements.Converters;
using NUnit.Framework;
using System;
using System.Globalization;

namespace Tests.Client.SkinEngine
{
  [TestFixture]
  public class ConfigurationConverters
  {
    [Test, Description("Get configuration root element from ListItem")]
    [TestCase("ConfigurationMain->/General", "General")]
    [TestCase("Config: '/General/System'->/General/System/Diagnostics", "General")]
    public void GetConfigurationRoot(string actionName, string expectedRoot)
    {
      var listItem = CreateActionListItem(actionName);

      var crc = new ConfigurationRootConverter();
      var success = crc.Convert(listItem, typeof(String), null, CultureInfo.CurrentCulture, out object result);
      Assert.IsTrue(success);
      Assert.IsNotNull(result);
      Assert.IsNotEmpty(result as string);
      Assert.AreEqual(result as string, expectedRoot);
    }

    [Test, Description("Get configuration path from ListItem")]
    [TestCase("ConfigurationMain->/General", "General")]
    [TestCase("Config: '/General/System'->/General/System/Diagnostics", "General_System_Diagnostics")]
    public void GetConfigurationPath(string actionName, string expectedPath)
    {
      var listItem = CreateActionListItem(actionName);

      var crc = new ConfigurationPathConverter();
      var success = crc.Convert(listItem, typeof(String), null, CultureInfo.CurrentCulture, out object result);
      Assert.IsTrue(success);
      Assert.IsNotNull(result);
      Assert.IsNotEmpty(result as string);
      Assert.AreEqual(result as string, expectedPath);
    }

    [Test, Description("Get configuration root element from ListItem")]
    [TestCase("ConfigurationMain->/General", "SettingsMenu\\{0}.jpg", "SettingsMenu\\General.jpg")]
    [TestCase("Config: '/General/System'->/General/System/Diagnostics", "SettingsMenu\\{0}.jpg", "SettingsMenu\\General.jpg")]
    public void GetConfigurationRootWithFormat(string actionName, string format, string expectedRoot)
    {
      var listItem = CreateActionListItem(actionName);

      var crc = new ConfigurationRootConverter();
      var success = crc.Convert(listItem, typeof(String), format, CultureInfo.CurrentCulture, out object result);
      Assert.IsTrue(success);
      Assert.IsNotNull(result);
      Assert.IsNotEmpty(result as string);
      Assert.AreEqual(expectedRoot, result as string);
    }

    [Test, Description("Get configuration path from ListItem")]
    [TestCase("ConfigurationMain->/General", "SettingsMenu\\{0}.jpg", "SettingsMenu\\General.jpg")]
    [TestCase("Config: '/General/System'->/General/System/Diagnostics", "SettingsMenu\\{0}.jpg", "SettingsMenu\\General_System_Diagnostics.jpg")]
    public void GetConfigurationPathWithFormat(string actionName, string format, string expectedPath)
    {
      var listItem = CreateActionListItem(actionName);

      var crc = new ConfigurationPathConverter();
      var success = crc.Convert(listItem, typeof(String), format, CultureInfo.CurrentCulture, out object result);
      Assert.IsTrue(success);
      Assert.IsNotNull(result);
      Assert.IsNotEmpty(result as string);
      Assert.AreEqual(expectedPath, result as string);
    }


    [Test, Description("Get configuration level from ListItem")]
    [TestCase("ConfigurationMain->/General", 0)]
    [TestCase("Config: '/General/System'->/General/System/Diagnostics", 2)]
    public void GetConfigurationLevel(string actionName, int expectedLevel)
    {
      var itemsList = new ItemsList();
      var listItem = CreateActionListItem(actionName);
      itemsList.Add(listItem);

      var crc = new ConfigurationLevelConverter();
      var success = crc.Convert(itemsList, typeof(String), null, CultureInfo.CurrentCulture, out object result);
      Assert.IsTrue(success);
      Assert.IsNotNull(result);
      Assert.AreEqual((int)result, expectedLevel);
    }

    private ListItem CreateActionListItem(string actionName)
    {
      WorkflowAction action = new PushTransientStateNavigationTransition(new Guid(), actionName, actionName, null, null, null, null);
      ListItem item = new ListItem("Name", action.DisplayTitle)
      {
        Command = new MethodDelegateCommand(action.Execute),
        Enabled = true
      };
      item.AdditionalProperties[AbstractConfigurationConverter.KEY_ITEM_ACTION] = action;
      item.SetLabel("Help", action.HelpText);
      return item;
    }
  }
}
