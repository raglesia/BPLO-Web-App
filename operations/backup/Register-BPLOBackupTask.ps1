param([string]$ConfigPath=(Join-Path $PSScriptRoot 'backup.config.json'))
$ErrorActionPreference='Stop'
$cfg=Get-Content -LiteralPath $ConfigPath -Raw|ConvertFrom-Json
if($cfg.Database -ne 'BPLS_Dev'){throw 'This development registration script permits BPLS_Dev only.'}
if($cfg.TaskName -notmatch 'Development'){throw 'Development task name required.'}
if(Get-ScheduledTask -TaskName $cfg.TaskName -ErrorAction SilentlyContinue){throw 'Task already exists; inspect it before replacing.'}
$exe=Join-Path $env:SystemRoot 'System32\WindowsPowerShell\v1.0\powershell.exe'
$script=Join-Path $PSScriptRoot 'Backup-BPLODatabase.ps1'
$action=New-ScheduledTaskAction -Execute $exe -Argument "-NoProfile -NonInteractive -ExecutionPolicy Bypass -File `"$script`" -ConfigPath `"$ConfigPath`""
$trigger=New-ScheduledTaskTrigger -Weekly -WeeksInterval 1 -DaysOfWeek $cfg.BackupDay -At ([datetime]::ParseExact($cfg.BackupTime,'HH:mm',[Globalization.CultureInfo]::InvariantCulture))
$settings=New-ScheduledTaskSettingsSet -StartWhenAvailable -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries -MultipleInstances IgnoreNew -ExecutionTimeLimit (New-TimeSpan -Hours 2)
$principal=New-ScheduledTaskPrincipal -UserId ([Security.Principal.WindowsIdentity]::GetCurrent().Name) -LogonType S4U -RunLevel Highest
Register-ScheduledTask -TaskName $cfg.TaskName -Action $action -Trigger $trigger -Settings $settings -Principal $principal
