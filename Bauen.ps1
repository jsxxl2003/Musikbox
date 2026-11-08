param([string]$WerkzeugePfad,[string]$Ausgabeordner)
$ErrorActionPreference='Stop'
Set-Location $PSScriptRoot
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) { throw '.NET SDK 8 fehlt.' }
if (-not $Ausgabeordner) { $Ausgabeordner=Join-Path $PSScriptRoot 'Ausgabe' }
if (-not $WerkzeugePfad) {
 $WerkzeugePfad=Join-Path (Split-Path $PSScriptRoot) 'Werkzeuge'
 if (-not (Test-Path $WerkzeugePfad)) { $WerkzeugePfad=Join-Path $PSScriptRoot 'tools' }
}
foreach($name in @('ffmpeg/ffmpeg.exe','ffmpeg/ffprobe.exe','mp3gain/mp3gain.exe','fpcalc/fpcalc.exe','freac/freaccmd.exe','whisper/whisper-cli.exe','whisper/ggml-base.bin','yt-dlp/yt-dlp.exe','yt-dlp/deno.exe')) {
 if(-not(Test-Path (Join-Path $WerkzeugePfad $name))){throw "Werkzeug fehlt: $name. Vollstaendigen Werkzeuge-Ordner des bisherigen Pakets ueber -WerkzeugePfad angeben."}
}
$packageName='Musikbox-Werkstatt-Studio-v0.64-QS-2026-11-08'
$package=Join-Path $Ausgabeordner $packageName
if(Test-Path $package){throw 'Ausgabeordner existiert bereits. Bitte einen neuen Ausgabeordner verwenden.'}
$program=Join-Path $package 'Programm'
dotnet publish MusikboxWerkstatt.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -p:NuGetAudit=false -p:ToolsRoot=__Werkzeuge_separat__ -o $program
if($LASTEXITCODE){throw 'Windows-Publish fehlgeschlagen.'}
# The apphost reserves 1024 bytes for its relative managed entry point.
$inner=Join-Path $program 'Musikbox-Werkstatt-Studio.exe'
$bytes=[IO.File]::ReadAllBytes($inner)
$binding='Musikbox-Werkstatt-Studio.dll'
$offset=[Text.Encoding]::ASCII.GetString($bytes).IndexOf($binding+[char]0,[StringComparison]::Ordinal)
if($offset -lt 0 -or $offset+1024 -gt $bytes.Length){throw 'Apphost-Bindung nicht gefunden.'}
[Array]::Clear($bytes,$offset,1024)
$target=[Text.Encoding]::UTF8.GetBytes('Programm/Musikbox-Werkstatt-Studio.dll')
[Array]::Copy($target,0,$bytes,$offset,$target.Length)
[IO.File]::WriteAllBytes((Join-Path $package 'Musikbox-Werkstatt-Studio.exe'),$bytes)
Remove-Item $inner
Copy-Item $WerkzeugePfad (Join-Path $package 'Werkzeuge') -Recurse
foreach($dir in @('Daten','Dokumentation','Quellcode')){New-Item -ItemType Directory (Join-Path $package $dir) -Force | Out-Null}
$source=Join-Path $package 'Quellcode'
foreach($dir in @('src','v061','v062','v063','v064','tools','.github')){Copy-Item (Join-Path $PSScriptRoot $dir) $source -Recurse}
Get-ChildItem $PSScriptRoot -File | Where-Object {$_.Extension -in @('.cs','.csproj','.ps1','.cmd','.md','.txt','.manifest','.config')} | Copy-Item -Destination $source
foreach($name in @('README.md','PROJEKTAKTE-v0.64.md','PRUEFBERICHT-v0.64.md','DRITTKOMPONENTEN-v0.63.md')){Copy-Item $name (Join-Path $package 'Dokumentation')}
Copy-Item 'README.md' $package
Compress-Archive -LiteralPath $package -DestinationPath ($package+'.zip')
Write-Host ('Paket erstellt: '+$package+'.zip')
