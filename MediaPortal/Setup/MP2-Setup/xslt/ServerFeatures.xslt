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
    <Feature Id="Server"
             Level="1"
             AllowAdvertise="no"
             ConfigurableDirectory="INSTALLDIR_SERVER"
             Title="!(loc.F_Server)"
             Description="!(loc.F_Server_Desc)">

      <ComponentRef Id="SERVER.DATA.FOLDER" />
      <ComponentRef Id="SERVER.CONFIG.FOLDER" />
      <ComponentRef Id="SERVER.LOG.FOLDER" />
      <ComponentRef Id="SERVER.DATABASE.FOLDER" />

      <ComponentRef Id="InstallDirServerRegistry" />

      <ComponentRef Id="S__Defaults" />

      <ComponentRef Id="ClientManager_create_1_0" />
      <ComponentRef Id="MediaLibrary_create_1_0" />
      <ComponentRef Id="MediaPortal_Basis_create_1_0" />

      <ComponentRef Id="MP2Server" />
      <ComponentRef Id="MP2Server.config" />
      <ComponentRef Id="MP2Server.Desktop.Shortcut" />
      <ComponentRef Id="MP2Server.StartMenu.Shortcut" />

      <ComponentRef Id="S__HttpServer" />
      <ComponentRef Id="S__log4net" />
      <ComponentRef Id="MediaPortal.Backend" />
      <ComponentRef Id="S__MediaPortal.Common" />
      <ComponentRef Id="S__MediaPortal.Utilities" />
      <ComponentRef Id="S_Microsoft.WindowsAPICodePack"/>
      <ComponentRef Id="S_Microsoft.WindowsAPICodePack.Shell"/>
      <ComponentRef Id="S__UPnP" />

      <Feature Id="Server.Plugins"
               Level="1"
               Absent="disallow"
               AllowAdvertise="no"
               Title="!(loc.F_Server_Plugins)"
               Description="!(loc.F_Server_Plugins_Desc)">
        <ComponentRef Id="EmptyComponent_WindowsInstallerBugWorkaround" />
        <xsl:for-each select="/wix:Wix/wix:Fragment/wix:DirectoryRef/wix:Directory">
          <Feature Level="1" AllowAdvertise="no" Description="Plugin">
            <xsl:attribute name="Id">
              <xsl:value-of select="concat('S_', translate(@Name, ' ', '_'))"/>
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
