param([Parameter(Mandatory=$true)][string]$Ziel)
$ErrorActionPreference='Stop'
$Ziel=[IO.Path]::GetFullPath($Ziel)
$temp=Join-Path ([IO.Path]::GetTempPath()) ('mw-tools-'+[Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Force $Ziel,$temp | Out-Null
function Download($url,$path,$sha='') {
 New-Item -ItemType Directory -Force (Split-Path $path) | Out-Null
 Invoke-WebRequest -Uri $url -OutFile $path -MaximumRetryCount 3
 if($sha -and (Get-FileHash $path -Algorithm SHA256).Hash -ne $sha){throw "Falsche SHA256-Pruefsumme: $url"}
}
function Unzip($url,$name,$sha='') {
 $zip=Join-Path $temp ($name+'.zip'); Download $url $zip $sha
 $folder=Join-Path $temp $name; Expand-Archive -LiteralPath $zip -DestinationPath $folder
 return $folder
}
try {
 Copy-Item "$PSScriptRoot/tools/*" $Ziel -Recurse -Force
 $ff=Unzip 'https://github.com/GyanD/codexffmpeg/releases/download/9.0.2/ffmpeg-9.0.2-essentials_build.zip' 'ff' '60f467265b1e312373dbcd92200c2618a74850f98d3d078e94296bb3fa2047ba'
 foreach($name in @('ffmpeg.exe','ffprobe.exe')) {
  $file=@(Get-ChildItem $ff -Recurse -File -Filter $name); if($file.Count -ne 1){throw "FFmpeg-Datei fehlt: $name"}
  Copy-Item $file[0].FullName "$Ziel/ffmpeg/$name"
 }
 Download 'https://ffmpeg.org/releases/ffmpeg-9.0.2.tar.xz' "$Ziel/ffmpeg/sources/ffmpeg-9.0.2.tar.xz"
 $freac=Unzip 'https://github.com/enzo1982/freac/releases/download/v1.1.7/freac-1.1.7-windows-x64.zip' 'freac'
 $cmd=@(Get-ChildItem $freac -Recurse -File -Filter freaccmd.exe); if($cmd.Count -ne 1){throw 'freaccmd.exe fehlt.'}
 New-Item -ItemType Directory -Force "$Ziel/freac" | Out-Null
 Copy-Item (Join-Path $cmd[0].DirectoryName '*') "$Ziel/freac" -Recurse -Force
 if((Get-FileHash "$Ziel/freac/freaccmd.exe").Hash -ne '42cb5d046d743385fed6040ae4f0de05a0a655437a342541202283f356990a3c'){throw 'Unerwartetes fre:ac-Programm.'}
 Download 'https://github.com/enzo1982/freac/releases/download/v1.1.7/freac-1.1.7.tar.gz' "$Ziel/freac/freac-v1.1.7-source.tar.gz"
 Download 'https://github.com/enzo1982/smooth/releases/download/v0.9.10/smooth-0.9.10.tar.gz' "$Ziel/freac/smooth-0.9.10.tar.gz"
 Download 'https://github.com/enzo1982/boca/releases/download/v1.0.6/boca-1.0.6.tar.gz' "$Ziel/freac/boca-1.0.6.tar.gz"
 # Das kleine Original-MP3Gain samt LGPL-Quellen liegt versioniert im Repository.
 if((Get-FileHash "$Ziel/mp3gain/mp3gain.exe").Hash -ne 'd46bc2e80b516a0b2423d01ee04e79d7a8d5e0b440ef54d40c8e5df3547ef1cf'){throw 'Unerwartetes MP3Gain-Programm.'}
 & "$PSScriptRoot/tools/Prepare-Fpcalc.ps1"
 Copy-Item "$PSScriptRoot/tools/fpcalc/*" "$Ziel/fpcalc" -Recurse -Force
 Download 'https://github.com/yt-dlp/yt-dlp/releases/download/2026.10.26/yt-dlp.exe' "$Ziel/yt-dlp/yt-dlp.exe" 'f5389d3698ecde4ef06f4e6b1ce0294fe9ac1396228c8990b8d3416d6d177a7d'
 Download 'https://raw.githubusercontent.com/yt-dlp/yt-dlp/2026.10.26/THIRD_PARTY_LICENSES.txt' "$Ziel/yt-dlp/THIRD_PARTY_LICENSES.txt"
 $deno=Unzip 'https://github.com/denoland/deno/releases/download/v2.9.8/deno-x86_64-pc-windows-msvc.zip' 'deno' 'cba84b26e0df5ae7f74fc66b86a95b866872a86292ebd84fa1e959927edd93bf'
 Copy-Item "$deno/deno.exe" "$Ziel/yt-dlp/deno.exe"
 if((Get-FileHash "$Ziel/yt-dlp/deno.exe").Hash -ne '924397800533d3eb0ed0c889b20ad70e18b970d93acd8135bc043f425e528266'){throw 'Unerwartetes Deno-Programm.'}
 Download 'https://raw.githubusercontent.com/denoland/deno/v2.9.8/LICENSE.md' "$Ziel/yt-dlp/LICENSE-Deno.md"
 Download 'https://raw.githubusercontent.com/yt-dlp/yt-dlp/2026.10.26/LICENSE' "$Ziel/yt-dlp/LICENSE-yt-dlp.txt"
 # Gleiche Whisper-Version wie bisher; zwei statische CPU-Varianten ohne systemweite Runtime.
 Download 'https://codeload.github.com/ggml-org/whisper.cpp/tar.gz/a8d002cfd879315632a579e73f0148d06959de36' "$Ziel/whisper/whisper-cpp-v1.7.6-source.tar.gz"
 tar -xzf "$Ziel/whisper/whisper-cpp-v1.7.6-source.tar.gz" -C $temp
 if($LASTEXITCODE){throw 'Whisper-Quellen konnten nicht entpackt werden.'}
 $src=Join-Path $temp 'whisper.cpp-a8d002cfd879315632a579e73f0148d06959de36'
 Copy-Item "$src/LICENSE" "$Ziel/whisper/LICENSE-whisper-cpp.txt"
 foreach($profile in @('avx2','compatible')) {
  $build=Join-Path $temp $profile; $simd=if($profile -eq 'avx2'){'ON'}else{'OFF'}
  cmake -S $src -B $build -A x64 -DBUILD_SHARED_LIBS=OFF -DCMAKE_POLICY_DEFAULT_CMP0091=NEW -DCMAKE_MSVC_RUNTIME_LIBRARY=MultiThreaded -DGGML_NATIVE=OFF -DGGML_OPENMP=OFF "-DGGML_AVX=$simd" "-DGGML_AVX2=$simd" "-DGGML_FMA=$simd" "-DGGML_F16C=$simd" -DWHISPER_BUILD_TESTS=OFF -DWHISPER_BUILD_SERVER=OFF
  if($LASTEXITCODE){throw "Whisper-Konfiguration fehlgeschlagen: $profile"}
  cmake --build $build --config Release --target whisper-cli --parallel 4
  if($LASTEXITCODE){throw "Whisper-Build fehlgeschlagen: $profile"}
  $name=if($profile -eq 'avx2'){'whisper-cli.exe'}else{'whisper-cli-compatible.exe'}
  Copy-Item "$build/bin/Release/whisper-cli.exe" "$Ziel/whisper/$name"
  & "$Ziel/whisper/$name" --help
  if($LASTEXITCODE){throw "Whisper-Startpruefung fehlgeschlagen: $profile"}
 }
 Download 'https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-base.bin' "$Ziel/whisper/ggml-base.bin" '60ed5bc3dd14eea856493d334349b405782ddcaf0028d4b5df4088345fba2efe'
 Download 'https://raw.githubusercontent.com/openai/whisper/v20250625/LICENSE' "$Ziel/whisper/LICENSE-OpenAI-Whisper.txt"
 Get-ChildItem $Ziel -Recurse -File | Where-Object {$_.Extension -in @('.exe','.dll','.bin')} | ForEach-Object {
  [pscustomobject]@{Datei=[IO.Path]::GetRelativePath($Ziel,$_.FullName);SHA256=(Get-FileHash $_.FullName).Hash}
 } | ConvertTo-Json | Set-Content "$Ziel/manifest.json" -Encoding utf8
} finally {if(Test-Path $temp){Remove-Item $temp -Recurse -Force}}
