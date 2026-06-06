$ErrorActionPreference = "Stop"
$env:DOTNET_CLI_TELEMETRY_OPTOUT = "1"
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = "1"
$env:DOTNET_CLI_HOME = "$PWD\.dotnet"
$env:APPDATA = "$PWD\.appdata"
$env:LOCALAPPDATA = "$PWD\.localappdata"
$env:NUGET_PACKAGES = "$PWD\.nuget\packages"
$env:NUGET_HTTP_CACHE_PATH = "$PWD\.nuget\http-cache"
$version = if ($env:WINDOWS_MONITOR_VERSION) { $env:WINDOWS_MONITOR_VERSION } else { "1.0.0" }
.\Tools\Generate-AppIcon.ps1
if (Test-Path ".\dist") {
    Get-ChildItem ".\dist" -Force | Remove-Item -Recurse -Force
}
if (Test-Path ".\release") {
    Get-ChildItem ".\release" -Force | Remove-Item -Recurse -Force
}
New-Item -ItemType Directory -Force -Path ".\dist", ".\release" | Out-Null

dotnet publish ".\windows-monitor.csproj" -c Release -r win-x64 --self-contained false -o ".\dist" -p:RestoreConfigFile="$PWD\NuGet.Config" -p:Version="$version"
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

Compress-Archive -Path ".\dist\*" -DestinationPath ".\release\windows-monitor.zip" -Force

$env:GOCACHE = "$PWD\.gocache"
$env:GO111MODULE = "off"
go build -trimpath -ldflags "-H=windowsgui -s -w -X main.version=$version" -o ".\release\windows-monitor-setup.exe" ".\cmd\windows-monitor-setup"
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

Write-Host "Generated .\release\windows-monitor.zip"
Write-Host "Generated .\release\windows-monitor-setup.exe"
