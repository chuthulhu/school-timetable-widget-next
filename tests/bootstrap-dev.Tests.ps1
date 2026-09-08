#requires -Version 5.1
# Isolated bootstrap control-flow checks. No real installer or SDK process is invoked.
[CmdletBinding()]
param([string]$Repository)
if (-not $Repository) { $Repository = Split-Path -Parent $PSScriptRoot }
$ErrorActionPreference = 'Stop'
$fixtureRoot = Join-Path ([IO.Path]::GetTempPath()) ('timetable-bootstrap-test-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory (Join-Path $fixtureRoot 'scripts') -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $Repository 'scripts/check-dev-env.ps1'),(Join-Path $Repository 'scripts/bootstrap-dev.ps1') -Destination (Join-Path $fixtureRoot 'scripts')
foreach ($relative in @('global.json','SchoolTimetableWidget.sln','src/SchoolTimetableWidget.Core/SchoolTimetableWidget.Core.csproj','src/SchoolTimetableWidget.Desktop/SchoolTimetableWidget.Desktop.csproj','tests/SchoolTimetableWidget.Tests/SchoolTimetableWidget.Tests.csproj')) {
    $target = Join-Path $fixtureRoot $relative
    New-Item -ItemType Directory (Split-Path $target -Parent) -Force | Out-Null
    Copy-Item -LiteralPath (Join-Path $Repository $relative) -Destination $target
}
$gitStub = Join-Path $fixtureRoot 'git-stub.ps1'
$dotnetStub = Join-Path $fixtureRoot 'dotnet-stub.ps1'
$wingetStub = Join-Path $fixtureRoot 'winget-stub.ps1'
Set-Content $gitStub "'git version probe'; exit 0"
Set-Content $dotnetStub @'
$global:Calls.Add('dotnet ' + ($args -join ' '))
switch ($args[0]) {
    '--version' {
        if ($global:Scenario -eq 'sdk11') { '11.0.100' }
        elseif ($global:Scenario -eq 'sdk-old') { '10.0.300' }
        elseif ($global:Scenario -eq 'sdk-preview') { '10.0.500-preview.1' }
        else { '10.0.400' }
    }
    '--info' { 'RID: win-x64' }
    'new' { if ($global:Scenario -ne 'no-wpf') { 'WPF Application wpf [C#],VB Common/WPF' } }
    default { if ($global:Scenario -eq ($args[0] + '-failure')) { exit 17 } }
}
exit 0
'@
Set-Content $wingetStub @'
$global:Calls.Add('winget ' + ($args -join ' '))
if ($global:Scenario -eq 'installer-failure') { exit 23 }
if ($global:Scenario -ne 'install-no-sdk') { $global:SdkInstalled = $true }
exit 0
'@
function Get-Command {
    param($Name, $CommandType, $ErrorAction)
    switch ($Name) {
        'git.exe' { if ($global:Scenario -ne 'no-git') { [pscustomobject]@{ Source = $gitStub } } }
        'dotnet.exe' { if ($global:SdkInstalled) { [pscustomobject]@{ Source = $dotnetStub } } }
        'winget.exe' { if ($global:Scenario -ne 'no-winget') { [pscustomobject]@{ Source = $wingetStub } } }
        default { throw "Unexpected command lookup: $Name" }
    }
}
function Test-Path {
    param($LiteralPath, $PathType)
    if ($LiteralPath -match '[\\/]dotnet[\\/]dotnet.exe$') { return $false }
    Microsoft.PowerShell.Management\Test-Path -LiteralPath $LiteralPath -PathType $PathType
}
function Read-Host {
    param($Prompt)
    $global:Calls.Add('prompt')
    if ($global:Scenario -eq 'consent-input-failure') { throw 'Input unavailable' }
    if ($global:Scenario -eq 'consent-yes') { return 'y' }
    return ''
}
$cases = @(
    @{ Name = 'ready'; Exit = 0; Sdk = $true; Install = 0; Prompt = 0; Steps = 'restore,build,test' },
    @{ Name = 'no-git'; Exit = 2; Sdk = $false; Install = 0; Prompt = 0; Steps = '' },
    @{ Name = 'bad-policy'; Exit = 2; Sdk = $true; Install = 0; Prompt = 0; Steps = '' },
    @{ Name = 'no-winget'; Exit = 3; Sdk = $false; Install = 0; Prompt = 0; Steps = '' },
    @{ Name = 'declined'; Exit = 5; Sdk = $false; Install = 0; Prompt = 1; Steps = '' },
    @{ Name = 'consent-input-failure'; Exit = 5; Sdk = $false; Install = 0; Prompt = 1; Steps = '' },
    @{ Name = 'consent-yes'; Exit = 0; Sdk = $false; Install = 1; Prompt = 1; Steps = 'restore,build,test' },
    @{ Name = 'yes-switch'; Exit = 0; Sdk = $false; Install = 1; Prompt = 0; Steps = 'restore,build,test'; Yes = $true },
    @{ Name = 'y-alias'; Exit = 0; Sdk = $false; Install = 1; Prompt = 0; Steps = 'restore,build,test'; Y = $true },
    @{ Name = 'installer-failure'; Exit = 23; Sdk = $false; Install = 1; Prompt = 0; Steps = ''; Yes = $true },
    @{ Name = 'install-no-sdk'; Exit = 3; Sdk = $false; Install = 1; Prompt = 0; Steps = ''; Yes = $true },
    @{ Name = 'sdk11'; Exit = 5; Sdk = $true; Install = 0; Prompt = 1; Steps = '' },
    @{ Name = 'sdk-old'; Exit = 5; Sdk = $true; Install = 0; Prompt = 1; Steps = '' },
    @{ Name = 'sdk-preview'; Exit = 5; Sdk = $true; Install = 0; Prompt = 1; Steps = '' },
    @{ Name = 'no-wpf'; Exit = 4; Sdk = $true; Install = 0; Prompt = 0; Steps = '' },
    @{ Name = 'restore-failure'; Exit = 17; Sdk = $true; Install = 0; Prompt = 0; Steps = 'restore' },
    @{ Name = 'build-failure'; Exit = 17; Sdk = $true; Install = 0; Prompt = 0; Steps = 'restore,build' },
    @{ Name = 'test-failure'; Exit = 17; Sdk = $true; Install = 0; Prompt = 0; Steps = 'restore,build,test' }
)
$originalLocation = (Get-Location).Path
foreach ($case in $cases) {
    $global:Scenario = $case.Name
    $global:Calls = New-Object 'System.Collections.Generic.List[string]'
    $global:SdkInstalled = $case.Sdk
    Copy-Item -LiteralPath (Join-Path $Repository 'global.json') -Destination (Join-Path $fixtureRoot 'global.json') -Force
    if ($case.Name -eq 'bad-policy') { Set-Content (Join-Path $fixtureRoot 'global.json') '{}' }
    $parameters = @{}
    if ($case.Yes) { $parameters.Yes = $true }
    if ($case.Y) { $parameters.Y = $true }
    $output = & (Join-Path $fixtureRoot 'scripts/bootstrap-dev.ps1') @parameters 6>&1
    $actualExit = $LASTEXITCODE
    $installs = @($global:Calls | Where-Object { $_ -like 'winget *' })
    $prompts = @($global:Calls | Where-Object { $_ -eq 'prompt' })
    $steps = @($global:Calls | Where-Object { $_ -match '^dotnet (restore|build|test) ' } | ForEach-Object { ($_ -split ' ')[1] }) -join ','
    if ($actualExit -ne $case.Exit -or $installs.Count -ne $case.Install -or $prompts.Count -ne $case.Prompt -or $steps -ne $case.Steps -or (Get-Location).Path -ne $originalLocation) {
        $output | Out-Host
        throw "Case $($case.Name): exit=$actualExit installs=$($installs.Count) prompts=$($prompts.Count) steps=$steps"
    }
    if ($installs.Count -gt 0 -and $installs[0] -ne 'winget install --id Microsoft.DotNet.SDK.10 -e --source winget') { throw 'Unexpected installation command' }
    Write-Host "PASS $($case.Name) (exit $actualExit)"
}
Write-Host "PASS $($cases.Count) isolated bootstrap cases; no real installer or SDK command was invoked."
Write-Host "Diagnostic fixture: $fixtureRoot"
