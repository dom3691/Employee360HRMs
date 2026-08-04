# Seed demo data for Employee360 HRMS (Batch 17).
# Applies EF migrations then runs a one-shot demo seed.
#
# Usage:
#   .\scripts\seed-demo-data.ps1
#   .\scripts\seed-demo-data.ps1 -ConnectionString "Server=..."

param(
    [string]$ConnectionString = "",
    [string]$DefaultPassword = "Demo@12345",
    [int]$HolidayYear = 2026
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
Set-Location $repoRoot

Write-Host "Applying database migrations..."
$migrationArgs = @(
    "ef", "database", "update",
    "--project", "src\Employee360.Infrastructure",
    "--startup-project", "src\Employee360.API"
)
if ($ConnectionString) {
    $env:ConnectionStrings__DefaultConnection = $ConnectionString
}
dotnet @migrationArgs

Write-Host "Running demo data seed..."
$env:Database__SeedOnStartup = "false"
$env:DemoData__Enabled = "true"
$env:DemoData__DefaultPassword = $DefaultPassword
$env:DemoData__HolidayYear = $HolidayYear
$env:Hangfire__Enabled = "false"

dotnet run --project src\Employee360.API -- --seed-demo

Write-Host "Demo seed complete."
Write-Host "Sample logins (password: $DefaultPassword):"
Write-Host "  admin@employee360.demo       (SystemAdmin)"
Write-Host "  hr.admin@employee360.demo    (HRAdmin)"
Write-Host "  manager@employee360.demo     (LineManager)"
Write-Host "  employee1@employee360.demo   (Employee)"
Write-Host "  employee2@employee360.demo   (Employee)"
