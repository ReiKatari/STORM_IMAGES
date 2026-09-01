# STORM IMAGES - Automated Production Build & Release Pipeline
$ErrorActionPreference = "Stop"

$baseDir = $PSScriptRoot
if (-not $baseDir) { $baseDir = "E:\STORM IMAGES" }
$sourcesDir = Join-Path $baseDir "Sources"
$appProjDir = Join-Path $sourcesDir "StormImages"
$installerProjDir = Join-Path $sourcesDir "StormInstaller"
$launcherProjDir = Join-Path $sourcesDir "StormLauncher"
$assemblingDir = Join-Path $baseDir "Assembling"
$filesDir = Join-Path $baseDir "Files"

# Read version from csproj
[xml]$appProjXml = Get-Content (Join-Path $appProjDir "StormImages.csproj")
$appVersion = $appProjXml.Project.PropertyGroup.Version
if (-not $appVersion) { $appVersion = "0.0.1" }

Write-Host "============================================================" -ForegroundColor Cyan
Write-Host " STORM IMAGES $appVersion - PRODUCTION BUILD PIPELINE " -ForegroundColor Cyan
Write-Host "============================================================" -ForegroundColor Cyan

$setupExePath = Join-Path $filesDir "STORM_IMAGES_${appVersion}_Setup.exe"

# Step 0: Terminate running instances
Write-Host "[0/7] Closing running instances..." -ForegroundColor Yellow
cmd.exe /c "taskkill /F /IM StormImages.exe /T >nul 2>&1"
cmd.exe /c "taskkill /F /IM StormInstaller.exe /T >nul 2>&1"
cmd.exe /c "taskkill /F /IM StormLauncher.exe /T >nul 2>&1"
Get-Process "StormImages", "StormInstaller", "StormLauncher" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 1

# Step 1: Clean build directories
Write-Host "[1/7] Cleaning build directories..." -ForegroundColor Yellow
if (Test-Path "$appProjDir\bin") { Remove-Item "$appProjDir\bin" -Recurse -Force -ErrorAction SilentlyContinue }
if (Test-Path "$appProjDir\obj") { Remove-Item "$appProjDir\obj" -Recurse -Force -ErrorAction SilentlyContinue }
if (Test-Path "$installerProjDir\bin") { Remove-Item "$installerProjDir\bin" -Recurse -Force -ErrorAction SilentlyContinue }
if (Test-Path "$installerProjDir\obj") { Remove-Item "$installerProjDir\obj" -Recurse -Force -ErrorAction SilentlyContinue }
if (Test-Path "$launcherProjDir\bin") { Remove-Item "$launcherProjDir\bin" -Recurse -Force -ErrorAction SilentlyContinue }
if (Test-Path "$launcherProjDir\obj") { Remove-Item "$launcherProjDir\obj" -Recurse -Force -ErrorAction SilentlyContinue }

# Step 2: Publish Single-File App Executable & Fast Launcher
Write-Host "[2/7] Publishing App & Fast Zero-UAC Launcher..." -ForegroundColor Yellow
$appPublishDir = Join-Path $appProjDir "bin\Release\net8.0-windows\win-x64\publish"
dotnet publish "$appProjDir\StormImages.csproj" -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true

$publishedExe = Join-Path $appPublishDir "StormImages.exe"
if (-not (Test-Path $publishedExe)) {
    throw "Error: Published executable $publishedExe was not created!"
}

$launcherPublishDir = Join-Path $launcherProjDir "bin\Release\net8.0-windows\win-x64\publish"
dotnet publish "$launcherProjDir\StormLauncher.csproj" -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true

$publishedLauncher = Join-Path $launcherPublishDir "StormLauncher.exe"

# Step 3: Digital Signature (Authenticode SHA-256)
Write-Host "[3/7] Applying digital signature (Authenticode SHA-256)..." -ForegroundColor Yellow
$cert = Get-ChildItem Cert:\CurrentUser\My -CodeSigningCert | Where-Object { $_.Subject -like "*STORM TEAM*" } | Select-Object -First 1
if (-not $cert) {
    $cert = Get-ChildItem Cert:\CurrentUser\My -CodeSigningCert | Where-Object { $_.Subject -like "*STORM Software*" -or $_.Subject -like "*STORM*" } | Select-Object -First 1
}

$cerPath = Join-Path $filesDir "STORM_Certificate.cer"
if ($cert) {
    [System.IO.File]::WriteAllBytes($cerPath, $cert.Export([System.Security.Cryptography.X509Certificates.X509ContentType]::Cert))
    Copy-Item $cerPath (Join-Path $filesDir "StormTeamRootCA.cer") -Force -ErrorAction SilentlyContinue
    Copy-Item $cerPath (Join-Path $filesDir "StormSoftwareRootCA.cer") -Force -ErrorAction SilentlyContinue
    Copy-Item $cerPath (Join-Path $baseDir "StormTeamRootCA.cer") -Force -ErrorAction SilentlyContinue
    Copy-Item $cerPath (Join-Path $baseDir "STORM_Certificate.cer") -Force -ErrorAction SilentlyContinue

    Set-AuthenticodeSignature -FilePath $publishedExe -Certificate $cert -HashAlgorithm SHA256 | Out-Null
    if (Test-Path $publishedLauncher) {
        Set-AuthenticodeSignature -FilePath $publishedLauncher -Certificate $cert -HashAlgorithm SHA256 | Out-Null
    }
}

