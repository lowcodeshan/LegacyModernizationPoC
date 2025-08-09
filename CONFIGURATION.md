# Configuration System

The Legacy Modernization Pipeline supports a flexible, hierarchical configuration system that follows industry best practices.

## Configuration Hierarchy

The pipeline uses the following configuration priority (highest to lowest):

1. **Environment Variables** (Highest Priority)
2. **appsettings.json** (Fallback)
3. **Error** (No valid configuration found)

## Configuration Methods

### 1. `CreateWithFallback()` - Recommended for Production
Uses the configuration hierarchy: Environment Variables → appsettings.json → Error

```csharp
var config = PipelineConfiguration.CreateWithFallback();
```

### 2. `CreateFromEnvironment()` - Strict Environment Variables Only
Requires all environment variables to be set, throws exception if any are missing.

```csharp
var config = PipelineConfiguration.CreateFromEnvironment();
```

### 3. `CreateDefault()` - Development Only
Uses appsettings.json with intelligent defaults as fallback.

```csharp
var config = PipelineConfiguration.CreateDefault();
```

### 4. `CreateForProduction()` - Alias for CreateWithFallback()
```csharp
var config = PipelineConfiguration.CreateForProduction();
```

## Environment Variables

All environment variables are required when using environment variable configuration:

| Variable | Description | Example (Windows) | Example (Linux) |
|----------|-------------|-------------------|-----------------|
| `LEGACY_PROJECT_BASE` | Base directory for the project | `C:\Production\LegacyModernization` | `/production/legacy-modernization` |
| `LEGACY_INPUT_PATH` | Input files directory | `\\FileServer\MonthlyData\Input` | `/data/monthly-input` |
| `LEGACY_OUTPUT_PATH` | Output files directory | `\\FileServer\MonthlyData\Output` | `/data/monthly-output` |
| `LEGACY_LOG_PATH` | Log files directory | `C:\Logs\Production` | `/var/logs/legacy-modernization` |
| `LEGACY_TESTDATA_PATH` | Test data files directory | `C:\Production\TestData` | `/production/test-data` |
| `LEGACY_EXPECTED_PATH` | Expected output files directory | `\\FileServer\Validation\Expected` | `/data/validation/expected` |

### Setting Environment Variables

**Windows (Command Prompt):**
```cmd
set LEGACY_PROJECT_BASE=C:\Production\LegacyModernization
set LEGACY_INPUT_PATH=\\FileServer\MonthlyData\Input
set LEGACY_OUTPUT_PATH=\\FileServer\MonthlyData\Output
set LEGACY_LOG_PATH=C:\Logs\Production
set LEGACY_TESTDATA_PATH=C:\Production\TestData
set LEGACY_EXPECTED_PATH=\\FileServer\Validation\Expected
```

**Windows (PowerShell):**
```powershell
$env:LEGACY_PROJECT_BASE="C:\Production\LegacyModernization"
$env:LEGACY_INPUT_PATH="\\FileServer\MonthlyData\Input"
$env:LEGACY_OUTPUT_PATH="\\FileServer\MonthlyData\Output"
$env:LEGACY_LOG_PATH="C:\Logs\Production"
$env:LEGACY_TESTDATA_PATH="C:\Production\TestData"
$env:LEGACY_EXPECTED_PATH="\\FileServer\Validation\Expected"
```

**Linux/Unix (bash):**
```bash
export LEGACY_PROJECT_BASE="/production/legacy-modernization"
export LEGACY_INPUT_PATH="/data/monthly-input"
export LEGACY_OUTPUT_PATH="/data/monthly-output"
export LEGACY_LOG_PATH="/var/logs/legacy-modernization"
export LEGACY_TESTDATA_PATH="/production/test-data"
export LEGACY_EXPECTED_PATH="/data/validation/expected"
```

## appsettings.json Configuration

Create an `appsettings.json` file in the **LegacyModernization.Core** project directory. This file will be automatically used by all projects (Pipeline, Validation, Tests) for centralized configuration management.

