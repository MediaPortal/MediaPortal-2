<?xml version="1.0"?>

<!--
  Note about XML namespace usage:
  - Default namespace is necessary for the output to be located in the default namespace
  - wix namespace necessary to match the input elements; default namespace doesn't work here
  - exclude-result-prefixes necessary to remove the wix namespace in the result document
-->
<xsl:stylesheet version="1.0"
    xmlns="http://schemas.microsoft.com/wix/2006/wi"
    xmlns:wix="http://schemas.microsoft.com/wix/2006/wi"
    xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
    xmlns:fn="http://www.w3.org/2005/xpath-functions"
    exclude-result-prefixes="wix">

  <xsl:output method="xml" indent="yes"/>

  <!-- Contains the frame of the features output; generates the Wix element and other enclosing elements -->
  <xsl:include href="PluginComponents2Features.xslt"/>

  <!-- This template will be called from our include file -->
  <xsl:template name="CreatePluginsDirectoryFeatures">
    <Feature Id="Client"
              Level="1"
              AllowAdvertise="no"
              ConfigurableDirectory="INSTALLDIR_CLIENT"
              Title="!(loc.F_Client)"
              Description="!(loc.F_Client_Desc)">

      <ComponentRef Id="CLIENT.DATA.FOLDER" />
      <ComponentRef Id="CLIENT.CONFIG.FOLDER" />
      <ComponentRef Id="CLIENT.LOG.FOLDER" />

      <ComponentRef Id="InstallDirClientRegistry" />

      <ComponentRef Id="Defaults" />

      <ComponentRef Id="BASSLicense" />
      <ComponentRef Id="MediaPortalLicense" />

      <ComponentRef Id="MP2Client" />
      <ComponentRef Id="MP2Client.config" />
      <ComponentRef Id="MP2Client.Splashscreen" />
      <ComponentRef Id="MP2Client.Desktop.Shortcut" />
      <ComponentRef Id="MP2Client.StartMenu.Shortcut" />

      <ComponentRef Id="HttpServer" />
      <ComponentRef Id="log4net" />
      <ComponentRef Id="MediaPortal.Common" />
      <ComponentRef Id="MediaPortal.UI" />
      <ComponentRef Id="MediaPortal.Utilities" />
      <ComponentRef Id="Microsoft.WindowsAPICodePack"/>
      <ComponentRef Id="Microsoft.WindowsAPICodePack.Shell"/>
      <ComponentRef Id="UPnP" />

      <Feature Id="Client.Plugins"
                Level="1"
                Absent="disallow"
                AllowAdvertise="no"
                Title="!(loc.F_Client_Plugins)"
                Description="!(loc.F_Client_Plugins_Desc)">
        <ComponentRef Id="EmptyComponent_WindowsInstallerBugWorkaround" />
        <xsl:for-each select="/wix:Wix/wix:Fragment/wix:DirectoryRef/wix:Directory">
          <Feature Level="1" AllowAdvertise="no" Description="Plugin">
            <xsl:attribute name="Id">
              <xsl:value-of select="concat('C_', translate(@Name, ' ', '_'))"/>
            </xsl:attribute>
            <xsl:attribute name="Title">
              <xsl:value-of select="@Name"/>
            </xsl:attribute>
            <xsl:call-template name="CollectComponentReferences"/>
          </Feature>
        </xsl:for-each>
      </Feature>
    </Feature>
  </xsl:template>
</xsl:stylesheet>
