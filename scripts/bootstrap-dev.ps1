#requires -Version 5.1
[CmdletBinding()]
param(
    # Explicit consent to SDK installation if needed; installation errors remain failures.
    [Alias('Y')]
    [switch]$Yes
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$stage = 'environment check'
$exitCode = 1

try {
    $check = & (Join-Path $PSScriptRoot 'check-dev-env.ps1') -PassThru
    if ($check.ExitCode -eq 3) {
        $stage = 'SDK installation prerequisites'
        $wingetCommand = Get-Command winget.exe -CommandType Application -ErrorAction SilentlyContinue | Select-Object -First 1
        if (-not $wingetCommand) {
            $exitCode = 3
            throw 'winget is unavailable. Install the Windows x64 .NET 10 SDK matching global.json manually, then rerun this script. No alternate installer was run.'
        }

        Write-Host 'The required x64 .NET 10 SDK is missing or unusable. Installation requires your consent.'
        Write-Host 'Command: winget install --id Microsoft.DotNet.SDK.10 -e --source winget'
        $approved = $Yes.IsPresent
        if (-not $approved) {
            $stage = 'SDK installation consent'
            $exitCode = 5
            $answer = Read-Host 'Install the .NET 10 SDK now? [y/N]'
            $approved = $answer -match '^(?i:y|yes)$'
        }
        if (-not $approved) {
            $exitCode = 5
            throw 'SDK installation was declined. No installer was run.'
        }

        $stage = 'SDK installation (winget)'
        $exitCode = 1
        & $wingetCommand.Source install --id Microsoft.DotNet.SDK.10 -e --source winget
        $installerExit = $LASTEXITCODE
        if ($installerExit -ne 0) {
            $exitCode = $installerExit
            throw "winget failed (exit $installerExit). Check its output; no alternate installer was run."
        }

        # Reprobe the standard install path without mutating PATH or restarting the shell.
        $stage = 'post-install environment check'
        $check = & (Join-Path $PSScriptRoot 'check-dev-env.ps1') -PassThru
    }

    if ($check.ExitCode -ne 0) {
        $exitCode = $check.ExitCode
        throw $check.Message
    }

    Push-Location -LiteralPath $repoRoot
    try {
        $solutionPath = Join-Path $repoRoot 'SchoolTimetableWidget.sln'
        foreach ($step in @(
            @{ Name = 'restore'; Arguments = @('restore', $solutionPath) },
            @{ Name = 'build'; Arguments = @('build', $solutionPath, '--no-restore') },
            @{ Name = 'test'; Arguments = @('test', $solutionPath, '--no-build') }
        )) {
            $stage = $step.Name
            $exitCode = 1
            Write-Host "[RUN] dotnet $stage"
            $stepArguments = $step.Arguments
            & $check.DotnetPath @stepArguments
            $commandExit = $LASTEXITCODE
            if ($commandExit -ne 0) {
                $exitCode = $commandExit
                throw "dotnet $stage failed (exit $commandExit)."
            }
        }
    }
    finally { Pop-Location }

    Write-Host '[OK] Restore, Debug build and test runner completed.'
    Write-Host '[INFO] Phase 0 has no product tests yet; a zero-test run is expected.'
    exit 0
}
catch {
    Write-Host "[FAIL] Stage: $stage; exit code: $exitCode"
    Write-Host $_.Exception.Message
    exit $exitCode
}
