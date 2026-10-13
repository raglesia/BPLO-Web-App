# Non-mutating installer guard checks; no SQL, IIS or task registration.
$ErrorActionPreference='Stop'
$script=Join-Path $PSScriptRoot 'Setup-BPLO.ps1'
$temp=Join-Path $env:TEMP ('BPLO-SetupTests-'+[guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $temp|Out-Null
$exe=Join-Path $env:SystemRoot 'System32\WindowsPowerShell\v1.0\powershell.exe'
function Check($name,$config,$success){
 $path=Join-Path $temp 'setup.json';$config|ConvertTo-Json|Set-Content -LiteralPath $path
 $prior=$ErrorActionPreference;$ErrorActionPreference='Continue'
 try{$result=& $exe -NoProfile -ExecutionPolicy Bypass -File $script -ConfigPath $path 2>&1;$code=$LASTEXITCODE}finally{$ErrorActionPreference=$prior}
 if(($code -eq 0) -ne $success){throw "FAIL $name : $result"}
 Write-Output "PASS $name"
}
try{
 $cfg=Get-Content (Join-Path $PSScriptRoot 'setup.example.json') -Raw|ConvertFrom-Json
 Check 'unconfigured template rejected' $cfg $false
 $cfg.ServerIdentity='TESTHOST\SQLEXPRESS';$cfg.Database='BPLO_ApprovedTest';$cfg.HostName='bplo.test.local';$cfg.CertificateThumbprint='A'*40
 Check 'completed configuration plan accepted without mutation' $cfg $true
 foreach($database in @('BPLS_Dev','master','BPLS_Staging_QA01','bad;DROP')){$cfg.Database=$database;Check "protected/invalid database $database" $cfg $false}
 $cfg.Database='BPLO_ApprovedTest';$cfg.Server='REMOTE\SQLEXPRESS';Check 'remote SQL rejected' $cfg $false
 $cfg.Server='.\SQLEXPRESS';$cfg.WebFolder='C:\Windows\BPLO';Check 'system destination rejected' $cfg $false
 $cfg.WebFolder='C:\BPLO\Web';$cfg.LanSubnet='0.0.0.0/0';Check 'unrestricted firewall rejected' $cfg $false
 $cfg.LanSubnet='999.168.1.0/24';Check 'invalid IPv4 rejected' $cfg $false
 $cfg.LanSubnet='192.168.1.0/24';$cfg.DatabaseMode='Restore';Check 'unsupported overwrite/restore mode rejected' $cfg $false
}finally{
 # Only this test's exact newly created files are removed.
 Remove-Item -LiteralPath (Join-Path $temp 'setup.json') -ErrorAction SilentlyContinue
 Remove-Item -LiteralPath $temp
}
