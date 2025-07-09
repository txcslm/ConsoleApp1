# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Structure

This repository contains two .NET console applications:

1. **ChubDownloader** - Main application for downloading character data from chub.ai
2. **ConsoleApp1** - Secondary console application (appears to be a template/starter project)

The primary application (ChubDownloader) follows MVP (Model-View-Presenter) architecture:

- **Models/** - Data models (CharacterInfo, DownloadMode, Segment)
- **Views/** - User interface layer (ConsoleView implementing IMainView)
- **Presenters/** - Business logic coordination (MainPresenter)
- **Services/** - Core business logic (CharacterScraper, DownloadService, WebDriverService)

## Common Commands

### Build and Run
```bash
# Build the solution
dotnet build

# Run ChubDownloader (main application)
dotnet run --project ChubDownloader

# Run ConsoleApp1 (secondary project)
dotnet run --project ConsoleApp1
```

### Development Commands
```bash
# Restore packages
dotnet restore

# Clean build artifacts
dotnet clean

# Build in release mode
dotnet build --configuration Release
```

## Key Dependencies

- **Selenium WebDriver** - For web automation and scraping
- **Newtonsoft.Json** - JSON serialization (ConsoleApp1 only)
- **System.Text.Json** - JSON handling (ChubDownloader uses built-in)
- **ChromeDriver** - Chrome browser automation

## Architecture Notes

### ChubDownloader Application Flow
1. **Program.cs** - Entry point, initializes ConsoleView and MainPresenter
2. **MainPresenter** - Coordinates between view and services, handles async operations
3. **ConsoleView** - Handles user input/output, validates parameters
4. **CharacterScraper** - Core scraping logic with two modes:
   - Leaderboard downloading (followers-based)
   - Segment-based downloading (quality, trending, etc.)
5. **WebDriverService** - Selenium WebDriver management
6. **DownloadService** - File download handling

### Key Service Interfaces
- `ICharacterScraper` - Character scraping operations
- `IDownloadService` - File download operations  
- `IWebDriverService` - WebDriver management
- `IMainView` - UI abstraction

### Data Flow
The application uses event-driven architecture where the view fires `DownloadRequested` events that the presenter handles asynchronously with progress reporting.

## File Organization

- **temp_downloads/** - Temporary download directory
- **ChromeProfile/** - Chrome browser profile storage
- **followers/** - Downloaded leaderboard characters
- **characters2/** - Downloaded segment-based characters
- **character_index.json** - Global character index to prevent duplicates

## Development Notes

The application uses Russian language for console output and supports two download modes with different character filtering criteria. The scraper implements retry logic and maintains a character index to avoid duplicate downloads.