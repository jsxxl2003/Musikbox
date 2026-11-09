param([Parameter(Mandatory=$true)][string]$Ziel)
$ErrorActionPreference='Stop'
$folder=Join-Path $Ziel 'sherpa-onnx'
$names=@('base-encoder.int8.onnx','base-decoder.int8.onnx','base-tokens.txt')
if(@($names | Where-Object {-not(Test-Path (Join-Path $folder $_))}).Count -eq 0){Write-Host 'Sherpa-Modell vorhanden; kein Download.';exit 0}
New-Item -ItemType Directory -Force $folder | Out-Null
$temp=Join-Path ([IO.Path]::GetTempPath()) ('sherpa-'+[guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Force $temp | Out-Null
try {
 $base='https://huggingface.co/csukuangfj/sherpa-onnx-whisper-base/resolve/main/'
 foreach($name in $names){
  $destination=Join-Path $folder $name
  if((Test-Path $destination) -and (Get-Item $destination).Length -gt 100000){continue}
  $partial=Join-Path $temp $name
  $url=$base+$name+'?download=true'
  curl.exe --fail --location --retry 4 --retry-all-errors --retry-delay 5 --connect-timeout 20 --max-time 900 --silent --show-error --output $partial $url
  if($LASTEXITCODE){throw "Sherpa-Modell-Datei konnte nicht geladen werden: $name (curl $LASTEXITCODE)"}
  if((Get-Item $partial).Length -lt 100000){throw "Sherpa-Modell-Datei unvollständig: $name"}
  Move-Item $partial $destination -Force
 }
 Write-Host 'Nur Sherpa-Modell ergänzt; alle bestehenden Werkzeuge unverändert.'
}finally{Remove-Item $temp -Recurse -Force -ErrorAction SilentlyContinue}
