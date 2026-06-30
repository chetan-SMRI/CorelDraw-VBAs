<?xml version="1.0"?>
<xsl:stylesheet version="1.0"
                xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
                xmlns:frmwrk="Corel Framework Data"
                exclude-result-prefixes="frmwrk">
  <xsl:output method="xml" encoding="UTF-8" indent="yes"/>

  <frmwrk:uiconfig>
    <frmwrk:compositeNode xPath="/uiConfig/commandBars/commandBarData[@guid='1056b8d8-9185-46ee-af8e-77c7ba383a4e']"/>
    <frmwrk:compositeNode xPath="/uiConfig/commandBars/commandBarData[@guid='c2b44f69-6dec-444e-a37e-5dbf7ff43dae']"/>
    <frmwrk:compositeNode xPath="/uiConfig/frame"/>
  </frmwrk:uiconfig>

  <xsl:template match="node()|@*">
    <xsl:copy>
      <xsl:apply-templates select="node()|@*"/>
    </xsl:copy>
  </xsl:template>

  <xsl:template match="commandBarData[@guid='1056b8d8-9185-46ee-af8e-77c7ba383a4e']/menu/item[@guidRef='6154a087-059d-4d9c-bac4-836b9377af23']">
    <xsl:copy>
      <xsl:apply-templates select="@*|node()"/>
    </xsl:copy>
    <xsl:if test="not(../item[@guidRef='505032A5-19B4-429A-85B5-6767F742DC51'])">
      <item guidRef="505032A5-19B4-429A-85B5-6767F742DC51"/>
    </xsl:if>
  </xsl:template>

  <xsl:template match="commandBarData[@guid='c2b44f69-6dec-444e-a37e-5dbf7ff43dae']/toolbar">
    <xsl:copy>
      <xsl:apply-templates select="node()|@*"/>
      <xsl:if test="not(./item[@guidRef='1C9197BF-8060-4C0F-8A4E-9817466BF0B8'])">
        <item guidRef="1C9197BF-8060-4C0F-8A4E-9817466BF0B8"/>
      </xsl:if>
      <xsl:if test="not(./item[@guidRef='505032A5-19B4-429A-85B5-6767F742DC51'])">
        <item guidRef="505032A5-19B4-429A-85B5-6767F742DC51"/>
      </xsl:if>
    </xsl:copy>
  </xsl:template>
</xsl:stylesheet>
