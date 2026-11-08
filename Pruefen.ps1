param([switch]$QS,[switch]$Alle,[switch]$Oberflaeche,[switch]$Sprache,[switch]$Dateitags,[switch]$Tagformate,[string]$Testmusikordner)
$ErrorActionPreference='Stop'
Set-Location $PSScriptRoot
if($QS -or $Alle){$projects=@('v061/Regression61.csproj','v064/Voice64.csproj','v064/QS-Windows.csproj','v064/Workspace64.csproj')}
elseif($Oberflaeche){$projects=@('v064/QS-Windows.csproj','v064/Workspace64.csproj')}
elseif($Sprache){$projects=@('v064/Voice64.csproj')}
elseif($Dateitags -or $Tagformate){
 if(-not $Testmusikordner){throw 'Eigenen Testordner mit glen.mp3, wilson.mp3, cover.png und second.jpg angeben. Originalmusik nicht verwenden.'}
 $projects=@('v063/Integrity63.csproj')
}else{$projects=@('v062/SearchCounter62.csproj')}
foreach($project in $projects){
 $arguments=@('run','--project',$project,'-c','Release','-p:NuGetAudit=false')
 if($Testmusikordner){$arguments+=@('--',(Resolve-Path -LiteralPath $Testmusikordner).Path)}
 & dotnet @arguments
 if($LASTEXITCODE){throw "Pruefung fehlgeschlagen: $project"}
}
