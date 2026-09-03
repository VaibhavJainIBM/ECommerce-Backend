param()

$ErrorActionPreference = 'Stop'
$commerceRoot = Split-Path -Parent $PSScriptRoot
$commerceApi = Join-Path $commerceRoot 'src\ECommerce.Api'
$commerceConnection = $null

# Read only the database connection setting. Never print secrets or tokens.
foreach ($commerceFile in @('appsettings.json', 'appsettings.Development.json')) {
    $commercePath = Join-Path $commerceApi $commerceFile
    if (Test-Path -LiteralPath $commercePath) {
        $commerceSettings = Get-Content -LiteralPath $commercePath -Raw | ConvertFrom-Json
        if ($commerceSettings.ConnectionStrings.DefaultConnection) {
            $commerceConnection = $commerceSettings.ConnectionStrings.DefaultConnection
        }
    }
}

[xml]$commerceProject = Get-Content -LiteralPath (Join-Path $commerceApi 'ECommerce.Api.csproj') -Raw
$commerceSecretsId = @($commerceProject.Project.PropertyGroup.UserSecretsId) | Where-Object { $_ } | Select-Object -First 1
if ($commerceSecretsId) {
    $commerceSecretPath = Join-Path ([Environment]::GetFolderPath('ApplicationData')) "Microsoft\UserSecrets\$commerceSecretsId\secrets.json"
    if (Test-Path -LiteralPath $commerceSecretPath) {
        $commerceSecrets = Get-Content -LiteralPath $commerceSecretPath -Raw | ConvertFrom-Json
        if ($commerceSecrets.'ConnectionStrings:DefaultConnection') {
            $commerceConnection = $commerceSecrets.'ConnectionStrings:DefaultConnection'
        }
        elseif ($commerceSecrets.ConnectionStrings.DefaultConnection) {
            $commerceConnection = $commerceSecrets.ConnectionStrings.DefaultConnection
        }
    }
}
if ($env:ConnectionStrings__DefaultConnection) { $commerceConnection = $env:ConnectionStrings__DefaultConnection }
if ($env:ECOMMERCE_TEST_SQLSERVER) { $commerceConnection = $env:ECOMMERCE_TEST_SQLSERVER }
if (-not $commerceConnection) { throw 'Configure the API development SQL connection or ECOMMERCE_TEST_SQLSERVER first.' }

$commercePreviousTestConnection = $env:ECOMMERCE_TEST_SQLSERVER
try {
    $env:ECOMMERCE_TEST_SQLSERVER = $commerceConnection
    Write-Host 'Tests create and delete only a uniquely named ECommerceMvpTests_ database on the configured server.'
    & dotnet test (Join-Path $commerceRoot 'tests\ECommerce.UnitTests\ECommerce.UnitTests.csproj') -p:UseAppHost=false
    if ($LASTEXITCODE -ne 0) { throw 'Unit tests failed.' }
    & dotnet test (Join-Path $commerceRoot 'tests\ECommerce.IntegrationTests\ECommerce.IntegrationTests.csproj') -p:UseAppHost=false
    if ($LASTEXITCODE -ne 0) { throw 'Database/API tests failed.' }
}
finally {
    $env:ECOMMERCE_TEST_SQLSERVER = $commercePreviousTestConnection
    $commerceConnection = $null
    $commerceSecrets = $null
}
