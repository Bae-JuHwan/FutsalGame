param(
    [string]$UnityEditor = 'C:\Program Files\Unity\Hub\Editor\6000.3.23f1\Editor\Unity.exe'
)
$ErrorActionPreference = 'Stop'
$sourceProject = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$checkProject = Join-Path ([IO.Path]::GetTempPath()) ('FutsalDribbleChecks-' + [guid]::NewGuid().ToString('N'))
foreach ($folder in @('Assets\Editor', 'Packages', 'ProjectSettings')) {
    New-Item -ItemType Directory -Path (Join-Path $checkProject $folder) -Force | Out-Null
}
Get-ChildItem -LiteralPath (Join-Path $sourceProject 'Assets') -Filter '*.cs' | ForEach-Object {
    Copy-Item -LiteralPath $_.FullName -Destination (Join-Path $checkProject 'Assets')
}
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'DribblePhysicsChecks.cs') -Destination (Join-Path $checkProject 'Assets\Editor')
Copy-Item -LiteralPath (Join-Path $sourceProject 'ProjectSettings\ProjectVersion.txt') -Destination (Join-Path $checkProject 'ProjectSettings')
$sourceManifest = Get-Content -LiteralPath (Join-Path $sourceProject 'Packages\manifest.json') -Raw | ConvertFrom-Json
$checkDependencies = @{}
foreach ($entry in $sourceManifest.dependencies.PSObject.Properties) {
    if ($entry.Name.StartsWith('com.unity.modules.')) { $checkDependencies[$entry.Name] = $entry.Value }
}
foreach ($packageName in @('com.unity.inputsystem', 'com.unity.ugui')) {
    $package = Get-ChildItem -LiteralPath (Join-Path $sourceProject 'Library\PackageCache') -Directory -Filter "$packageName@*" | Select-Object -First 1
    if ($null -eq $package) { throw "Open the game in Unity first to resolve $packageName." }
    $checkDependencies[$packageName] = 'file:' + $package.FullName.Replace('\', '/')
}
@{ dependencies = $checkDependencies } | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $checkProject 'Packages\manifest.json') -Encoding UTF8
$checkLog = Join-Path $checkProject 'validation.log'
$arguments = "-batchmode -nographics -projectPath `"$checkProject`" -executeMethod DribblePhysicsChecks.Run -logFile `"$checkLog`""
$process = Start-Process -FilePath $UnityEditor -ArgumentList $arguments -WindowStyle Hidden -Wait -PassThru
Select-String -LiteralPath $checkLog -Pattern 'DRIBBLE |DRIBBLE_CHECKS|error CS|Exception:'
Write-Output "Validation artifacts: $checkProject"
if ($process.ExitCode -ne 0 -or !(Select-String -LiteralPath $checkLog -SimpleMatch 'DRIBBLE_CHECKS_PASS' -Quiet)) {
    throw "Dribble validation failed. See $checkLog"
}
