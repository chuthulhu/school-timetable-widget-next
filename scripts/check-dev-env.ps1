#requires -Version 5.1
[CmdletBinding()]
param(
    # Structured result for bootstrap; diagnostics use the host stream.
    [switch]$PassThru
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$result = [pscustomobject]@{
    ExitCode = 1
    DotnetPath = $null
    SdkVersion = $null
    Message = 'Environment check did not complete.'
}

try {
    Push-Location -LiteralPath $repoRoot
    try {
        if ([Environment]::OSVersion.Platform -ne [PlatformID]::Win32NT -or
            [System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture.ToString() -ne 'X64') {
            $result.ExitCode = 2
            throw 'Windows x64 is required.'
        }
        Write-Host '[OK] Windows x64'

        $gitCommand = Get-Command git.exe -CommandType Application -ErrorAction SilentlyContinue | Select-Object -First 1
        if (-not $gitCommand) {
            $result.ExitCode = 2
            throw 'Git is missing. Install Git for Windows, then rerun bootstrap-dev.ps1.'
        }
        $gitVersion = & $gitCommand.Source --version
        if ($LASTEXITCODE -ne 0) {
            $result.ExitCode = 2
            throw "Git check failed (exit $LASTEXITCODE)."
        }
        Write-Host "[OK] $gitVersion"

        $result.ExitCode = 2
        $policy = Get-Content -LiteralPath (Join-Path $repoRoot 'global.json') -Raw | ConvertFrom-Json
        $minimumSdk = [version]$policy.sdk.version
        if ($minimumSdk.Major -ne 10 -or $minimumSdk.Minor -ne 0 -or
            $policy.sdk.rollForward -ne 'latestFeature' -or $policy.sdk.allowPrerelease -ne $false) {
            throw 'global.json must specify a stable .NET 10.0 SDK with latestFeature and allowPrerelease=false.'
        }
        Write-Host "[OK] global.json: $minimumSdk / latestFeature / stable only"

        $projects = @(
            'src/SchoolTimetableWidget.Core/SchoolTimetableWidget.Core.csproj'
            'src/SchoolTimetableWidget.Desktop/SchoolTimetableWidget.Desktop.csproj'
            'tests/SchoolTimetableWidget.Tests/SchoolTimetableWidget.Tests.csproj'
        )
        foreach ($relativePath in @('SchoolTimetableWidget.sln') + $projects) {
            if (-not (Test-Path -LiteralPath (Join-Path $repoRoot $relativePath) -PathType Leaf)) {
                throw "Repository file missing: $relativePath. Restore the checkout before bootstrapping."
            }
        }
        Write-Host '[OK] Solution and all three project files exist'

        # Also probe the standard x64 install when the current shell has a stale PATH.
        $candidates = @()
        $pathDotnet = Get-Command dotnet.exe -CommandType Application -ErrorAction SilentlyContinue | Select-Object -First 1
        if ($pathDotnet) { $candidates += $pathDotnet.Source }
        $programFiles64 = [Environment]::GetEnvironmentVariable('ProgramW6432')
        if (-not $programFiles64) { $programFiles64 = [Environment]::GetFolderPath('ProgramFiles') }
        $candidates += Join-Path $programFiles64 'dotnet/dotnet.exe'

        $result.ExitCode = 3
        foreach ($candidate in @($candidates | Select-Object -Unique)) {
            if (-not (Test-Path -LiteralPath $candidate -PathType Leaf)) { continue }
            try {
                $sdkOutput = @(& $candidate --version 2>&1)
                $sdkExit = $LASTEXITCODE
                $sdkText = ($sdkOutput -join "`n").Trim()
                if ($sdkExit -ne 0 -or $sdkText -notmatch '^10\.0\.\d+$' -or [version]$sdkText -lt $minimumSdk) {
                    Write-Host "[INFO] $candidate cannot select the required SDK (exit $sdkExit)."
                    continue
                }
                $infoOutput = @(& $candidate --info 2>&1)
                if ($LASTEXITCODE -ne 0 -or ($infoOutput -join "`n") -notmatch '\bwin-x64\b') {
                    Write-Host "[INFO] $candidate is not a usable Windows x64 SDK host."
                    continue
                }
                $result.DotnetPath = $candidate
                $result.SdkVersion = $sdkText
                break
            }
            catch {
                Write-Host "[INFO] Could not probe ${candidate}: $($_.Exception.Message)"
            }
        }
        if (-not $result.DotnetPath) {
            throw "A compatible x64 .NET SDK is missing or unusable. Required: $minimumSdk or later stable 10.0 feature band."
        }
        Write-Host "[OK] .NET SDK $($result.SdkVersion): $($result.DotnetPath)"

        $result.ExitCode = 4
        $templateOutput = @(& $result.DotnetPath new list wpf 2>&1)
        $templateExit = $LASTEXITCODE
        if ($templateExit -ne 0 -or ($templateOutput -join "`n") -notmatch '(?m)\s+wpf\s+') {
            throw "WPF application template is unavailable (CLI exit $templateExit). Repair the .NET 10 SDK installation."
        }
        Write-Host '[OK] WPF application template'

        $assetsPresent = $true
        foreach ($project in $projects) {
            $assetsPath = Join-Path (Split-Path -Parent (Join-Path $repoRoot $project)) 'obj/project.assets.json'
            if (-not (Test-Path -LiteralPath $assetsPath -PathType Leaf)) { $assetsPresent = $false }
        }
        Write-Host "[INFO] Restore assets present for all projects: $assetsPresent (presence does not prove freshness)."
        Write-Host '[INFO] Local prerequisites ready. NuGet access, restore, build and test execution are not checked here.'
        Write-Host '[INFO] Run scripts/bootstrap-dev.ps1 for restore -> build -> test.'
        $result.ExitCode = 0
        $result.Message = 'Local development prerequisites are ready.'
    }
    finally { Pop-Location }
}
catch {
    $result.Message = $_.Exception.Message
    Write-Host "[FAIL] $($result.Message)"
}

Write-Host "Environment check exit code: $($result.ExitCode)"
if ($PassThru) { $result }
exit $result.ExitCode