**Location**: `LegacyModernization.Core/appsettings.json`

```json
{
  "PipelineConfiguration": {
    "ProjectBase": "C:\\Production\\LegacyModernization",
    "InputPath": "\\\\FileServer\\MonthlyData\\Input",
    "OutputPath": "\\\\FileServer\\MonthlyData\\Output",
    "LogPath": "C:\\Logs\\Production",
    "TestDataPath": "C:\\Production\\TestData",
    "ExpectedOutputPath": "\\\\FileServer\\Validation\\Expected"
  }
}
```

**Note:** Use double backslashes (`\\\\`) in JSON for Windows paths to properly escape the backslashes.

### Centralized Configuration Benefits

- **Single Source of Truth**: All projects use the same configuration file
- **Easy Maintenance**: Update paths in one place for all components
- **Consistent Behavior**: Pipeline, Validation, and Tests use identical paths
- **Version Control**: Configuration changes are tracked with code changes

### appsettings.json Search Priority

The system searches for `appsettings.json` in the following order:
1. **LegacyModernization.Core project directory** (Highest Priority - Centralized)
2. Current working directory
3. Solution root directory

## Development Setup

For development environments, you can use relative paths:

**Environment Variables:**
```cmd
set LEGACY_PROJECT_BASE=C:\Dev\LegacyModernizationPoC
set LEGACY_INPUT_PATH=C:\Dev\LegacyModernizationPoC\TestData
set LEGACY_OUTPUT_PATH=C:\Dev\LegacyModernizationPoC\Output
set LEGACY_LOG_PATH=C:\Dev\LegacyModernizationPoC\Logs
set LEGACY_TESTDATA_PATH=C:\Dev\LegacyModernizationPoC\TestData
set LEGACY_EXPECTED_PATH=C:\Dev\LegacyModernizationPoC\ExpectedOutput
```

**appsettings.json:**
```json
{
  "PipelineConfiguration": {
    "ProjectBase": "C:\\Dev\\LegacyModernizationPoC",
    "InputPath": "C:\\Dev\\LegacyModernizationPoC\\TestData",
    "OutputPath": "C:\\Dev\\LegacyModernizationPoC\\Output",
    "LogPath": "C:\\Dev\\LegacyModernizationPoC\\Logs",
    "TestDataPath": "C:\\Dev\\LegacyModernizationPoC\\TestData",
    "ExpectedOutputPath": "C:\\Dev\\LegacyModernizationPoC\\ExpectedOutput"
  }
}
```

## Configuration Validation

The system automatically:
- Validates that all required configuration values are present
- Creates directories if they don't exist
- Provides detailed error messages with examples if configuration is missing
- Shows which configuration source is being used

## Best Practices

1. **Production**: Use environment variables for security and flexibility
2. **Staging**: Use appsettings.json or environment variables
3. **Development**: Use appsettings.json for convenience
4. **CI/CD**: Use environment variables for deployment flexibility

## Error Messages

If configuration is missing, you'll see helpful error messages:

```
Configuration Error:
No valid configuration found. Please either:

1. Set environment variables:
   Missing: LEGACY_PROJECT_BASE, LEGACY_INPUT_PATH, LEGACY_OUTPUT_PATH, LEGACY_LOG_PATH, LEGACY_TESTDATA_PATH, LEGACY_EXPECTED_PATH

2. Create appsettings.json with PipelineConfiguration section:
   Expected location: C:\Production\appsettings.json

3. Use CreateDefault() method for development with auto-generated paths

Environment Variable Examples:
[Detailed examples provided...]
```

## Migration from Hardcoded Paths

The new configuration system eliminates all hardcoded paths and provides a clean migration path:

- **Before**: Hardcoded paths in source code
- **After**: Flexible configuration through environment variables or appsettings.json
- **Benefit**: Portable, secure, and production-ready deployment
