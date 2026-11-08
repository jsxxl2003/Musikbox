param([switch]$Oberflaeche,[switch]$Sprache,[switch]$Dateitags,[switch]$Tagformate,[string]$Testmusikordner)
$ErrorActionPreference='Stop'
Set-Location $PSScriptRoot
if ($Oberflaeche) { $project='v064\Workspace64.csproj' }
elseif ($Sprache) { $project='v064\Voice64.csproj' }
elseif ($Dateitags -or $Tagformate) {
 if (-not $Testmusikordner) { throw 'Nur einen eigenen Testordner mit glen.mp3, wilson.mp3, cover.png und second.jpg angeben. Originalmusik nicht verwenden.' }
 $project='v063\Integrity63.csproj'
} else { $project='v062\SearchCounter62.csproj' }
$arguments=@('run','--project',$project,'-c','Release')
if ($Testmusikordner) { $arguments+=@('--',(Resolve-Path -LiteralPath $Testmusikordner).Path) }
& dotnet @arguments
if ($LASTEXITCODE -ne 0) { throw 'Pruefung fehlgeschlagen.' }
