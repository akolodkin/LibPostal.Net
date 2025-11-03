#!/bin/bash
# LibPostal Model Downloader
# Downloads libpostal models from GitHub Releases
# Based on libpostal's libpostal_data.in script

set -e

# Default parameters
DATA_DIR="${LIBPOSTAL_DATA_DIR:-$HOME/.libpostal}"
VERSION="${LIBPOSTAL_VERSION:-v1.0.0}"
COMPONENT="${1:-parser}"
FORCE=false

# Parse command line arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        --data-dir)
            DATA_DIR="$2"
            shift 2
            ;;
        --version)
            VERSION="$2"
            shift 2
            ;;
        --force)
            FORCE=true
            shift
            ;;
        *)
            COMPONENT="$1"
            shift
            ;;
    esac
done

# GitHub release base URL
BASE_URL="https://github.com/openvenues/libpostal/releases/download/$VERSION"

# Color codes
RED='\033[0;31m'
GREEN='\033[0;32m'
CYAN='\033[0;36m'
GRAY='\033[0;90m'
NC='\033[0m' # No Color

echo -e "${CYAN}LibPostal Model Downloader${NC}"
echo -e "${GRAY}Version: $VERSION${NC}"
echo -e "${GRAY}Data Directory: $DATA_DIR${NC}"
echo ""

# Component definitions
declare -A PARSER_COMP=(
    [file]="parser.tar.gz"
    [extract_dir]="address_parser"
    [required_files]="address_parser_crf.dat address_parser_vocab.trie"
)

declare -A LANG_COMP=(
    [file]="language_classifier.tar.gz"
    [extract_dir]="language_classifier"
    [required_files]="language_classifier.dat"
)

declare -A BASE_COMP=(
    [file]="libpostal_data.tar.gz"
    [extract_dir]="."
    [required_files]="numex address_expansions"
)

# Check if models exist
check_models_exist() {
    local comp_name=$1
    local extract_dir required_files

    case $comp_name in
        parser)
            extract_dir="${PARSER_COMP[extract_dir]}"
            required_files="${PARSER_COMP[required_files]}"
            ;;
        language_classifier)
            extract_dir="${LANG_COMP[extract_dir]}"
            required_files="${LANG_COMP[required_files]}"
            ;;
        base)
            extract_dir="${BASE_COMP[extract_dir]}"
            required_files="${BASE_COMP[required_files]}"
            ;;
        *)
            echo -e "${RED}Unknown component: $comp_name${NC}"
            return 1
            ;;
    esac

    local full_extract_dir="$DATA_DIR/$extract_dir"

    if [ ! -d "$full_extract_dir" ]; then
        return 1
    fi

    for file in $required_files; do
        if [ ! -e "$full_extract_dir/$file" ]; then
            return 1
        fi
    done

    return 0
}

# Download and extract component
download_component() {
    local comp_name=$1
    local file url archive_path

    case $comp_name in
        parser)
            file="${PARSER_COMP[file]}"
            ;;
        language_classifier)
            file="${LANG_COMP[file]}"
            ;;
        base)
            file="${BASE_COMP[file]}"
            ;;
        *)
            echo -e "${RED}Unknown component: $comp_name${NC}"
            return 1
            ;;
    esac

    url="$BASE_URL/$file"
    archive_path="$DATA_DIR/$file"

    echo -e "${CYAN}Downloading $comp_name component...${NC}"
    echo -e "${GRAY}  URL: $url${NC}"
    echo -e "${GRAY}  Archive: $archive_path${NC}"

    # Create data directory
    mkdir -p "$DATA_DIR"

    # Download with curl (more portable than wget)
    if command -v curl &> /dev/null; then
        curl -L --progress-bar -o "$archive_path" "$url"
    elif command -v wget &> /dev/null; then
        wget --show-progress -O "$archive_path" "$url"
    else
        echo -e "${RED}Error: Neither curl nor wget found. Please install curl or wget.${NC}"
        return 1
    fi

    # Extract archive
    echo -e "${GRAY}  Extracting...${NC}"

    if command -v tar &> /dev/null; then
        tar -xzf "$archive_path" -C "$DATA_DIR"
    else
        echo -e "${RED}Error: tar command not found. Please install tar.${NC}"
        return 1
    fi

    # Clean up archive
    rm -f "$archive_path"

    echo -e "${GREEN}  ✓ $comp_name downloaded successfully!${NC}"
}

# Main execution
if [ "$COMPONENT" = "all" ]; then
    COMPONENTS_TO_DOWNLOAD=("parser" "language_classifier" "base")
else
    COMPONENTS_TO_DOWNLOAD=("$COMPONENT")
fi

download_needed=false

for comp in "${COMPONENTS_TO_DOWNLOAD[@]}"; do
    if check_models_exist "$comp" && [ "$FORCE" = false ]; then
        echo -e "${GREEN}✓ $comp already exists, skipping...${NC}"
    else
        download_needed=true
        download_component "$comp"
    fi
done

if [ "$download_needed" = false ]; then
    echo ""
    echo -e "${GREEN}✓ All models already downloaded!${NC}"
    echo -e "${GRAY}  Use --force to re-download${NC}"
else
    echo ""
    echo -e "${GREEN}✓ All models downloaded successfully!${NC}"
    echo -e "${GRAY}  Data directory: $DATA_DIR${NC}"
fi

# Verify installation
echo ""
echo -e "${CYAN}Verifying installation...${NC}"
parser_model="$DATA_DIR/address_parser/address_parser_crf.dat"
if [ -f "$parser_model" ]; then
    file_size=$(du -h "$parser_model" | cut -f1)
    echo -e "${GREEN}  ✓ Parser model: $file_size${NC}"
else
    echo -e "${RED}  ✗ Parser model not found!${NC}"
fi

echo ""
echo -e "${CYAN}LibPostal models ready at: $DATA_DIR${NC}"
