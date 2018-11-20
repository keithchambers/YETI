param([string]$folderPath = 'D:')

Get-ChildItem $folderPath -recurse -Directory | ForEach-Object{
    $props = @{
        Folder = $_.FullName
        Count = (Get-ChildItem -Path $_.Fullname -File | Measure-Object).Count
    }
    New-Object PSObject -Property $props
} | Select-Object Folder,Count  