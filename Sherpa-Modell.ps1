param([Parameter(Mandatory=$true)][string]$Ziel)
$ErrorActionPreference='Stop'
$folder=Join-Path $Ziel 'sherpa-onnx'
$names=@('base-encoder.int8.onnx','base-decoder.int8.onnx','base-tokens.txt')
if(@($names | Where-Object {-not(Test-Path (Join-Path $folder $_))}).Count -eq 0){Write-Host 'Sherpa-Modell vorhanden; kein Download.';exit 0}
New-Item -ItemType Directory -Force $folder | Out-Null
$temp=Join-Path ([IO.Path]::GetTempPath()) ('sherpa-'+[guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Force $temp | Out-Null
try {
 $archive=Join-Path $temp 'model.tar.bz2'
 $url='https://github.com/k2-fsa/sherpa-onnx/releases/download/asr-models/sherpa-onnx-whisper-base.tar.bz2'
 curl.exe --fail --location --retry 4 --retry-all-errors --retry-delay 5 --connect-timeout 20 --max-time 360 --output $archive $url
 if($LASTEXITCODE){throw "Sherpa-Modell-Download fehlgeschlagen: $LASTEXITCODE"}
 tar -xjf $archive -C $temp
 if($LASTEXITCODE){throw 'Sherpa-Modell konnte nicht entpackt werden.'}
 foreach($name in $names){
  $files=@(Get-ChildItem $temp -Recurse -File -Filter $name)
  if($files.Count -ne 1){throw "Sherpa-Datei fehlt: $name"}
  Copy-Item $files[0].FullName (Join-Path $folder $name) -Force
 }
 Write-Host 'Nur Sherpa-Modell ergänzt; alle bestehenden Werkzeuge unverändert.'
}finally{Remove-Item $temp -Recurse -Force -ErrorAction SilentlyContinue}
