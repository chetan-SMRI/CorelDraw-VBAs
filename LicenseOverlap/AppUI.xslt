<?xml version="1.0"?>
<xsl:stylesheet version="1.0"
                xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
                xmlns:frmwrk="Corel Framework Data">
  <xsl:output method="xml" encoding="UTF-8" indent="yes"/>

  <frmwrk:uiconfig>
    <frmwrk:applicationInfo userConfiguration="true"/>
  </frmwrk:uiconfig>

  <xsl:template match="node()|@*">
    <xsl:copy>
      <xsl:apply-templates select="node()|@*"/>
    </xsl:copy>
  </xsl:template>

  <xsl:template match="uiConfig/items">
    <xsl:copy>
      <xsl:apply-templates select="node()|@*"/>
      <itemData guid="505032A5-19B4-429A-85B5-6767F742DC51"
                type="button"
                onInvoke="*Bind(DataSource=LicenseOverlapDS;Path=MenuItemCommand)"
                caption="*Bind(DataSource=LicenseOverlapDS;Path=Caption)"
                icon="*Bind(DataSource=LicenseOverlapDS;Path=Icon)"
                enable="true"/>
      <itemData guid="1C9197BF-8060-4C0F-8A4E-9817466BF0B8"
                type="wpfhost"
                hostedType="Addons\LicenseOverlap\LicenseOverlap.CorelAddon,LicenseOverlap.ControlUI"
                enable="true"/>
    </xsl:copy>
  </xsl:template>

  <xsl:template match="uiConfig/commandBars">
    <xsl:copy>
      <xsl:apply-templates select="node()|@*"/>
      <container>
        <item dock="fill" margin="0,0,0,0" guidRef="1C9197BF-8060-4C0F-8A4E-9817466BF0B8"/>
      </container>
    </xsl:copy>
  </xsl:template>
</xsl:stylesheet>
