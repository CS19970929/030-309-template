param(
    [switch]$Quiet
)

$requiredPaths = @(
    "C:\Users\Administrator\AppData\Local\Microsoft\WinGet\Links",
    "C:\Program Files\CMake\bin",
    "C:\Program Files (x86)\Arm GNU Toolchain arm-none-eabi\14.2 rel1\bin",
    "C:\Program Files\SEGGER\JLink_V818",
    "C:\Users\Administrator\AppData\Local\Microsoft\WinGet\Packages\xpack-dev-tools.openocd-xpack_Microsoft.Winget.Source_8wekyb3d8bbwe\xpack-openocd-0.12.0-7\bin"
)

$currentSegments = @($env:PATH -split ';' | Where-Object { $_ -and $_.Trim() -ne "" })
$newSegments = New-Object System.Collections.Generic.List[string]

foreach ($pathEntry in $requiredPaths) {
    if ((Test-Path $pathEntry) -and -not $newSegments.Contains($pathEntry)) {
        $null = $newSegments.Add($pathEntry)
    }
}

foreach ($pathEntry in $currentSegments) {
    if (-not $newSegments.Contains($pathEntry)) {
        $null = $newSegments.Add($pathEntry)
    }
}

$env:PATH = ($newSegments -join ';')

if (-not $Quiet) {
    $toolMap = @{
        task = "task"
        cmake = "cmake"
        gcc = "arm-none-eabi-gcc"
        jlink = "JLink.exe"
        openocd = "openocd"
    }

    Write-Host "PATH refreshed." -ForegroundColor Green
    foreach ($tool in $toolMap.Keys) {
        $resolved = (Get-Command $toolMap[$tool] -ErrorAction SilentlyContinue).Source
        if ($resolved) {
            Write-Host ("[OK] {0} -> {1}" -f $tool, $resolved)
        }
        else {
            Write-Host ("[MISS] {0}" -f $tool) -ForegroundColor Yellow
        }
    }
}
