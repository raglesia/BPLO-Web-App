param([string]$ConfigPath=(Join-Path $PSScriptRoot 'backup.config.json'),[switch]$AllowConfiguredDatabase)
$ErrorActionPreference='Stop'
$connection=$null;$log=$null
function Log($message){$line="$(Get-Date -Format o) $message";Write-Output $line;if($log){Add-Content -LiteralPath $log -Value $line}}
try {
 $cfg=Get-Content -LiteralPath $ConfigPath -Raw|ConvertFrom-Json
 if($cfg.Database -ne 'BPLS_Dev' -and !$AllowConfiguredDatabase){throw 'Development guard: only BPLS_Dev is permitted.'}
 if($cfg.Server -notmatch '^(\.|localhost|'+[regex]::Escape($env:COMPUTERNAME)+')(\\[^\\]+)?$'){throw 'Backup must execute on the local SQL Server host.'}
 if(!$cfg.BackupFolder -or ![IO.Path]::IsPathRooted($cfg.BackupFolder)){throw 'Backup folder must be absolute.'}
 if(!(Test-Path -LiteralPath $cfg.BackupFolder)){
  [void](New-Item -ItemType Directory -Path $cfg.BackupFolder)
  # Grant only the SQL service identity Modify on this dedicated backup folder.
  $acl=Get-Acl -LiteralPath $cfg.BackupFolder
  $rule=[Security.AccessControl.FileSystemAccessRule]::new($cfg.SqlServiceAccount,'Modify','ContainerInherit,ObjectInherit','None','Allow')
  $acl.AddAccessRule($rule);Set-Acl -LiteralPath $cfg.BackupFolder -AclObject $acl
 }
 $logs=Join-Path $cfg.BackupFolder 'logs';[void](New-Item -ItemType Directory -Path $logs -Force)
 $log=Join-Path $logs 'backup.log'
 $probe=Join-Path $cfg.BackupFolder ([guid]::NewGuid().ToString()+'.write-test');[IO.File]::WriteAllText($probe,'write check');Remove-Item -LiteralPath $probe
 $file=Join-Path $cfg.BackupFolder ($cfg.Database+'_'+(Get-Date -Format 'yyyy-MM-dd_HHmmss_fff')+'_'+[guid]::NewGuid().ToString('N').Substring(0,8)+'.bak')
 if(Test-Path -LiteralPath $file){throw 'Backup filename already exists.'}
 Log "START database=$($cfg.Database) file=$file"
 $builder=[System.Data.SqlClient.SqlConnectionStringBuilder]::new();$builder['Data Source']=$cfg.Server;$builder['Initial Catalog']=$cfg.Database;$builder['Integrated Security']=$true;$builder['Encrypt']=$true;$builder['TrustServerCertificate']=$true;$builder['Connect Timeout']=15
 $connection=[System.Data.SqlClient.SqlConnection]::new($builder.ConnectionString);$connection.Open()
 $cmd=$connection.CreateCommand();$cmd.CommandText='SELECT SUM(CONVERT(bigint,size))*8192 FROM sys.database_files';$estimate=[long]$cmd.ExecuteScalar()
 $drive=[IO.DriveInfo]::new([IO.Path]::GetPathRoot($file));if($drive.AvailableFreeSpace -lt ($estimate*1.2+100MB)){throw "Insufficient free disk space; database estimate=$estimate bytes."}
 $db='['+$cfg.Database.Replace(']',']]')+']'
 $cmd=$connection.CreateCommand();$cmd.CommandTimeout=3600;$cmd.CommandText="BACKUP DATABASE $db TO DISK=@file WITH COPY_ONLY, CHECKSUM, NOINIT, STATS=10";[void]$cmd.Parameters.AddWithValue('@file',$file);[void]$cmd.ExecuteNonQuery()
 if(!(Test-Path -LiteralPath $file) -or (Get-Item -LiteralPath $file).Length -le 0){throw 'Backup file missing or empty.'}
 $cmd=$connection.CreateCommand();$cmd.CommandTimeout=3600;$cmd.CommandText='RESTORE VERIFYONLY FROM DISK=@file WITH CHECKSUM';[void]$cmd.Parameters.AddWithValue('@file',$file);[void]$cmd.ExecuteNonQuery()
 Log "VERIFYONLY PASSED database=$($cfg.Database) file=$file"
 Log "SUCCESS bytes=$((Get-Item -LiteralPath $file).Length)";exit 0
}catch{try{Log "FAILURE $($_.Exception.Message)"}catch{Write-Error $_};Write-Error 'Backup failed. See preceding error/log.';exit 1}finally{if($connection){$connection.Dispose()}}
