# OxyGenAI first-time setup: asks for the PostgreSQL login and the (optional) Gemini API key,
# then writes oxygenai.settings.json next to OxyGenAI.exe. Run it again any time to change them.
$ErrorActionPreference = 'Stop'
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$target = Join-Path $here 'oxygenai.settings.json'

function Read-Default([string]$prompt, [string]$default) {
    $value = Read-Host "$prompt [$default]"
    if ([string]::IsNullOrWhiteSpace($value)) { return $default } else { return $value.Trim() }
}

function Read-Secret([string]$prompt) {
    $secure = Read-Host $prompt -AsSecureString
    $ptr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($secure)
    try { return [Runtime.InteropServices.Marshal]::PtrToStringBSTR($ptr) }
    finally { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($ptr) }
}

Write-Host ''
Write-Host '=== OxyGenAI setup ===' -ForegroundColor Cyan
Write-Host 'PostgreSQL must be installed and running. The database is created automatically on first start.'
Write-Host ''

$pgHost = Read-Default 'PostgreSQL host' 'localhost'
$pgPort = Read-Default 'PostgreSQL port' '5432'
$pgDb   = Read-Default 'Database name' 'ai_environmental_db'
$pgUser = Read-Default 'PostgreSQL user' 'postgres'
$pgPass = Read-Secret 'PostgreSQL password'
Write-Host ''
Write-Host 'Gemini API key (optional, for the Ask Gemini page). Get one free at https://aistudio.google.com/apikey'
$gemini = Read-Secret 'Gemini API key (press Enter to skip)'
$port   = Read-Default 'Port the app listens on' '5260'

# Quote values that contain ; or = so the connection string stays valid.
function Format-ConnValue([string]$v) {
    if ($v -match '[;=''"]') { return "'" + $v.Replace("'", "''") + "'" } else { return $v }
}

$settings = [ordered]@{
    ConnectionStrings = [ordered]@{
        DefaultConnection = "Host=$pgHost;Port=$pgPort;Database=$pgDb;Username=$(Format-ConnValue $pgUser);Password=$(Format-ConnValue $pgPass)"
    }
    Gemini = [ordered]@{ ApiKey = $gemini; Model = 'gemini-2.5-flash' }
    Kestrel = @{ Endpoints = @{ Http = @{ Url = "http://localhost:$port" } } }
    OpenBrowser = $true
}
$json = $settings | ConvertTo-Json -Depth 5
[IO.File]::WriteAllText($target, $json, (New-Object Text.UTF8Encoding($false)))

Write-Host ''
Write-Host "Saved settings to $target" -ForegroundColor Green
Write-Host 'Now double-click "Start OxyGenAI.bat".'
