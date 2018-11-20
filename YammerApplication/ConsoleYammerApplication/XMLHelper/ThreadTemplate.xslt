<?xml version="1.0"?>

<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform" version="1.0">
  <xsl:output method="html"/>

  <xsl:template match="/">
    <h2>
      <xsl:value-of select="/Thread/ParentMessage/Group_Name"/>
    </h2>
    <form>
      <fieldset style="border: solid;" >
        <xsl:apply-templates/>
      </fieldset>
    </form>
  </xsl:template>

  
  <xsl:template match="/Thread/ParentMessage">
    <dl>
      <dt>
        <a href="#" style="color:firebrick;font-size:larger">
          <xsl:value-of select="PSender_fullname"/> 
        </a> <xsl:text disable-output-escaping="yes">&amp;emsp;</xsl:text> <span style="color:firebrick;font-size:small">
          <xsl:value-of select="PSender_email"/> </span> -- <span style="color: blue;font-size:small">
          <xsl:value-of select="PTimestamp"/>
        </span>
      </dt>
      <p>
        <xsl:value-of select="PBody" disable-output-escaping="yes"/>
      </p>
    </dl>
    <xsl:if test="Pattachment !=''">
      <nav>
        <xsl:text disable-output-escaping="yes">&amp;emsp;Attachments</xsl:text>      
        <br />
        <td>
          <xsl:value-of select="Pattachment" disable-output-escaping="yes"/>
        </td>
      </nav>
    </xsl:if>
    <ul style="background-color: lightgrey;font-size:small">
      <xsl:for-each select="/Thread/ParentMessage/ReplyMessage/Message">
        <dl>
          <dt>
            <a href="#" style="color:black;font-size:medium">
              <xsl:value-of select="Sender_fullname"/>
            </a> <xsl:text disable-output-escaping="yes">&amp;emsp;</xsl:text> <span style="color:black;font-size:small">
          <xsl:value-of select="Sender_email"/> </span> -- <span style="color: blue;font-size:smaller">
              <xsl:value-of select="Timestamp"/>
            </span>
          </dt>
          <p>
            <xsl:value-of select="Body" disable-output-escaping="yes"/>
          </p>
        </dl>
        <xsl:if test="attachment !=''">
          <nav>
            <xsl:text disable-output-escaping="yes">&amp;emsp;Attachments</xsl:text>
            <br />
            <td>
              <xsl:value-of select="attachment" disable-output-escaping="yes"/>
            </td>
          </nav>
        </xsl:if>
        <hr></hr>
      </xsl:for-each>
    </ul>
  </xsl:template>

  <xsl:template match="/Thread/ReplyMessage">
    <ul style="background-color: lightgrey;font-size:small">
      <xsl:for-each select="Message">
        <dl>
          <dt>
            <a href="#" style="color:black;font-size:medium">
              <xsl:value-of select="Sender_fullname"/>
            </a> <xsl:text disable-output-escaping="yes">&amp;emsp;</xsl:text> <span style="color:black;font-size:small">
                  <xsl:value-of select="Sender_email"/> </span> -- <span style="color: blue;font-size:smaller">
              <xsl:value-of select="Timestamp"/>
            </span>
          </dt>
          <p>
            <xsl:value-of select="Body"  disable-output-escaping="yes"/>
          </p>
        </dl>
        <xsl:if test="attachment !=''">
          <nav>
            <xsl:text disable-output-escaping="yes">&amp;emsp;Attachments</xsl:text>
            <br />
            <td>
              <xsl:value-of select="attachment" disable-output-escaping="yes"/>
            </td>
          </nav>
        </xsl:if>
        <hr></hr>
      </xsl:for-each>
    </ul>
  </xsl:template>
</xsl:stylesheet>