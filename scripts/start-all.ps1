# 一键启动全部 API Demo（各开新窗口）
$Root = Split-Path $PSScriptRoot -Parent
$apis = Get-ChildItem $Root -Directory -Filter "Demo.*.Api" | Sort-Object Name
foreach ($dir in $apis) {
    $slnProj = Join-Path $dir.FullName ($dir.Name + ".csproj")
    Start-Process pwsh -ArgumentList @(
        "-NoExit",
        "-Command",
        "Set-Location '$($dir.FullName)'; Write-Host 'Starting $($dir.Name)' -ForegroundColor Cyan; dotnet run --project '$slnProj'"
    )
}
Write-Host "已启动 $($apis.Count) 个 API 窗口。"
