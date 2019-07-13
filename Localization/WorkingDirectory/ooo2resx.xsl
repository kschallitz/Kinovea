<?xml version="1.0" encoding="UTF-8"?>
<!-- We must define several namespaces, because we need them to access -->
<!-- the document model of the in-memory OpenOffice.org document.      -->
<!-- If we want to access more parts of the document model, we must    -->
<!-- add there namesspaces here, too.                                  -->

<xsl:stylesheet 
	version="2.0" 
	xmlns:xsl="http://www.w3.org/1999/XSL/Transform" 
	xmlns:office="urn:oasis:names:tc:opendocument:xmlns:office:1.0"
	xmlns:table="urn:oasis:names:tc:opendocument:xmlns:table:1.0"
	xmlns:text="urn:oasis:names:tc:opendocument:xmlns:text:1.0"
	exclude-result-prefixes="office table text">

<xsl:output method = "text"/>
<xsl:output method = "xml" name="xml" indent = "yes" encoding = "UTF-8" omit-xml-declaration = "no"/>


<!-- Process the document model -->
<xsl:template match="/">

	<!-- Process all tables (=sheet) -->
	<xsl:apply-templates select="//table:table[2]"/>
	<xsl:apply-templates select="//table:table[3]"/>
	<xsl:apply-templates select="//table:table[4]"/>
	<xsl:apply-templates select="//table:table[5]"/>

</xsl:template>


<xsl:template match="table:table">
	
	<!-- Each language on this table (sheet) will be converted to its own file -->
	
	<xsl:call-template name="saveResx"><xsl:with-param name="col" select="3" /></xsl:call-template>
	<xsl:call-template name="saveResx"><xsl:with-param name="col" select="4" /></xsl:call-template>
	<xsl:call-template name="saveResx"><xsl:with-param name="col" select="5" /></xsl:call-template>
	<xsl:call-template name="saveResx"><xsl:with-param name="col" select="6" /></xsl:call-template>
	<xsl:call-template name="saveResx"><xsl:with-param name="col" select="7" /></xsl:call-template>
	<xsl:call-template name="saveResx"><xsl:with-param name="col" select="8" /></xsl:call-template>
	<xsl:call-template name="saveResx"><xsl:with-param name="col" select="9" /></xsl:call-template>
	<xsl:call-template name="saveResx"><xsl:with-param name="col" select="10" /></xsl:call-template>
	<xsl:call-template name="saveResx"><xsl:with-param name="col" select="11" /></xsl:call-template>
	<xsl:call-template name="saveResx"><xsl:with-param name="col" select="12" /></xsl:call-template>
	<xsl:call-template name="saveResx"><xsl:with-param name="col" select="13" /></xsl:call-template>
	<xsl:call-template name="saveResx"><xsl:with-param name="col" select="14" /></xsl:call-template>
	<xsl:call-template name="saveResx"><xsl:with-param name="col" select="15" /></xsl:call-template>
	<xsl:call-template name="saveResx"><xsl:with-param name="col" select="16" /></xsl:call-template>
	
</xsl:template>


