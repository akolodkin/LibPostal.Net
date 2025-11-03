# LibPostal.Net Data Distribution Guide

This document explains how LibPostal.NET distributes and uses the ~2GB of pre-trained models from the libpostal project.

---

## Why a Separate Data Package?

**Problem**: NuGet.org has a 250MB hard limit per package, but libpostal models are ~2GB.

**Solution**: The `LibPostal.Net.Data` package (~100 KB) contains download scripts that automatically fetch models from GitHub Releases on first build.

---

## Quick Start

### Installation

```bash
# Install both packages
dotnet add package LibPostal.Net
dotnet add package LibPostal.Net.Data

# Build your project (models auto-download on first build)
dotnet build
```

### Usage

```csharp
using LibPostal.Net.Parser;

// Simple - uses default location (~/.libpostal)
var parser = AddressParser.LoadDefault();
var result = parser.Parse("123 Main Street Brooklyn NY 11216");

Console.WriteLine(result.GetComponent("house_number")); // "123"
Console.WriteLine(result.GetComponent("road"));         // "main street"
Console.WriteLine(result.GetComponent("city"));         // "brooklyn"
```

---

## How It Works

### Auto-Download Process

1. **Install LibPostal.Net.Data** via NuGet
2. **First build** triggers MSBuild target
3. **Download script** executes (PowerShell on Windows, Bash on Linux/Mac)
4. **Models downloaded** from GitHub Releases (~2GB, takes 5-10 minutes)
5. **Extracted to** `~/.libpostal` (or `%USERPROFILE%\.libpostal` on Windows)
6. **Subsequent builds** skip download (models cached)

### What Gets Downloaded

**Parser Component** (~1.8GB):
- `address_parser/address_parser_crf.dat` - CRF model weights
- `address_parser/address_parser_vocab.trie` - Vocabulary
- `address_parser/address_parser_phrases.dat` - Dictionary phrases
- `address_parser/address_parser_postal_codes.dat` - Postal codes

**Language Classifier** (~200MB):
- `language_classifier/language_classifier.dat` - Language detection model

**Base Data** (~1.8GB):
- `address_expansions/` - Address normalization dictionaries
- `numex/` - Numeric expression data
- `transliteration/` - Transliteration tables

---

## Configuration Options

### 1. Environment Variable

```bash
# Linux/Mac
export LIBPOSTAL_DATA_DIR=/opt/libpostal-data

# Windows
set LIBPOSTAL_DATA_DIR=C:\LibPostal\Data

# Then in code:
var parser = AddressParser.LoadDefault(); // Uses LIBPOSTAL_DATA_DIR
```

### 2. MSBuild Properties

```xml
<!-- In your .csproj or Directory.Build.props -->
<PropertyGroup>
  <!-- Custom data directory -->
  <LibPostalDataDir>C:\CustomPath\libpostal</LibPostalDataDir>

  <!-- Disable auto-download -->
  <LibPostalAutoDownload>false</LibPostalAutoDownload>

  <!-- Specify model version -->
  <LibPostalModelVersion>v1.0.0</LibPostalModelVersion>

  <!-- Choose components (parser, language_classifier, base, or all) -->
  <LibPostalComponents>parser</LibPostalComponents>
</PropertyGroup>
```

### 3. Programmatic Download

```csharp
using LibPostal.Net.Data;

var downloader = new ModelDownloader("C:\\CustomPath");

// Download with progress reporting
var progress = new Progress<DownloadProgress>(p =>
{
    Console.WriteLine($"{p.Component}: {p.Status} - {p.PercentComplete}%");
});

await downloader.DownloadModelsAsync(
    ModelComponent.Parser,
    version: "v1.0.0",
    progress: progress
);

// Then load parser
var parser = AddressParser.LoadFromDirectory("C:\\CustomPath");
```

---

## Manual Download

If auto-download fails or you prefer manual control:

### Option 1: PowerShell Script (Windows)

```powershell
# Run the download script directly
pwsh LibPostal.Net.Data/tools/download-models.ps1 -DataDir "C:\LibPostal" -Component parser

# Or with defaults (~/.libpostal)
pwsh LibPostal.Net.Data/tools/download-models.ps1
```

### Option 2: Bash Script (Linux/Mac)

```bash
# Run the download script
bash LibPostal.Net.Data/tools/download-models.sh --data-dir ~/.libpostal parser

# Or download all components
bash LibPostal.Net.Data/tools/download-models.sh all
```

### Option 3: Direct Download

Download from GitHub Releases manually:

```bash
# Create directory
mkdir -p ~/.libpostal
cd ~/.libpostal

# Download parser models
curl -L https://github.com/openvenues/libpostal/releases/download/v1.0.0/parser.tar.gz -o parser.tar.gz
tar -xzf parser.tar.gz
rm parser.tar.gz

# Verify
ls -lh address_parser/address_parser_crf.dat
```

