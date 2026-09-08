# 脚手架：创建全部独立 Demo 项目（各有自己的 .sln）
$ErrorActionPreference = "Stop"
$Root = Split-Path $PSScriptRoot -Parent
Set-Location $Root

$apis = @(
    @{ Name = "Demo.Rest.Api"; Port = 5101 },
    @{ Name = "Demo.Versioning.Api"; Port = 5102 },
    @{ Name = "Demo.ETag.Api"; Port = 5103 },
    @{ Name = "Demo.Idempotent.Api"; Port = 5104 },
    @{ Name = "Demo.Outbox.Api"; Port = 5105 },
    @{ Name = "Demo.Inbox.Api"; Port = 5106 },
    @{ Name = "Demo.MqConsumer.Api"; Port = 5107 },
    @{ Name = "Demo.Saga.Api"; Port = 5108 },
    @{ Name = "Demo.RateLimit.Api"; Port = 5109 },
    @{ Name = "Demo.Retry.Api"; Port = 5110 },
    @{ Name = "Demo.CircuitBreaker.Api"; Port = 5111 },
    @{ Name = "Demo.ShortUrl.Api"; Port = 5112 },
    @{ Name = "Demo.Redis.Api"; Port = 5113 },
    @{ Name = "Demo.CacheProblems.Api"; Port = 5114 },
    @{ Name = "Demo.Seckill.Api"; Port = 5115 },
    @{ Name = "Demo.PasswordAuth.Api"; Port = 5116 },
    @{ Name = "Demo.JwtAuth.Api"; Port = 5117 },
    @{ Name = "Demo.QrLogin.Api"; Port = 5118 },
    @{ Name = "Demo.Sso.Api"; Port = 5119 },
    @{ Name = "Demo.OptimisticLock.Api"; Port = 5120 },
    @{ Name = "Demo.DistributedLock.Api"; Port = 5121 },
    @{ Name = "Demo.OrderTimeout.Api"; Port = 5122 },
    @{ Name = "Demo.Payment.Api"; Port = 5123 },
    @{ Name = "Demo.DiLifetime.Api"; Port = 5124 }
)

$consoles = @(
    "Demo.AsyncAwait.Console",
    "Demo.Linq.Console",
    "Demo.Delegates.Console"
)

foreach ($api in $apis) {
    $name = $api.Name
    $port = $api.Port
    $dir = Join-Path $Root $name
    if (Test-Path $dir) {
        Write-Host "SKIP $name (exists)"
        continue
    }

    Write-Host "CREATE $name :$port"
    New-Item -ItemType Directory -Path $dir -Force | Out-Null
    Push-Location $dir
    try {
        dotnet new webapi -n $name -o . --use-controllers false --no-openapi false -f net9.0 | Out-Null
        dotnet new sln -n $name | Out-Null
        dotnet sln "$name.sln" add "$name.csproj" | Out-Null
        dotnet add package Scalar.AspNetCore --version 2.4.18 | Out-Null

        $launch = @"
{
  "`$schema": "https://json.schemastore.org/launchsettings.json",
  "profiles": {
    "http": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": true,
      "launchUrl": "scalar",
      "applicationUrl": "http://localhost:$port",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
"@
        New-Item -ItemType Directory -Path "Properties" -Force | Out-Null
        Set-Content -Path "Properties\launchSettings.json" -Value $launch -Encoding UTF8
    }
    finally {
        Pop-Location
    }
}

foreach ($name in $consoles) {
    $dir = Join-Path $Root $name
    if (Test-Path $dir) {
        Write-Host "SKIP $name (exists)"
        continue
    }
    Write-Host "CREATE $name"
    New-Item -ItemType Directory -Path $dir -Force | Out-Null
    Push-Location $dir
    try {
        dotnet new console -n $name -o . -f net9.0 | Out-Null
        dotnet new sln -n $name | Out-Null
        dotnet sln "$name.sln" add "$name.csproj" | Out-Null
    }
    finally {
        Pop-Location
    }
}

$algo = "Demo.Algorithms.Tests"
$algoDir = Join-Path $Root $algo
if (-not (Test-Path $algoDir)) {
    Write-Host "CREATE $algo"
    New-Item -ItemType Directory -Path $algoDir -Force | Out-Null
    Push-Location $algoDir
    try {
        dotnet new xunit -n $algo -o . -f net9.0 | Out-Null
        dotnet new sln -n $algo | Out-Null
        dotnet sln "$algo.sln" add "$algo.csproj" | Out-Null
    }
    finally {
        Pop-Location
    }
}
else {
    Write-Host "SKIP $algo (exists)"
}

Write-Host "DONE"
