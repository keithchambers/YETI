
[string]$sourceFiles = 'Enter Source File Path',
[string]$sourcePackage = 'U:\SharePoint\SourcePackage',
[string]$targetPackage = 'U:\SharePoint\TargetPackage', 
[string]$subfolder = 'Private_Conversations\6',

[string]$targetWeb = 'Enter Sharepoint team site name', 

[string]$targetDocLib = 'Yammer2009')

# Create new package from file share without security (faster)

New-SPOMigrationPackage -SourceFilesPath $sourceFiles -OutputPackagePath $sourcePackage -IncludeFileSharePermissions -TargetWebUrl $targetWeb -TargetDocumentLibraryPath $targetDocLib -TargetDocumentLibrarySubFolderPath $subfolder

# Convert package to a targeted one by looking up data in target site collection
ConvertTo-SPOMigrationTargetedPackage -SourceFilesPath $sourceFiles -SourcePackagePath $sourcePackage -OutputPackagePath $targetPackage -TargetWebUrl $targetWeb -TargetDocumentLibraryPath $targetDocLib -TargetDocumentLibrarySubFolderPath $subfolder -Credentials $creds