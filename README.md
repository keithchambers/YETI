
# Introduction
	This document describes the environment details (Servers etc..) related to Yammer eDiscovery (YETI) application.


# About Yammer Extraction Tool (YET)
	Yammer Extraction Tool (YETI) is used to download, process and upload yammer data related to all groups (public and private) to SharePoint for future purposes. Yammer Export API is used to download data. Data uploaded to SP is crawled and indexed and will be ready for eDiscovery searches.        
Data Processing:
	Export API gives output Zip file for each day. Data is loaded into SQL tables & extract attachments to File share. User information is fetched using Users API. We download missing attachments and notes (Pages). HTML file for each thread with related conversations and attachments is prepared. Then folder is created for each Thread and Html file and attachments are placed.


# Installation Steps:
Please refer below documentation for Yammer Extraction Tool (YETI) installation steps.
https://github.com/Microsoft/YETI/blob/master/GitHub_YETI_Installation.docx


# Contributing

This project welcomes contributions and suggestions.  Most contributions require you to agree to a
Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us
the rights to use your contribution. For details, visit https://cla.microsoft.com.

When you submit a pull request, a CLA-bot will automatically determine whether you need to provide
a CLA and decorate the PR appropriately (e.g., label, comment). Simply follow the instructions
provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.



