param([string]$OutputFolder=(Join-Path $PSScriptRoot '..\..\artifacts\BPLO-Setup'))
$ErrorActionPreference='Stop'
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$output=[IO.Path]::GetFullPath($OutputFolder)
if(Test-Path -LiteralPath $output){throw 'Output already exists; choose a new folder. Existing packages are not overwritten.'}
New-Item -ItemType Directory -Path $output|Out-Null
& dotnet publish (Join-Path $repo 'BusinessPermitLicensingSystem.Web\BusinessPermitLicensingSystem.Web.csproj') -c Release -r win-x64 --self-contained false -o (Join-Path $output 'payload\Web')
if($LASTEXITCODE -ne 0){throw 'Publish failed. Package is incomplete.'}
Copy-Item -LiteralPath (Join-Path $repo 'Setup-BPLO.bat') -Destination $output
New-Item -ItemType Directory -Path (Join-Path $output 'operations')|Out-Null
foreach($folder in @('setup','database','backup')){Copy-Item -LiteralPath (Join-Path $repo "operations\$folder") -Destination (Join-Path $output 'operations') -Recurse}
# Host configuration is supplied locally, never carried from the build PC.
foreach($file in @('setup.json','backup.config.json')){
 $candidate=if($file -eq 'setup.json'){Join-Path $output "operations\setup\$file"}else{Join-Path $output "operations\backup\$file"}
 if(Test-Path -LiteralPath $candidate){Remove-Item -LiteralPath $candidate}
}
$development=Join-Path $output 'payload\Web\appsettings.Development.json'
if(Test-Path -LiteralPath $development){Remove-Item -LiteralPath $development}
$production=Join-Path $output 'payload\Web\appsettings.Production.json'
if(Test-Path -LiteralPath $production){throw 'Unexpected Production configuration in source publish; review before packaging.'}
$unexpected=Get-ChildItem -LiteralPath $output -Recurse -File|Where-Object {$_.Extension -in @('.bak','.db','.mdf','.ldf') -or $_.Name -match '(?i)(credentials|secrets)'}
if($unexpected){throw 'Private data detected; package rejected.'}
Get-ChildItem -LiteralPath $output -Recurse -File|ForEach-Object {
 [pscustomobject]@{Path=$_.FullName.Substring($output.Length+1);SHA256=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash}
}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $output 'package-manifest.json') -Encoding UTF8
Write-Host "Package built: $output. Copy the entire folder, not just the batch file."
