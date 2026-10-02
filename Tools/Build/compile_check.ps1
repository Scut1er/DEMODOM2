# Компиляция скриптов без Unity (для проверки кода агентами и в CI).
# Берёт сгенерированные Unity csproj, заменяет списки файлов на маски Assets/Scripts/**, собирает через dotnet.
# Запуск из корня проекта:  powershell -ExecutionPolicy Bypass -File Tools/Build/compile_check.ps1
param([switch]$RuntimeOnly)

$ErrorActionPreference = 'Continue'
$root = Resolve-Path (Join-Path $PSScriptRoot '..\..')
Set-Location $root

function Make-Check([string]$source, [string]$target, [string]$glob, [string]$exclude, [hashtable]$refs) {
    $xml = Get-Content $source -Raw
    # убрать все Compile Include из Assets (новые файлы Unity ещё не прописал) и вставить маску
    $xml = [regex]::Replace($xml, '\s*<Compile Include="Assets[^"]*"\s*/>', '')
    $item = "  <ItemGroup>`n    <Compile Include=""$glob"" Exclude=""$exclude"" />`n  </ItemGroup>`n  <Import"
    $xml = $xml.Replace('  <Import Project="$(MSBuildToolsPath)\Microsoft.CSharp.targets" />', $item + ' Project="$(MSBuildToolsPath)\Microsoft.CSharp.targets" />')
    $name = [IO.Path]::GetFileNameWithoutExtension($target)
    $xml = $xml.Replace('<OutputPath>Temp\bin\Debug\</OutputPath>', "<OutputPath>Temp\cc\bin\</OutputPath><BaseIntermediateOutputPath>Temp\cc\obj\$name\</BaseIntermediateOutputPath><IntermediateOutputPath>Temp\cc\obj\$name\</IntermediateOutputPath>")
    foreach ($k in $refs.Keys) { $xml = $xml.Replace($k, $refs[$k]) }
    Set-Content -Path $target -Value $xml -Encoding UTF8
}

function Build([string]$project) {
    $out = & dotnet build $project @common 2>&1
    $failed = $LASTEXITCODE -ne 0
    $errors = $out | Select-String -Pattern 'error (CS|MSB)\d+'
    if ($errors) { $failed = $true }
    if ($failed) {
        if ($errors) { $errors | ForEach-Object { $_.Line.Trim() } | Sort-Object -Unique | ForEach-Object { Write-Host $_ } }
        else { $out | Select-Object -Last 20 | ForEach-Object { Write-Host $_ } }
    }
    return -not $failed
}

# Референсные сборки .NET 4.7.1 берём из Mono внутри Unity (Developer Pack ставить не нужно).
$engine = (Select-String -Path 'Assembly-CSharp.csproj' -Pattern '<HintPath>(.*)\\Managed\\UnityEngine\\UnityEngine\.dll</HintPath>').Matches[0].Groups[1].Value
$framework = Join-Path $engine 'MonoBleedingEdge\lib\mono\4.7.1-api'
$common = @('-nologo', '-v', 'q', '-clp:ErrorsOnly', "-p:FrameworkPathOverride=$framework")

Make-Check 'Assembly-CSharp.csproj' '_cc_Runtime.csproj' 'Assets\Scripts\**\*.cs' 'Assets\Scripts\**\Editor\**' @{}
$ok = Build '_cc_Runtime.csproj'

if ($ok -and -not $RuntimeOnly) {
    Make-Check 'Assembly-CSharp-Editor.csproj' '_cc_Editor.csproj' 'Assets\Scripts\**\Editor\**\*.cs' '' @{ 'Include="Assembly-CSharp.csproj"' = 'Include="_cc_Runtime.csproj"' }
    $ok = Build '_cc_Editor.csproj'
}

if ($ok) { Write-Output 'COMPILE OK' } else { Write-Output 'COMPILE FAILED'; exit 1 }
