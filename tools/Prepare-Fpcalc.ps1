$ErrorActionPreference = 'Stop'
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
$destination = Join-Path $PSScriptRoot 'fpcalc'
$manifest = Get-Content -LiteralPath (Join-Path $destination 'manifest.json') -Raw | ConvertFrom-Json
function Test-Content($path, $hash) {
    return (Test-Path -LiteralPath $path -PathType Leaf) -and ((Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -eq $hash)
}
$temporary = Join-Path ([IO.Path]::GetTempPath()) ('Musikbox-fpcalc-' + [Guid]::NewGuid().ToString('N'))
try {
    foreach ($entry in $manifest.files) {
        $target = Join-Path $destination $entry.path
        if (Test-Content $target $entry.sha256) { continue }
        New-Item -ItemType Directory -Path $temporary -Force | Out-Null
        $download = Join-Path $temporary 'download'
        Invoke-WebRequest -UseBasicParsing -Uri $entry.url -OutFile $download
        if ($entry.archive_entry) {
            if (-not (Test-Content $download $entry.archive_sha256)) { throw 'fpcalc-Download hat eine unerwartete Pruefsumme.' }
            Add-Type -AssemblyName System.IO.Compression.FileSystem
            $archive = [IO.Compression.ZipFile]::OpenRead($download)
            try {
                $item = $archive.GetEntry($entry.archive_entry)
                if ($null -eq $item) { throw 'fpcalc.exe fehlt im Downloadarchiv.' }
                $extracted = Join-Path $temporary 'fpcalc.exe'
                [IO.Compression.ZipFileExtensions]::ExtractToFile($item, $extracted, $true)
                $download = $extracted
            } finally { $archive.Dispose() }
        }
        if (-not (Test-Content $download $entry.sha256)) { throw ('Unerwartete Pruefsumme: ' + $entry.path) }
        New-Item -ItemType Directory -Path (Split-Path -Parent $target) -Force | Out-Null
        Copy-Item -LiteralPath $download -Destination $target -Force
    }
    Write-Host 'fpcalc 1.6.2 mit Lizenztexten und Quellen ist vorbereitet.'
} finally {
    if (Test-Path -LiteralPath $temporary) { Remove-Item -LiteralPath $temporary -Recurse -Force }
}
