# Builds the shareable package: publish\Saleling-v<version>.zip
#
#   .\build-package.ps1
#
# The version is read from <Version> in Saleling.UI\Saleling.UI.csproj.
# Needs the .NET 8 SDK and Visual Studio (with the Data storage and processing workload) to build the database.

$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot

$project = 'Saleling.UI\Saleling.UI.csproj'
$version = ([xml](Get-Content $project)).Project.PropertyGroup.Version | Where-Object { $_ } | Select-Object -First 1
if (-not $version) { throw "No <Version> found in $project." }

$name = "Saleling-v$version"
$folder = "publish\$name"
$zip = "publish\$name.zip"

function Invoke-Step([string]$Description, [scriptblock]$Command) {
    Write-Host "`n== $Description" -ForegroundColor Cyan
    & $Command
    if ($LASTEXITCODE -ne 0) { throw "$Description failed (exit code $LASTEXITCODE)." }
}

Write-Host "Building $name"

# Start clean so the package never contains files from an older build.
if (Test-Path publish) { Remove-Item publish -Recurse -Force }
New-Item -ItemType Directory -Force "$folder\Database" | Out-Null

Invoke-Step 'Publish the app as a single exe' {
    dotnet publish Saleling.UI -c Release -r win-x64 --self-contained true `
        -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish\_build
}

# Saleling.DB is an SSDT project, so it needs Visual Studio's MSBuild instead of dotnet.
$vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
$msbuild = & $vswhere -latest -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1
if (-not $msbuild) { throw 'Visual Studio MSBuild was not found. Install Visual Studio with the Data storage and processing workload.' }

Invoke-Step 'Build the database package (.dacpac)' {
    & $msbuild Saleling.DB\Saleling.DB.sqlproj /p:Configuration=Release /v:minimal /nologo
}

Copy-Item publish\_build\Saleling.UI.exe, publish\_build\Saleling.UI.dll.config $folder
Copy-Item Saleling.DB\bin\Release\Saleling.DB.dacpac "$folder\Database"
Copy-Item packaging\SETUP.txt $folder
Remove-Item publish\_build -Recurse -Force

Compress-Archive -Path "$folder\*" -DestinationPath $zip -Force

Write-Host "`nDone: $zip" -ForegroundColor Green
