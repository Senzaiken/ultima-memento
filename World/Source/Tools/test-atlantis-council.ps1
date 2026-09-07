$ErrorActionPreference = 'Stop'
$worldRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\..')).Path
$testBuildDirectory = Join-Path $PSScriptRoot 'bin\AtlantisCouncilTests'
[System.IO.Directory]::CreateDirectory($testBuildDirectory) | Out-Null
$compilerPath = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$coreAssembly = Join-Path $worldRoot 'World.exe'
$scriptsAssembly = Join-Path $testBuildDirectory 'AtlantisAllScripts.dll'
$responsePath = Join-Path $testBuildDirectory 'scripts.rsp'
$compilerArguments = [System.Collections.Generic.List[string]]::new()
$compilerArguments.Add('/nologo')
$compilerArguments.Add('/target:library')
$compilerArguments.Add('/optimize')
$compilerArguments.Add('/out:"' + $scriptsAssembly + '"')
$compilerArguments.Add('/reference:"' + $coreAssembly + '"')
foreach ($assemblyName in Get-Content -LiteralPath (Join-Path $worldRoot 'Data\System\CFG\Assemblies.cfg')) {
    if ($assemblyName -and !$assemblyName.StartsWith('#')) {
        $compilerArguments.Add('/reference:' + $assemblyName)
    }
}
foreach ($sourceDirectory in @('Source\Scripts', 'Info\Scripts')) {
    foreach ($sourceFile in [System.IO.Directory]::GetFiles((Join-Path $worldRoot $sourceDirectory), '*.cs', [System.IO.SearchOption]::AllDirectories)) {
        $compilerArguments.Add('"' + $sourceFile + '"')
    }
}
[System.IO.File]::WriteAllLines($responsePath, $compilerArguments)
& $compilerPath ('@' + $responsePath)
if ($LASTEXITCODE -ne 0) { throw 'Server script compilation failed.' }
$testExecutable = Join-Path $testBuildDirectory 'AtlantisCouncilTests.exe'
& $compilerPath /nologo /target:exe ('/out:' + $testExecutable) ('/reference:' + $scriptsAssembly) ('/reference:' + $coreAssembly) (Join-Path $PSScriptRoot 'AtlantisCouncilTests.cs')
if ($LASTEXITCODE -ne 0) { throw 'Test compilation failed.' }
& $testExecutable $worldRoot
if ($LASTEXITCODE -ne 0) { throw 'Atlantis council regression checks failed.' }
