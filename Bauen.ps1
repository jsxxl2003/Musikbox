param([string]$WerkzeugePfad,[string]$Ausgabeordner,[string]$PublishOrdner,[string]$Pruefprotokolle)
$ErrorActionPreference='Stop'
Set-Location $PSScriptRoot
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) { throw '.NET SDK 8 fehlt.' }
if (-not $Ausgabeordner) { $Ausgabeordner=Join-Path $PSScriptRoot 'Ausgabe' }
if (-not $WerkzeugePfad) {
 $WerkzeugePfad=Join-Path (Split-Path $PSScriptRoot) 'Werkzeuge'
 if (-not (Test-Path $WerkzeugePfad)) { $WerkzeugePfad=Join-Path $PSScriptRoot 'tools' }
}
foreach($name in @('ffmpeg/ffmpeg.exe','ffmpeg/ffprobe.exe','mp3gain/mp3gain.exe','fpcalc/fpcalc.exe','freac/freaccmd.exe','whisper/whisper-cli.exe','whisper/whisper-cli-compatible.exe','whisper/ggml-base.bin','yt-dlp/yt-dlp.exe','yt-dlp/deno.exe')) {
 if(-not(Test-Path (Join-Path $WerkzeugePfad $name))){throw "Werkzeug fehlt: $name. Vollstaendigen Werkzeuge-Ordner des bisherigen Pakets ueber -WerkzeugePfad angeben."}
}
$packageName='MW'
$package=Join-Path $Ausgabeordner $packageName
if(Test-Path $package){throw 'Ausgabeordner existiert bereits. Bitte einen neuen Ausgabeordner verwenden.'}
if($PublishOrdner) {
 New-Item -ItemType Directory -Force $package | Out-Null
 Copy-Item (Join-Path $PublishOrdner '*') $package -Recurse -Force
} else {
 dotnet publish MusikboxWerkstatt.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -p:NuGetAudit=false -p:ToolsRoot=__Werkzeuge_separat__ -o $package
 if($LASTEXITCODE){throw 'Windows-Publish fehlgeschlagen.'}
}
Copy-Item $WerkzeugePfad (Join-Path $package 'Werkzeuge') -Recurse
foreach($dir in @('Daten','Dokumentation','Quellcode')){New-Item -ItemType Directory (Join-Path $package $dir) -Force | Out-Null}
$source=Join-Path $package 'Quellcode'
# Nur versionierte Dateien: keine bin/obj-Verzeichnisse oder privaten Daten.
if(Test-Path (Join-Path $PSScriptRoot '.git')) {
 $files=@(git -C $PSScriptRoot ls-files)
 if($LASTEXITCODE){throw 'Quellcodedateien konnten nicht ermittelt werden.'}
 foreach($file in $files) {
  $target=Join-Path $source $file
  New-Item -ItemType Directory -Force (Split-Path $target) | Out-Null
  Copy-Item (Join-Path $PSScriptRoot $file) $target
 }
} else {
 foreach($dir in @('src','v061','v062','v063','v064','tools','.github')) {
  Get-ChildItem (Join-Path $PSScriptRoot $dir) -Recurse -File -Force | Where-Object {$_.FullName -notmatch '[\\/](bin|obj|\.git)[\\/]'} | ForEach-Object {
   $target=Join-Path $source ([IO.Path]::GetRelativePath($PSScriptRoot,$_.FullName))
   New-Item -ItemType Directory -Force (Split-Path $target) | Out-Null
   Copy-Item $_.FullName $target
  }
 }
 Get-ChildItem $PSScriptRoot -File -Force | Where-Object {$_.Extension -in @('.cs','.csproj','.ps1','.cmd','.md','.txt','.manifest','.config') -or $_.Name -in @('.gitattributes','.gitignore')} | Copy-Item -Destination $source
}
foreach($name in @('README.md','PROJEKTAKTE-v0.64.md','PRUEFBERICHT-v0.64.md','DRITTKOMPONENTEN-v0.63.md')){Copy-Item $name (Join-Path $package 'Dokumentation')}
Copy-Item 'README.md' $package
if($Pruefprotokolle) {
 New-Item -ItemType Directory -Force "$package/Dokumentation/Logs" | Out-Null
 Get-ChildItem $Pruefprotokolle -File -Filter '*.txt' | Copy-Item -Destination "$package/Dokumentation/Logs"
}
# 120 Zeichen fuer ZIP-Innenpfade lassen auch unter langen Downloadordnern Platz.
$paths=@(Get-ChildItem $package -Recurse -File -Force | ForEach-Object {[IO.Path]::GetRelativePath($Ausgabeordner,$_.FullName)})
$longest=$paths | Sort-Object Length -Descending | Select-Object -First 1
if($longest.Length -gt 120){throw "Archivpfad zu lang ($($longest.Length)): $longest"}
if(-not(Test-Path "$package/Programm/Musikbox-Werkstatt-Studio.dll")){throw 'Portable Programmstruktur fehlt.'}
@("Paket: MW-064", "Commit: $env:GITHUB_SHA", "Dateien: $($paths.Count)", "Laengster Archivpfad: $($longest.Length) Zeichen", "Pfad: $longest", 'Windows-MAX_PATH: bei 130 Zeichen Zielordner maximal 251 Zeichen.') | Set-Content "$package/Dokumentation/Paket.txt" -Encoding utf8
# ZipFile nimmt auch .github und andere versteckte Quellcodedateien mit.
Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip=Join-Path $Ausgabeordner 'MW-064.zip'
[IO.Compression.ZipFile]::CreateFromDirectory($package,$zip,[IO.Compression.CompressionLevel]::Optimal,$true)
$archive=[IO.Compression.ZipFile]::OpenRead($zip)
try {
 if(-not($archive.Entries | Where-Object {$_.FullName -eq 'MW/Quellcode/.github/workflows/main.yml'})){throw 'Workflow fehlt im Quellcodearchiv.'}
 if($archive.Entries | Where-Object {$_.FullName.Length -gt 120}){throw 'ZIP enthaelt einen zu langen Pfad.'}
 $check=Join-Path $Ausgabeordner ('Pruefpfad-' + ('x'*100))
 [IO.Compression.ZipFile]::ExtractToDirectory($zip,$check)
 if(-not(Test-Path "$check/MW/Musikbox-Werkstatt-Studio.exe")){throw 'Entpackpruefung fehlgeschlagen.'}
 Remove-Item $check -Recurse -Force
} finally {$archive.Dispose()}
Write-Host ('Vollstaendiges Paket erstellt und entpackt: '+$zip)