---

## Troubleshooting

### Models Not Downloading

**Symptoms**: Build succeeds but models not found

**Solutions**:
1. Check internet connection
2. Verify GitHub isn't blocked by firewall
3. Run download script manually with verbose output
4. Check disk space (need ~2GB free)
5. Disable auto-download and use manual process

### "tar command not found"

**Windows**: Install Git for Windows (includes tar) or use Windows 10+ built-in tar

**Linux/Mac**: Install tar via package manager:
```bash
# Ubuntu/Debian
sudo apt-get install tar

# macOS
brew install gnu-tar
```

### PowerShell Execution Policy

**Error**: "cannot be loaded because running scripts is disabled"

**Solution**:
```powershell
Set-ExecutionPolicy -Scope CurrentUser -ExecutionPolicy RemoteSigned
```

### Permission Denied

**Linux/Mac**: Make script executable:
```bash
chmod +x LibPostal.Net.Data/tools/download-models.sh
```

### Slow Downloads

- Downloads are ~2GB, expect 5-15 minutes depending on connection
- Use wired connection if possible
- Consider manual download if automated download repeatedly fails

---

## Advanced Scenarios

### CI/CD Integration

#### GitHub Actions

```yaml
jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3

      - name: Cache LibPostal Models
        uses: actions/cache@v3
        with:
          path: ~/.libpostal
          key: libpostal-models-v1.0.0

      - name: Build (auto-downloads models if not cached)
        run: dotnet build

      - name: Test
        run: dotnet test
```

#### Docker

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:9.0

# Pre-download models during image build
RUN mkdir -p ~/.libpostal && \
    curl -L https://github.com/openvenues/libpostal/releases/download/v1.0.0/parser.tar.gz | \
    tar -xz -C ~/.libpostal

WORKDIR /app
COPY . .
RUN dotnet build

# Models already available, no download on container start
ENTRYPOINT ["dotnet", "run"]
```

### Multiple Projects Sharing Models

Models are cached in `~/.libpostal` and automatically shared across all projects on the same machine:

```
~/.libpostal/
├── address_parser/
│   └── address_parser_crf.dat  ← Shared by all projects
├── language_classifier/
└── ...

Project1/ ← Uses ~/.libpostal
Project2/ ← Uses ~/.libpostal (no re-download!)
Project3/ ← Uses ~/.libpostal
```

### Updating Models

```bash
# Re-download latest version
pwsh download-models.ps1 -Force

# Or delete cache and rebuild
rm -rf ~/.libpostal
dotnet build
```

---

## Model Details

### Source

Models are trained by the libpostal project on:
- **1 billion+** addresses from OpenStreetMap
- **OpenAddresses** dataset
- **60+ languages**
- **99.45% accuracy** (C implementation)

### License

- **Code**: MIT License
- **Data**: Derived from OpenStreetMap (ODbL) and OpenAddresses (various)
- **Attribution**: Required for ODbL data

### Version History

- **v1.0.0** (2017-04-07): Initial release, current stable version

---

## File Sizes

| Component | Compressed | Extracted | Files |
|-----------|------------|-----------|-------|
| Parser | ~1.8GB | ~2.2GB | 4 main files + dictionaries |
| Language Classifier | ~200MB | ~250MB | 1 file |
| Base Data | ~1.8GB | ~2.2GB | Expansions, numex, transliteration |
| **Total** | **~3.8GB** | **~4.6GB** | - |

**Note**: Downloading only `parser` component (~1.8GB) is sufficient for address parsing.

---

## FAQ

**Q: Why not include models in the NuGet package?**
A: NuGet.org has a 250MB hard limit. Our models are ~2GB.

**Q: Can I use my own models?**
A: Yes! Use `AddressParser.LoadFromDirectory("your/path")` with custom trained models.

**Q: Do I need all components?**
A: No. For address parsing, only `parser` is required (~1.8GB).

**Q: How often should I update models?**
A: libpostal v1.0.0 (2017) is still current. Update when new versions released.

**Q: Can I disable auto-download?**
A: Yes, set `<LibPostalAutoDownload>false</LibPostalAutoDownload>` in your project.

**Q: Where are models stored?**
A: `~/.libpostal` by default (Linux/Mac) or `%USERPROFILE%\.libpostal` (Windows).

---

## Support

- **Issues**: https://github.com/yourusername/LibPostal.Net/issues
- **Libpostal Upstream**: https://github.com/openvenues/libpostal
- **Model Download**: https://github.com/openvenues/libpostal/releases

---

*LibPostal.NET Data Distribution - Version 1.0.0*
