
[string]$FileContainerUri = '',
[string]$PackageContainerUri = '',
[string]$targetWeb = 'Enter Sharepoint team site name')

# Submit package data to site collection to create new migration job

Submit-SPOMigrationJob -TargetWebUrl $targetWeb -FileContainerUri $FileContainerUri -PackageContainerUri $PackageContainerUri -Credentials $creds