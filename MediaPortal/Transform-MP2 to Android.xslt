<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
    xmlns:msxsl="urn:schemas-microsoft-com:xslt" exclude-result-prefixes="msxsl"
>
  <xsl:output method="xml" indent="yes" encoding="utf-8"/>

  <xsl:template match="/Language">
    <resources>
      <xsl:apply-templates select="Section/String"/>
    </resources>
  </xsl:template>

  <xsl:template match="String">
    <string>
      <xsl:attribute name="name">
        <xsl:value-of select="../@Name"/>/<xsl:value-of select="@Name"/>
      </xsl:attribute>
      <xsl:value-of select="@Text"/>
    </string>
  </xsl:template>
</xsl:stylesheet>