
[string]$sourceFiles = 'Enter Source files Location',
[string]$targetPackage = 'U:\SharePoint\TargetPackage', 
[string]$azureAccountName = 'migrationtospo' ,
[string]$azureAccountKey = '')


# Create azure containers and upload package into them, finally snapshotting all files
$global:al = Set-SPOMigrationPackageAzureSource -SourceFilesPath $sourceFiles -SourcePackagePath $targetPackage -AccountName $azureAccountName -AccountKey $azureAccountKey
# This displays the return Azure location
$al|Format-List