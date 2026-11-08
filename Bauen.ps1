$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) { throw '.NET SDK 8 fehlt.' }
$packageName = 'Musikbox-Werkstatt-Studio-v0.64-CSharp-Portable'
$publish = Join-Path $PSScriptRoot ('Ausgabe\' + $packageName)
if (Test-Path -LiteralPath $publish) { throw 'Dieser Ausgabeordner existiert bereits. Bitte vorher umbenennen, damit nichts ueberschrieben wird.' }
& dotnet publish (Join-Path $PSScriptRoot 'MusikboxWerkstatt.csproj') -c Release -r win-x64 --self-contained true -o $publish
if ($LASTEXITCODE -ne 0) { throw 'Windows-Publish fehlgeschlagen.' }
foreach ($required in @('Musikbox-Werkstatt-Studio.exe','Programm\coreclr.dll','Programm\System.Windows.Forms.dll','Werkzeuge\fpcalc\fpcalc.exe','Werkzeuge\ffmpeg\ffmpeg.exe','Werkzeuge\ffmpeg\ffprobe.exe','Werkzeuge\mp3gain\mp3gain.exe','Werkzeuge\whisper\whisper-cli.exe','Werkzeuge\whisper\whisper-cli-compatible.exe','Werkzeuge\whisper\ggml-base.bin','Werkzeuge\freac\freaccmd.exe','Werkzeuge\yt-dlp\yt-dlp.exe','Werkzeuge\yt-dlp\deno.exe')) {
 if (-not (Test-Path -LiteralPath (Join-Path $publish $required))) { throw "Paketdatei fehlt: $required" }
}
$source = Join-Path $publish 'Quellcode'
New-Item -ItemType Directory -Path $source -Force | Out-Null
foreach ($dir in @('src','Tests')) { Copy-Item -LiteralPath (Join-Path $PSScriptRoot $dir) -Destination $source -Recurse }
Get-ChildItem -LiteralPath $PSScriptRoot -File | Where-Object { $_.Extension -in @('.csproj','.ps1','.cmd','.md','.txt','.json','.manifest','.config') } | Copy-Item -Destination $source
foreach ($name in @('README.md','PRUEFBERICHT-v0.64.md','PROJEKTAKTE-v0.64.md')) { Copy-Item -LiteralPath (Join-Path $PSScriptRoot $name) -Destination (Join-Path $publish 'Dokumentation') }
$zip = Join-Path (Split-Path $publish) ($packageName + '.zip')
if (Test-Path -LiteralPath $zip) { throw 'ZIP existiert bereits. Das neue Paket liegt im Ausgabeordner.' }
Compress-Archive -LiteralPath $publish -DestinationPath $zip
Write-Host ('Paket erstellt: ' + $zip)
