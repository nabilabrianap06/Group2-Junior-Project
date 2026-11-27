# OxyGenAI first-time setup: asks for the Neon connection string and the (optional) Gemini API key,
# then writes oxygenai.settings.json next to OxyGenAI.exe. Run it again any time to change them.
# OxyGenAI runs on this PC; its database is a Neon (cloud PostgreSQL) database.
# Without questions (for tests or scripted installs):  setup.ps1 -ConnectionString "..." [-GeminiApiKey "..."] [-Port 5260]
param(
    [string]$ConnectionString,
    [string]$GeminiApiKey,
    [int]$Port = 0
)
$ErrorActionPreference = 'Stop'
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$target = Join-Path $here 'oxygenai.settings.json'

function Read-Secret([string]$prompt) {
    $secure = Read-Host $prompt -AsSecureString
    $ptr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($secure)
    try { return [Runtime.InteropServices.Marshal]::PtrToStringBSTR($ptr) }
    finally { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($ptr) }
}

# Quote values that contain ; = or quotes so the connection string stays valid.
function Format-ConnValue([string]$v) {
    if ($v -match '[;=''"]') { return "'" + $v.Replace("'", "''") + "'" } else { return $v }
}

# Accepts what the Neon console shows (postgresql://user:password@host/database?...) or the
# Host=...;Database=...;Username=...;Password=... form. Returns the form the app uses.
# The "-pooler" part of the host is removed: the app updates its tables at start-up and needs a direct connection.
function ConvertTo-NeonConnectionString([string]$text) {
    $text = "$text".Trim().Trim('"').Trim("'")
    if ($text -match '^psql\s+') { $text = ($text -replace '^psql\s+', '').Trim().Trim('"').Trim("'") }

    if ($text -match '^postgres(ql)?://') {
        $uri = [Uri]$text
        $parts = $uri.UserInfo.Split(':', 2)
        $database = $uri.AbsolutePath.Trim('/')
        if ($parts.Count -ne 2 -or -not $parts[1] -or -not $database) { throw 'The connection string needs a user, a password and a database name.' }
        $dbHost = $uri.Host
        $result = "Host=$($dbHost -replace '-pooler(?=\.)', '');Database=$database;" +
                  "Username=$(Format-ConnValue ([Uri]::UnescapeDataString($parts[0])));" +
                  "Password=$(Format-ConnValue ([Uri]::UnescapeDataString($parts[1])));SSL Mode=Require"
    }
    elseif ($text -match '(^|;)\s*Host\s*=\s*([^;]+)') {
        $dbHost = $Matches[2].Trim()
        if ($text -notmatch '(^|;)\s*Password\s*=') { throw 'The connection string has no Password=.' }
        $result = $text.Replace($dbHost, ($dbHost -replace '-pooler(?=\.)', '')).TrimEnd(';')
        if ($result -notmatch '(^|;)\s*SSL\s*Mode\s*=') { $result += ';SSL Mode=Require' }
    }
    else {
        throw 'This is not a connection string. Copy it from the Neon console (Connect button).'
    }

    if ($dbHost -notmatch '\.neon\.tech$') { throw "The database host '$dbHost' is not a Neon host (it must end in .neon.tech)." }
    return $result
}

$interactive = -not $ConnectionString

if ($interactive) {
    Write-Host ''
    Write-Host '=== OxyGenAI setup ===' -ForegroundColor Cyan
    Write-Host 'OxyGenAI keeps its data in a Neon database (https://neon.tech), so this PC needs internet access.'
    Write-Host 'In the Neon console open your project, press Connect and copy the connection string.'
    Write-Host 'Typing and pasting are hidden. Paste with a right-click or Ctrl+V, then press Enter.'
    Write-Host ''
}

while ($true) {
    if ($interactive) { $ConnectionString = Read-Secret 'Neon connection string' }
    try { $connection = ConvertTo-NeonConnectionString $ConnectionString; break }
    catch {
        if (-not $interactive) { throw }
        Write-Host $_.Exception.Message -ForegroundColor Yellow
    }
}

if ($interactive) {
    Write-Host ''
    Write-Host 'Gemini API key (optional, for Ask Gemini). Get one free at https://aistudio.google.com/apikey'
    $GeminiApiKey = Read-Secret 'Gemini API key (press Enter to skip)'
    $answer = Read-Host 'Port for the browser version [5260]'
    if ($answer -match '^\d+$') { $Port = [int]$answer }
}
if ($Port -le 0) { $Port = 5260 }

# The Gemini model is not written here: the app's own default (appsettings.json) applies.
$settings = [ordered]@{
    ConnectionStrings = [ordered]@{ DefaultConnection = $connection }
    Gemini = [ordered]@{ ApiKey = "$GeminiApiKey" }
    Kestrel = @{ Endpoints = @{ Http = @{ Url = "http://localhost:$Port" } } }
    OpenBrowser = $true
}
$json = $settings | ConvertTo-Json -Depth 5
[IO.File]::WriteAllText($target, $json, (New-Object Text.UTF8Encoding($false)))

Write-Host ''
Write-Host "Saved settings to $target" -ForegroundColor Green
Write-Host 'Now double-click "OxyGenAI-Desktop.exe" (or "Start OxyGenAI.bat" for the browser version).'
