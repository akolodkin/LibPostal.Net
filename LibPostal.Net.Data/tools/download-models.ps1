# LibPostal Model Downloader
# Downloads libpostal models from GitHub Releases
# Based on libpostal's libpostal_data.in script

param(
    [string]$DataDir = "$env:USERPROFILE\.libpostal",
    [string]$Version = "v1.0.0",
    [string]$Component = "parser",  # parser, language_classifier, base, or all
    [switch]$Force = $false
)

$ErrorActionPreference = "Stop"

# GitHub release base URL
$baseUrl = "https://github.com/openvenues/libpostal/releases/download/$Version"

# Component definitions
$components = @{
    parser = @{
        file = "parser.tar.gz"
        extractDir = "address_parser"
        requiredFiles = @("address_parser_crf.dat", "address_parser_vocab.trie")
    }
    language_classifier = @{
        file = "language_classifier.tar.gz"
        extractDir = "language_classifier"
        requiredFiles = @("language_classifier.dat")
    }
    base = @{
        file = "libpostal_data.tar.gz"
        extractDir = "."
        requiredFiles = @("numex", "address_expansions")
    }
}

function Test-ModelsExist {
    param($ComponentKey)

    $comp = $components[$ComponentKey]
    $extractPath = Join-Path $DataDir $comp.extractDir

    if (-not (Test-Path $extractPath)) {
        return $false
    }

    foreach ($file in $comp.requiredFiles) {
        $filePath = Join-Path $extractPath $file
        if (-not (Test-Path $filePath)) {
            return $false
        }
    }

    return $true
}

function Download-Component {
    param($ComponentKey)

    Write-Host "Downloading $ComponentKey component..." -ForegroundColor Cyan

    $comp = $components[$ComponentKey]
    $url = "$baseUrl/$($comp.file)"
    $archivePath = Join-Path $DataDir $comp.file

    # Create data directory
    New-Item -Path $DataDir -ItemType Directory -Force | Out-Null

    # Download archive
    Write-Host "  URL: $url" -ForegroundColor Gray
    Write-Host "  Downloading to: $archivePath" -ForegroundColor Gray

    try {
        # Use BITS (Background Intelligent Transfer Service) for reliable downloads
        if (Get-Command Start-BitsTransfer -ErrorAction SilentlyContinue) {
            Start-BitsTransfer -Source $url -Destination $archivePath -DisplayName "LibPostal $ComponentKey"
        } else {
            # Fallback to Invoke-WebRequest
            $ProgressPreference = 'SilentlyContinue'
            Invoke-WebRequest -Uri $url -OutFile $archivePath -UseBasicParsing
            $ProgressPreference = 'Continue'
        }
    } catch {
        Write-Error "Failed to download $ComponentKey`: $_"
        throw
    }

    # Extract archive
    Write-Host "  Extracting $($comp.file)..." -ForegroundColor Gray

    # Check if tar is available (Windows 10+ has built-in tar)
    if (Get-Command tar -ErrorAction SilentlyContinue) {
        tar -xzf $archivePath -C $DataDir
    } else {
        # Fallback to .NET compression (slower)
        Write-Host "  Warning: tar not found, using .NET compression (slower)" -ForegroundColor Yellow

        # This would require SharpZipLib or similar for .tar.gz
        # For now, require tar to be installed
        throw "tar command not found. Please install tar or Git for Windows (includes tar)."
    }

    # Clean up archive
    Remove-Item $archivePath -ErrorAction SilentlyContinue

    Write-Host "  $ComponentKey downloaded successfully!" -ForegroundColor Green
}

# Main execution
Write-Host "`nLibPostal Model Downloader" -ForegroundColor Cyan
Write-Host "Version: $Version" -ForegroundColor Gray
Write-Host "Data Directory: $DataDir`n" -ForegroundColor Gray

# Determine components to download
$toDownload = @()
if ($Component -eq "all") {
    $toDownload = @("parser", "language_classifier", "base")
} else {
    $toDownload = @($Component)
}

# Check each component
$downloadNeeded = $false
foreach ($comp in $toDownload) {
    if (-not $components.ContainsKey($comp)) {
        Write-Error "Unknown component: $comp. Valid: parser, language_classifier, base, all"
        exit 1
    }

    if ((Test-ModelsExist $comp) -and -not $Force) {
        Write-Host "✓ $comp already exists, skipping..." -ForegroundColor Green
    } else {
        $downloadNeeded = $true
        Download-Component $comp
    }
}

if (-not $downloadNeeded) {
    Write-Host "`n✓ All models already downloaded!" -ForegroundColor Green
    Write-Host "  Use -Force to re-download`n" -ForegroundColor Gray
} else {
    Write-Host "`n✓ All models downloaded successfully!" -ForegroundColor Green
    Write-Host "  Data directory: $DataDir`n" -ForegroundColor Gray
}

# Verify installation
Write-Host "Verifying installation..." -ForegroundColor Cyan
$parserDir = Join-Path $DataDir "address_parser"
if (Test-Path (Join-Path $parserDir "address_parser_crf.dat")) {
    $fileSize = (Get-Item (Join-Path $parserDir "address_parser_crf.dat")).Length
    Write-Host "  ✓ Parser model: $('{0:N2}' -f ($fileSize / 1MB)) MB" -ForegroundColor Green
} else {
    Write-Warning "  Parser model not found!"
}

Write-Host "`nLibPostal models ready at: $DataDir" -ForegroundColor Cyan
