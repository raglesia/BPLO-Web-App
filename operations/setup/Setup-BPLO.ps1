param([string]$ConfigPath=(Join-Path $PSScriptRoot 'setup.json'),[switch]$Apply)
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
$transcript=$false
function Require($ok,$message){if(!$ok){throw $message}}
function GrantFolder($path,$identity,$rights){
 $acl=Get-Acl -LiteralPath $path
 $acl.AddAccessRule([Security.AccessControl.FileSystemAccessRule]::new($identity,$rights,'ContainerInherit,ObjectInherit','None','Allow'))
 Set-Acl -LiteralPath $path -AclObject $acl
}
function VendorInstaller($path,$arguments){
 Require (Test-Path -LiteralPath $path -PathType Leaf) "Installer missing: $path"
 $signature=Get-AuthenticodeSignature -LiteralPath $path
 Require ($signature.Status -eq 'Valid' -and $signature.SignerCertificate.Subject -match 'Microsoft Corporation') 'Only a valid Microsoft-signed installer is accepted.'
 $parameters=@{FilePath=$path;Wait=$true;PassThru=$true}
 if($arguments){$parameters.ArgumentList=$arguments}
 $result=Start-Process @parameters
 Require ($result.ExitCode -in @(0,3010)) "Installer failed: $($result.ExitCode)"
 Require ($result.ExitCode -ne 3010) 'Restart Windows, then rerun setup. No application installation performed yet.'
}
try {
 Require (Test-Path -LiteralPath $ConfigPath) 'Copy setup.example.json to setup.json and complete approved host settings first.'
 $cfg=Get-Content -LiteralPath $ConfigPath -Raw|ConvertFrom-Json
 foreach($key in @('Server','ServerIdentity','Database','HostName','CertificateThumbprint','WebFolder','SiteName','PoolName','LanSubnet')){
  Require (![string]::IsNullOrWhiteSpace($cfg.$key) -and $cfg.$key -notmatch 'REPLACE') "Complete $key in setup.json."
 }
 Require ($cfg.Database -cmatch '^[A-Za-z][A-Za-z0-9_]{0,127}$' -and $cfg.Database -notin @('master','model','msdb','tempdb','BPLS_Dev') -and $cfg.Database -notmatch '^BPLS_Staging') 'Protected or invalid production database name.'
 Require ($cfg.DatabaseMode -in @('Create','Validate')) 'DatabaseMode must be Create or Validate.'
 Require ($cfg.Server -match ('^(\.|localhost|'+[regex]::Escape($env:COMPUTERNAME)+')(\\[A-Za-z0-9_]+)?$')) 'This installer supports SQL and IIS on the same host only.'
 Require ($cfg.SiteName -match '^[A-Za-z][A-Za-z0-9_-]*$' -and $cfg.PoolName -match '^[A-Za-z][A-Za-z0-9_-]*$') 'Use simple IIS site/pool names.'
 Require ($cfg.HostName -match '^[A-Za-z0-9][A-Za-z0-9.-]+$') 'Invalid approved DNS hostname.'
 Require ($cfg.CertificateThumbprint -match '^[A-Fa-f0-9]{40}$') 'Invalid certificate thumbprint.'
 Require ([int]$cfg.HttpsPort -ge 1 -and [int]$cfg.HttpsPort -le 65535) 'Invalid HTTPS port.'
 Require ($cfg.LanSubnet -match '^\d{1,3}(\.\d{1,3}){3}/\d{1,2}$') 'LanSubnet must be an explicit IPv4 CIDR range.'
 $parts=$cfg.LanSubnet.Split('/');Require ([int]$parts[1] -ge 8 -and [int]$parts[1] -le 32) 'LAN prefix must be /8 through /32.'
 foreach($part in $parts[0].Split('.')){Require ([int]$part -le 255) 'Invalid LAN address.'}
 $web=[IO.Path]::GetFullPath($cfg.WebFolder).TrimEnd('\')
 Require ($web -match '^[A-Za-z]:\\[^\\]+\\[^\\]+' -and $web -notmatch '(?i)\\(Windows|Users|Program Files)(\\|$)') 'Use a dedicated absolute application folder, for example C:\BPLO\Web.'
 $package=Join-Path $PSScriptRoot '..\..\payload\Web'
 Write-Host "PLAN: Production $($cfg.DatabaseMode) $($cfg.Database) on $($cfg.Server), identity $($cfg.ServerIdentity)"
 Write-Host "IIS: $($cfg.SiteName), HTTPS $($cfg.HostName):$($cfg.HttpsPort), folder $web, LAN $($cfg.LanSubnet)"
 Write-Host 'No records are seeded, transferred, deleted or restored. Existing installations are never overwritten.'
 if(!$Apply){Write-Host 'Plan only. Run Setup-BPLO.bat -Apply after reviewing setup.json.';exit 0}
 $principal=[Security.Principal.WindowsPrincipal]::new([Security.Principal.WindowsIdentity]::GetCurrent())
 Require ($principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) 'Run the batch file from an administrator terminal.'
 $log=Join-Path $env:TEMP ('BPLO-Setup-'+(Get-Date -Format 'yyyyMMdd-HHmmss')+'.log')
 Start-Transcript -Path $log|Out-Null;$transcript=$true;Write-Host "Setup log: $log"
 Require (!(Test-Path -LiteralPath $web)) 'Application folder already exists. Inspect it; this fresh installer does not perform upgrades.'
 foreach($file in @('web.config','BusinessPermitLicensingSystem.Web.dll','appsettings.json')){Require (Test-Path -LiteralPath (Join-Path $package $file)) "Published payload missing $file. Run Build-SetupPackage.ps1 on the build PC."}
 if($cfg.InstallPrerequisites){
  Enable-WindowsOptionalFeature -Online -FeatureName IIS-WebServerRole,IIS-WebServer,IIS-ManagementConsole -All -NoRestart|ForEach-Object {Require (!$_.RestartNeeded) 'Restart Windows and rerun setup.'}
  if($cfg.SqlInstaller){VendorInstaller $cfg.SqlInstaller ''}
  if($cfg.HostingBundleInstaller){VendorInstaller $cfg.HostingBundleInstaller '/install /quiet /norestart'}
 }
 Require (Test-Path "$env:ProgramFiles\IIS\Asp.Net Core Module\V2\aspnetcorev2.dll") '.NET 10 Hosting Bundle is required after IIS installation.'
 Require (Test-Path "$env:ProgramFiles\dotnet\shared\Microsoft.AspNetCore.App") 'ASP.NET Core runtime missing.'
 Require (@(Get-ChildItem "$env:ProgramFiles\dotnet\shared\Microsoft.AspNetCore.App" -Directory|Where-Object Name -like '10.*').Count -gt 0) '.NET 10 ASP.NET Core runtime required.'
 Import-Module WebAdministration
 Require (!(Test-Path "IIS:\Sites\$($cfg.SiteName)") -and !(Test-Path "IIS:\AppPools\$($cfg.PoolName)")) 'IIS site or pool already exists; refusing replacement.'
 $cert=Get-Item "Cert:\LocalMachine\My\$($cfg.CertificateThumbprint)"
 Require ($cert.HasPrivateKey -and $cert.NotAfter -gt (Get-Date) -and $cert.NotBefore -le (Get-Date)) 'HTTPS certificate needs a valid private key and dates.'
 Require (@($cert.DnsNameList|Where-Object {$_.Unicode -ieq $cfg.HostName}).Count -gt 0) 'Certificate must include the approved exact DNS hostname.'
 Require (@(Get-NetTCPConnection -State Listen -LocalPort $cfg.HttpsPort -ErrorAction SilentlyContinue).Count -eq 0) 'HTTPS port is occupied. Setup will not stop another application.'
 Require (!(Get-NetFirewallRule -DisplayName 'BPLO HTTPS LAN' -ErrorAction SilentlyContinue)) 'BPLO firewall rule already exists; inspect before retrying.'
 if($cfg.PrepareBackupTask){
  Require ($cfg.BackupTaskName -match '^BPLO [A-Za-z0-9 _-]+$') 'Invalid backup task name.'
  Require (!(Get-ScheduledTask -TaskName $cfg.BackupTaskName -ErrorAction SilentlyContinue)) 'Backup task already exists.'
  Require (!(Test-Path 'C:\BPLO_Operations\Backup')) 'Backup operations folder already exists.'
  $service=Get-CimInstance Win32_Service|Where-Object {$_.Name -like 'MSSQL*' -and $_.StartName -ieq $cfg.SqlServiceAccount}
  Require (@($service).Count -eq 1) 'Confirm the exact local SQL service account in setup.json.'
 }
 # Schema validator connects with trusted encryption and verifies actual SQL identity.
 & (Join-Path $PSScriptRoot '..\database\Initialize-BPLODatabase.ps1') -Server $cfg.Server -Database $cfg.Database -ExpectedServerIdentity $cfg.ServerIdentity -Mode $cfg.DatabaseMode -AllowConfiguredDatabase
 New-WebAppPool -Name $cfg.PoolName|Out-Null
 Set-ItemProperty "IIS:\AppPools\$($cfg.PoolName)" managedRuntimeVersion ''
 Set-ItemProperty "IIS:\AppPools\$($cfg.PoolName)" processModel.loadUserProfile $true
 New-Item -ItemType Directory -Path $web|Out-Null
 Copy-Item -Path (Join-Path $package '*') -Destination $web -Recurse
 GrantFolder $web ('IIS AppPool\'+$cfg.PoolName) 'ReadAndExecute'
 $builder=[System.Data.SqlClient.SqlConnectionStringBuilder]::new()
 $builder['Data Source']=$cfg.Server;$builder['Initial Catalog']=$cfg.Database;$builder['Integrated Security']=$true;$builder['Encrypt']=$true;$builder['TrustServerCertificate']=$false
 @{Database=@{ApprovedServer=$cfg.Server;ApprovedServerIdentity=$cfg.ServerIdentity;ApprovedDatabase=$cfg.Database};ConnectionStrings=@{BPLS=$builder.ConnectionString};AllowedHosts=$cfg.HostName}|ConvertTo-Json -Depth 5|Set-Content -LiteralPath (Join-Path $web 'appsettings.Production.json') -Encoding UTF8
 # Dedicated local IIS identity: no server role and no schema modification privileges.
 $cn=[System.Data.SqlClient.SqlConnection]::new($builder.ConnectionString)
 try{
  $cn.Open();$cmd=$cn.CreateCommand();$login='IIS AppPool\'+$cfg.PoolName
  $cmd.CommandText="IF NOT EXISTS(SELECT 1 FROM sys.server_principals WHERE name=N'$login') CREATE LOGIN [$login] FROM WINDOWS; IF NOT EXISTS(SELECT 1 FROM sys.database_principals WHERE name=N'$login') CREATE USER [$login] FOR LOGIN [$login]; GRANT SELECT, INSERT, UPDATE, DELETE, EXECUTE ON SCHEMA::dbo TO [$login];"
  [void]$cmd.ExecuteNonQuery()
 }finally{$cn.Dispose()}
 $xml=[xml](Get-Content -LiteralPath (Join-Path $web 'web.config') -Raw)
 $asp=$xml.SelectSingleNode('//aspNetCore');Require ($null -ne $asp) 'Published web.config lacks ASP.NET Core settings.'
 $vars=$asp.SelectSingleNode('environmentVariables');if(!$vars){$vars=$xml.CreateElement('environmentVariables');[void]$asp.AppendChild($vars)}
 foreach($node in @($vars.SelectNodes("environmentVariable[@name='ASPNETCORE_ENVIRONMENT']"))){[void]$vars.RemoveChild($node)}
 $node=$xml.CreateElement('environmentVariable');$node.SetAttribute('name','ASPNETCORE_ENVIRONMENT');$node.SetAttribute('value','Production');[void]$vars.AppendChild($node);$xml.Save((Join-Path $web 'web.config'))
 New-Website -Name $cfg.SiteName -PhysicalPath $web -ApplicationPool $cfg.PoolName -Port $cfg.HttpsPort -HostHeader $cfg.HostName -Ssl|Out-Null
 Set-WebBinding -Name $cfg.SiteName -BindingInformation "*:$($cfg.HttpsPort):$($cfg.HostName)" -PropertyName sslFlags -Value 1
 (Get-WebBinding -Name $cfg.SiteName -Protocol https).AddSslCertificate($cfg.CertificateThumbprint,'My')
 New-NetFirewallRule -DisplayName 'BPLO HTTPS LAN' -Direction Inbound -Action Allow -Protocol TCP -LocalPort $cfg.HttpsPort -RemoteAddress $cfg.LanSubnet -Profile Domain,Private|Out-Null
 if($cfg.PrepareBackupTask){
  $ops='C:\BPLO_Operations\Backup';New-Item -ItemType Directory -Path $ops|Out-Null
  Copy-Item -LiteralPath (Join-Path $PSScriptRoot '..\backup\Backup-BPLODatabase.ps1') -Destination $ops
  $account=[Security.Principal.WindowsIdentity]::GetCurrent().Name
  New-Item -ItemType Directory -Path 'C:\BPLO_DB_Backup' -Force|Out-Null
  GrantFolder 'C:\BPLO_DB_Backup' $cfg.SqlServiceAccount 'Modify'
  GrantFolder 'C:\BPLO_DB_Backup' $account 'Modify'
  @{Server=$cfg.Server;Database=$cfg.Database;BackupFolder='C:\BPLO_DB_Backup';SqlServiceAccount=$cfg.SqlServiceAccount;TrustServerCertificate=$false;ExpectedServerIdentity=$cfg.ServerIdentity}|ConvertTo-Json|Set-Content "$ops\backup.config.json" -Encoding UTF8
  & "$env:SystemRoot\System32\WindowsPowerShell\v1.0\powershell.exe" -NoProfile -ExecutionPolicy Bypass -File "$ops\Backup-BPLODatabase.ps1" -ConfigPath "$ops\backup.config.json" -AllowConfiguredDatabase
  Require ($LASTEXITCODE -eq 0) 'Backup/VERIFYONLY failed; task not registered.'
  $action=New-ScheduledTaskAction -Execute "$env:SystemRoot\System32\WindowsPowerShell\v1.0\powershell.exe" -Argument "-NoProfile -NonInteractive -ExecutionPolicy Bypass -File `"$ops\Backup-BPLODatabase.ps1`" -ConfigPath `"$ops\backup.config.json`" -AllowConfiguredDatabase"
  $trigger=New-ScheduledTaskTrigger -Weekly -DaysOfWeek Friday -At '16:30'
  $settings=New-ScheduledTaskSettingsSet -StartWhenAvailable -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries -MultipleInstances IgnoreNew
  $identity=New-ScheduledTaskPrincipal -UserId $account -LogonType S4U -RunLevel Highest
  # Register disabled atomically; enable only after unattended/independence acceptance.
  $settings.Enabled=$false
  Register-ScheduledTask -TaskName $cfg.BackupTaskName -Action $action -Trigger $trigger -Settings $settings -Principal $identity|Out-Null
  Write-Host 'Backup verified. Friday 16:30 task registered DISABLED; unattended acceptance and activation remain.'
 }
 Write-Host "Installation prepared: https://$($cfg.HostName):$($cfg.HttpsPort)/Account/Login"
 Write-Host 'Complete official data transfer, three employee accounts, trusted LAN access, restart/cookie and backup independence tests before acceptance.'
}catch{Write-Error "Setup stopped: $($_.Exception.Message)" -ErrorAction Continue;Write-Host 'Partial resources may remain. Nothing is automatically deleted or rolled back. Inspect before retrying.';exit 1}finally{if($transcript){Stop-Transcript|Out-Null}}