# Step 4: Packaging binaries into Assembling & Installer Resources
Write-Host "[4/7] Packaging binaries into Assembling & Installer Resources..." -ForegroundColor Yellow
if (-not (Test-Path $assemblingDir)) { New-Item -ItemType Directory -Path $assemblingDir | Out-Null }
try {
    Copy-Item $publishedExe "$assemblingDir\StormImages.exe" -Force -ErrorAction Stop
    if (Test-Path $publishedLauncher) {
        Copy-Item $publishedLauncher "$assemblingDir\StormLauncher.exe" -Force -ErrorAction Stop
    }
    # Copy Server scripts into Assembling
    $serverDir = Join-Path $sourcesDir "StormImagesServer"
    $assemblingServerDir = Join-Path $assemblingDir "StormImagesServer"
    if (-not (Test-Path $assemblingServerDir)) { New-Item -ItemType Directory -Path $assemblingServerDir | Out-Null }
    Copy-Item "$serverDir\*" $assemblingServerDir -Recurse -Force -ErrorAction SilentlyContinue
} catch {
    Write-Host "Note: Output binaries in Assembling are currently in use." -ForegroundColor DarkGray
}

$installerResDir = Join-Path $installerProjDir "Resources"
if (-not (Test-Path $installerResDir)) { New-Item -ItemType Directory -Path $installerResDir | Out-Null }
try {
    Copy-Item $publishedExe "$installerResDir\StormImages.exe" -Force -ErrorAction SilentlyContinue
    if (Test-Path $publishedLauncher) {
        Copy-Item $publishedLauncher "$installerResDir\StormLauncher.exe" -Force -ErrorAction SilentlyContinue
    }
    if (Test-Path $cerPath) {
        Copy-Item $cerPath "$installerResDir\STORM_Certificate.cer" -Force -ErrorAction SilentlyContinue
    }
} catch { }

# Step 5: Publish & Sign Installer
Write-Host "[5/7] Publishing and Signing Installer $appVersion..." -ForegroundColor Yellow
$installerPublishDir = Join-Path $installerProjDir "bin\Release\net8.0-windows\win-x64\publish"
dotnet publish "$installerProjDir\StormInstaller.csproj" -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true

$publishedInstaller = Join-Path $installerPublishDir "StormInstaller.exe"
if (-not (Test-Path $publishedInstaller)) {
    throw "Error: Published installer $publishedInstaller was not created!"
}

Copy-Item $publishedInstaller $setupExePath -Force

if ($cert) {
    Set-AuthenticodeSignature -FilePath $setupExePath -Certificate $cert -HashAlgorithm SHA256 | Out-Null
    if (Test-Path "$assemblingDir\StormImages.exe") {
        try { Set-AuthenticodeSignature -FilePath "$assemblingDir\StormImages.exe" -Certificate $cert -HashAlgorithm SHA256 -ErrorAction SilentlyContinue | Out-Null } catch {}
    }
}

# Step 6: Create ZIP archive for Portable distribution
Write-Host "[6/7] Creating Portable ZIP archive..." -ForegroundColor Yellow
$zipPath = Join-Path $filesDir "STORM_IMAGES_${appVersion}.zip"
if (Test-Path $zipPath) { Remove-Item $zipPath -Force -ErrorAction SilentlyContinue }
try {
    Compress-Archive -Path "$assemblingDir\*" -DestinationPath $zipPath -Force
    Write-Host "Portable ZIP archive created: $zipPath" -ForegroundColor Green
} catch {
    Write-Host "Warning: Could not create ZIP: $_" -ForegroundColor DarkYellow
}

# Step 7: Unblock Files and apply exclusions
Write-Host "[7/7] Unblocking output files..." -ForegroundColor Yellow
Get-ChildItem -Path $baseDir -Recurse -Include *.exe, *.dll, *.bat, *.ps1, *.cer, *.zip -ErrorAction SilentlyContinue | ForEach-Object {
    Unblock-File -Path $_.FullName -ErrorAction SilentlyContinue
}

Write-Host "============================================================" -ForegroundColor Green
Write-Host " RELEASE $appVersion SUCCESSFULLY BUILT AND PACKAGED! " -ForegroundColor Green
Write-Host "============================================================" -ForegroundColor Green
Write-Host " 1. Portable EXE: $assemblingDir\StormImages.exe" -ForegroundColor Cyan
Write-Host " 2. Installer:    $setupExePath" -ForegroundColor Cyan
Write-Host " 3. Portable ZIP: $zipPath" -ForegroundColor Cyan
Write-Host "============================================================" -ForegroundColor Green