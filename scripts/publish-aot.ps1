[CmdletBinding()]
param(
    [string]$RuntimeIdentifier = 'win-x64',
    [string]$Configuration = 'Release',
    [string]$OutputPath
)

$ErrorActionPreference = 'Stop'

# NativeAOT discovers Visual Studio through vswhere and initializes the selected
# C++ toolchain itself. Variables inherited from another Visual Studio instance
# can prevent vcvarsall from adding link.exe and the correct x64 libraries.
$visualStudioVariablePattern = '^(VS|VC|VisualStudio|DevEnvDir|CommandPromptType|FrameworkDir|FrameworkVersion|WindowsSDK|WindowsSdk|UniversalCRT|UCRTVersion|INCLUDE|LIB|LIBPATH|EXTERNAL_INCLUDE|Platform|ExtensionSdkDir|NETFXSDKDir|VSCMD|ServiceHub)'
$visualStudioVariables = Get-ChildItem Env: | Where-Object {
    $_.Name -match $visualStudioVariablePattern
}

foreach ($variable in $visualStudioVariables) {
    Remove-Item -LiteralPath ('Env:' + $variable.Name)
}

$env:Path = (($env:Path -split ';') | Where-Object {
    $_ -notmatch '\\Microsoft Visual Studio\\'
}) -join ';'

$projectPath = Join-Path $PSScriptRoot '..\PngMetadataReaderUI'
$publishArguments = @(
    'publish'
    $projectPath
    '-c'
    $Configuration
    '-r'
    $RuntimeIdentifier
)

if (-not [string]::IsNullOrWhiteSpace($OutputPath)) {
    $publishArguments += @('-o', $OutputPath)
}

& dotnet @publishArguments
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}