<xsl:template name="saveResx">
	<xsl:param name="col" select="3" />

	<!-- 
	Build the filename dynamically from the sheet name and the language identifier (found in second row).
	 => "(sheet name)" + "Lang" + [".langId"] + ".resx"
	if there is no language id (ie:english) we don't output anything between "Lang" and ".resx" 
	-->
	
	<xsl:variable name="filename" select="concat(@table:name, if ( exists(table:table-row[2]/table:table-cell[$col]/text:p)) then concat('.', table:table-row[2]/table:table-cell[$col]/text:p) else '', '.resx')" />
	<xsl:result-document href="{$filename}" format="xml">
	
	
	<!-- Comments -->
	<xsl:comment> 
		www.kinovea.org
		
		This file was generated by a tool. It is not meant to be edited directly. 
		To change language strings, use the Kinovea-l14n-rev*.ods latest file and regenerate.
		This file follow MS Resx schema.
	</xsl:comment>
	<xsl:text>&#10;</xsl:text>
	
	<!-- Resx Header -->
	<root>
	  <xsd:schema id="root" xmlns="" xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:msdata="urn:schemas-microsoft-com:xml-msdata">
		<xsd:import namespace="http://www.w3.org/XML/1998/namespace" />
		<xsd:element name="root" msdata:IsDataSet="true">
		  <xsd:complexType>
			<xsd:choice maxOccurs="unbounded">
			  <xsd:element name="metadata">
				<xsd:complexType>
				  <xsd:sequence>
					<xsd:element name="value" type="xsd:string" minOccurs="0" />
				  </xsd:sequence>
				  <xsd:attribute name="name" use="required" type="xsd:string" />
				  <xsd:attribute name="type" type="xsd:string" />
				  <xsd:attribute name="mimetype" type="xsd:string" />
				  <xsd:attribute ref="xml:space" />
				</xsd:complexType>
			  </xsd:element>
			  <xsd:element name="assembly">
				<xsd:complexType>
				  <xsd:attribute name="alias" type="xsd:string" />
				  <xsd:attribute name="name" type="xsd:string" />
				</xsd:complexType>
			  </xsd:element>
			  <xsd:element name="data">
				<xsd:complexType>
				  <xsd:sequence>
					<xsd:element name="value" type="xsd:string" minOccurs="0" msdata:Ordinal="1" />
					<xsd:element name="comment" type="xsd:string" minOccurs="0" msdata:Ordinal="2" />
				  </xsd:sequence>
				  <xsd:attribute name="name" type="xsd:string" use="required" msdata:Ordinal="1" />
				  <xsd:attribute name="type" type="xsd:string" msdata:Ordinal="3" />
				  <xsd:attribute name="mimetype" type="xsd:string" msdata:Ordinal="4" />
				  <xsd:attribute ref="xml:space" />
				</xsd:complexType>
			  </xsd:element>
			  <xsd:element name="resheader">
				<xsd:complexType>
				  <xsd:sequence>
					<xsd:element name="value" type="xsd:string" minOccurs="0" msdata:Ordinal="1" />
				  </xsd:sequence>
				  <xsd:attribute name="name" type="xsd:string" use="required" />
				</xsd:complexType>
			  </xsd:element>
			</xsd:choice>
		  </xsd:complexType>
		</xsd:element>
	  </xsd:schema>
	  <resheader name="resmimetype">
		<value>text/microsoft-resx</value>
	  </resheader>
	  <resheader name="version">
		<value>2.0</value>
	  </resheader>
	  <resheader name="reader">
		<value>System.Resources.ResXResourceReader, System.Windows.Forms, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</value>
	  </resheader>
	  <resheader name="writer">
		<value>System.Resources.ResXResourceWriter, System.Windows.Forms, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</value>
	  </resheader>
	
	
	<!-- Resource strings -->
	<xsl:for-each select="table:table-row">
		<!-- First two rows are not used  -->
		<xsl:if test="position()>2">
			<!-- Retrieve all data -->
			<data>
				<xsl:for-each select="table:table-cell">
					<xsl:choose>
						<xsl:when test="position()=1">
							<xsl:attribute name="name"><xsl:value-of select="text:p"/></xsl:attribute>
							<xsl:attribute name="xml:space">preserve</xsl:attribute>
						</xsl:when>
						<!-- Get translation -->
						<xsl:when test="position()=$col">
							<value><xsl:value-of select="text:p"/></value>
						</xsl:when>
					</xsl:choose>
				</xsl:for-each>
			</data>
		</xsl:if>
	</xsl:for-each>
	
	
	<!-- Resx footer --> 
	</root>
	
	
	</xsl:result-document>
</xsl:template>



</xsl:stylesheet>