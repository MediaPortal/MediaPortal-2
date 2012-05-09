<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
    xmlns:msxsl="urn:schemas-microsoft-com:xslt" exclude-result-prefixes="msxsl"
>
  <xsl:output method="xml" indent="yes" encoding="utf-8" />
 
  <xsl:key name="sections" match="/resources/string" use="substring-before(@name, '/')"/>
 
  <xsl:template match="/resources">
    <Language>
      <xsl:for-each select="string[count(.|key('sections', substring-before(@name, '/'))[1]) = 1]">
        <Section>
          <xsl:attribute name="Name">
            <xsl:value-of select="substring-before(@name, '/')" />
          </xsl:attribute>
          <xsl:for-each select="key('sections', substring-before(@name, '/'))">
            <String>
              <xsl:attribute name="Name">
                <xsl:value-of select="substring-after(@name, '/')"/>
              </xsl:attribute>
              <xsl:attribute name="Text">
                <xsl:value-of select="text()"/>
              </xsl:attribute>
            </String>
          </xsl:for-each>
        </Section>       
      </xsl:for-each>
    </Language>
  </xsl:template>
 
</xsl:stylesheet>