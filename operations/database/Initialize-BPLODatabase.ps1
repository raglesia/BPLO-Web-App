param(
 [Parameter(Mandatory=$true)][string]$Server,
 [Parameter(Mandatory=$true)][string]$Database,
 [Parameter(Mandatory=$true)][string]$ExpectedServerIdentity,
 [ValidateSet('Validate','Create')][string]$Mode='Validate',
 [switch]$AllowConfiguredDatabase,
 [switch]$TrustServerCertificate
)
$ErrorActionPreference='Stop'
$dbSpec=Get-Content (Join-Path $PSScriptRoot 'schema-v1.database.json') -Raw|ConvertFrom-Json
if($dbSpec.Collation -notmatch '^[A-Za-z0-9_]+$'){throw 'Invalid schema collation'}
if(-not $AllowConfiguredDatabase){throw 'Explicit -AllowConfiguredDatabase approval required.'}
if($Database -notmatch '^[A-Za-z][A-Za-z0-9_]{0,127}$' -or $Database -in @('master','model','msdb','tempdb','BPLS_Dev')){throw 'Invalid or protected target database.'}
$b=New-Object System.Data.SqlClient.SqlConnectionStringBuilder
$b['Data Source']=$Server;$b['Initial Catalog']='master';$b['Integrated Security']=$true;$b['Encrypt']=$true;$b['TrustServerCertificate']=[bool]$TrustServerCertificate
$cn=New-Object System.Data.SqlClient.SqlConnection($b.ConnectionString);$cn.Open()
function Execute($sql){$c=$cn.CreateCommand();$c.CommandText=$sql;$c.CommandTimeout=120;if($script:tx){$c.Transaction=$script:tx};return $c.ExecuteNonQuery()}
function Scalar($sql){$c=$cn.CreateCommand();$c.CommandText=$sql;if($script:tx){$c.Transaction=$script:tx};return $c.ExecuteScalar()}
try {
 if([string](Scalar "SELECT CONVERT(nvarchar(256),SERVERPROPERTY('ServerName'))") -ine $ExpectedServerIdentity){throw 'Server identity mismatch; no database change.'}
 $exists=[int](Scalar "SELECT COUNT(*) FROM sys.databases WHERE name=N'$Database'")
 if($Mode -eq 'Create'){
  if($exists){throw 'Database already exists. Refusing overwrite. Use Validate or a separately reviewed migration.'}
  [void](Execute "CREATE DATABASE [$Database] COLLATE $($dbSpec.Collation)")
  $cn.ChangeDatabase($Database)
  $script:tx=$cn.BeginTransaction()
  $sql=Get-Content (Join-Path $PSScriptRoot 'schema-v1.sql') -Raw
   [void](Execute "EXEC sys.sp_set_session_context @key=N'BPLO_ApprovedSchemaTarget', @value=N'$Database'")
   $sql=[regex]::Replace($sql,'(?im)^:ON ERROR EXIT\s*$','')
   foreach($batch in [regex]::Split($sql,'(?im)^\s*GO\s*$')){if($batch.Trim()){[void](Execute $batch)}}
 }else{if(-not $exists){throw 'Target database does not exist.'};$cn.ChangeDatabase($Database)}
 if([string](Scalar 'SELECT DB_NAME()') -ine $Database){throw 'Connected database mismatch'}
 if([string](Scalar "SELECT CONVERT(nvarchar(128),DATABASEPROPERTYEX(DB_NAME(),'Collation'))") -ine $dbSpec.Collation){throw 'Database collation differs from the verified schema; review required.'}
 $expected=Get-Content (Join-Path $PSScriptRoot 'schema-v1.columns.json') -Raw|ConvertFrom-Json
 foreach($r in $expected){
  $collation=if($null -eq $r.Collation){'c.collation_name IS NULL'}else{"c.collation_name=N'$($r.Collation)'"}
  $default=if($null -eq $r.Default){'c.default_object_id=0'}else{"EXISTS(SELECT 1 FROM sys.default_constraints dc WHERE dc.object_id=c.default_object_id AND dc.definition=N'$($r.Default.Replace("'","''"))')"}
  $sql="SELECT COUNT(*) FROM sys.tables t JOIN sys.columns c ON c.object_id=t.object_id JOIN sys.types ty ON ty.user_type_id=c.user_type_id WHERE SCHEMA_NAME(t.schema_id)='dbo' AND t.name=N'$($r.Table)' AND c.name=N'$($r.Column)' AND ty.name=N'$($r.Type)' AND c.max_length=$($r.Length) AND c.precision=$($r.Precision) AND c.scale=$($r.Scale) AND c.is_nullable=$([int]$r.Nullable) AND c.is_identity=$([int]$r.Identity) AND $collation AND $default"
  if([int](Scalar $sql) -ne 1){throw "Schema mismatch: $($r.Table).$($r.Column). No automatic repair."}
 }
 $structure=Get-Content (Join-Path $PSScriptRoot 'schema-v1.structure.json') -Raw|ConvertFrom-Json
 $c=$cn.CreateCommand();$c.CommandText=Get-Content (Join-Path $PSScriptRoot 'schema-structure-query.sql') -Raw;if($script:tx){$c.Transaction=$script:tx}
 $actual=New-Object System.Data.DataTable;[void](New-Object System.Data.SqlClient.SqlDataAdapter($c)).Fill($actual)
 foreach($item in $structure){
  $found=@($actual.Rows|Where-Object {$_.Kind -eq $item.Kind -and $_.TableName -eq $item.Table -and $_.ObjectName -eq $item.Name})
  if($found.Count -ne 1 -or ([regex]::Replace([string]$found[0].Definition,'\s+',' ').Trim() -cne [regex]::Replace([string]$item.Definition,'\s+',' ').Trim())){throw "Structural mismatch: $($item.Kind) $($item.Table).$($item.Name). Review required; no automatic repair."}
 }
 # Required payment constraints and triggers must be trusted/enabled, not merely present.
 if([int](Scalar "SELECT COUNT(*) FROM sys.foreign_keys WHERE is_disabled=1 OR is_not_trusted=1") -gt 0){throw 'Disabled or untrusted foreign key'}
 if([int](Scalar "SELECT COUNT(*) FROM sys.check_constraints WHERE is_disabled=1 OR is_not_trusted=1") -gt 0){throw 'Disabled or untrusted check constraint'}
 foreach($name in @('TR_PaymentHistory_ReceiptRegistry','TR_VehiclePermitHistory_ReceiptRegistry','TR_VehiclePaymentAssessmentSnapshots_Immutable')){if([int](Scalar "SELECT COUNT(*) FROM sys.triggers WHERE name=N'$name' AND is_disabled=0") -ne 1){throw "Missing enabled receipt trigger $name"}}
 if([int](Scalar "SELECT COUNT(*) FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.MonthlyBilling') AND is_unique=1 AND is_disabled=0 AND name='UQ_MonthlyBilling'") -ne 1){throw 'Missing monthly period uniqueness'}
 if([int](Scalar "SELECT COUNT(*) FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.VehiclePermitHistory') AND is_unique=1 AND is_disabled=0 AND name='UX_VehiclePermitHistory_VIN_PermitYear'") -ne 1){throw 'Missing vehicle/year uniqueness'}
 if([int](Scalar "SELECT COUNT(*) FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.ReceiptRegistry') AND is_unique=1 AND is_disabled=0 AND name='UQ_ReceiptRegistry_ORNumber'") -ne 1){throw 'Missing global receipt uniqueness'}
 $violations=[int](Scalar "SELECT COUNT(*) FROM (SELECT dbo.NormalizeOR(ORNumber) COLLATE Latin1_General_100_CI_AS n FROM (SELECT ORNumber FROM PaymentHistory UNION ALL SELECT ORNumber FROM VehiclePermitHistory)p GROUP BY dbo.NormalizeOR(ORNumber) COLLATE Latin1_General_100_CI_AS HAVING COUNT(*)>1)d")
 if($violations){throw 'Conflicting OR history; manual review required'}
 if([int](Scalar "SELECT COUNT(*) FROM (SELECT ORNumber FROM PaymentHistory UNION ALL SELECT ORNumber FROM VehiclePermitHistory)p WHERE NULLIF(dbo.NormalizeOR(ORNumber),N'') IS NULL") -gt 0){throw 'Blank historical OR; manual review required'}
 $payments="SELECT 'StallRental' PaymentType,Id,ORNumber,RecordedBy,DatePaid FROM PaymentHistory UNION ALL SELECT 'VehiclePermit',Id,ORNumber,RecordedBy,DatePaid FROM VehiclePermitHistory"
 if([int](Scalar "WITH p AS ($payments) SELECT COUNT(*) FROM p LEFT JOIN ReceiptRegistry r ON r.PaymentType=p.PaymentType AND r.PaymentReferenceId=p.Id WHERE r.Id IS NULL OR r.ORNumber<>dbo.NormalizeOR(p.ORNumber) COLLATE Latin1_General_100_CI_AS OR r.RecordedByUserId<>p.RecordedBy OR r.RecordedAt<>p.DatePaid") -gt 0){throw 'Missing or mismatched receipt linkage; manual review required'}
 if([int](Scalar "WITH p AS ($payments) SELECT COUNT(*) FROM ReceiptRegistry r LEFT JOIN p ON r.PaymentType=p.PaymentType AND r.PaymentReferenceId=p.Id WHERE p.Id IS NULL") -gt 0){throw 'Orphan or pending receipt reservation; manual review required'}
 if($script:tx){$script:tx.Commit();$script:tx.Dispose();$script:tx=$null}
 "PASS: $Mode $Database; $($expected.Count) columns validated; required receipt/billing safeguards enabled. No seed data or payments inserted."
}catch{if($script:tx){$script:tx.Rollback()};throw}finally{if($script:tx){$script:tx.Dispose();$script:tx=$null};$cn.Dispose()}

