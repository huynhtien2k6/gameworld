param(
    [ValidateRange(1024, 65535)]
    [int]$Port = 5287
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Net.Http
$repoPath = Split-Path -Parent $PSScriptRoot
$runPath = Join-Path $repoPath ('artifacts/verify-runtime/' + [Guid]::NewGuid().ToString('N'))
$databasePath = Join-Path $runPath 'verify.db'
$apiProject = Join-Path $repoPath 'GameWorld.Api/GameWorld.Api.csproj'
$apiRoot = Join-Path $repoPath 'GameWorld.Api'
New-Item -ItemType Directory -Path $runPath -Force | Out-Null
$buildLog = & dotnet build $apiProject --no-restore --nologo --output $runPath 2>&1
if ($LASTEXITCODE -ne 0) { throw ('Verification build failed: ' + (($buildLog | Select-Object -Last 12) -join [Environment]::NewLine)) }

$randomBytes = New-Object byte[] 64
$rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
$rng.GetBytes($randomBytes)
$jwtKey = [Convert]::ToBase64String($randomBytes)
$rng.GetBytes($randomBytes)
$adminPassword = [Convert]::ToBase64String($randomBytes)
$rng.GetBytes($randomBytes)
$playerPassword = [Convert]::ToBase64String($randomBytes)
$rng.Dispose()
$adminUsername = 'verify_admin'
$baseUrl = 'http://127.0.0.1:' + $Port
$client = New-Object System.Net.Http.HttpClient
$client.Timeout = [TimeSpan]::FromSeconds(15)
$script:serverProcess = $null
$script:stdoutTask = $null
$script:stderrTask = $null
$script:checks = 0

function Start-VerifyServer {
    $startInfo = New-Object System.Diagnostics.ProcessStartInfo
    $startInfo.FileName = 'dotnet'
    $startInfo.Arguments = '"' + (Join-Path $runPath 'GameWorld.Api.dll') + '" --urls ' + $baseUrl
    $startInfo.WorkingDirectory = $apiRoot
    $startInfo.UseShellExecute = $false
    $startInfo.CreateNoWindow = $true
    $startInfo.RedirectStandardOutput = $true
    $startInfo.RedirectStandardError = $true
    # Credentials exist only in the child process environment and this script's memory.
    $startInfo.EnvironmentVariables['ASPNETCORE_ENVIRONMENT'] = 'Development'
    $startInfo.EnvironmentVariables['Database__Provider'] = 'Sqlite'
    $startInfo.EnvironmentVariables['Database__Initialize'] = 'true'
    $startInfo.EnvironmentVariables['ConnectionStrings__DefaultConnection'] = 'Data Source=' + $databasePath
    $startInfo.EnvironmentVariables['JwtSettings__Key'] = $jwtKey
    $startInfo.EnvironmentVariables['BootstrapAdmin__Username'] = $adminUsername
    $startInfo.EnvironmentVariables['BootstrapAdmin__Password'] = $adminPassword
    $startInfo.EnvironmentVariables['Logging__LogLevel__Default'] = 'Warning'
    $startInfo.EnvironmentVariables['Logging__LogLevel__Microsoft'] = 'Warning'
    $script:serverProcess = [System.Diagnostics.Process]::Start($startInfo)
    $script:stdoutTask = $script:serverProcess.StandardOutput.ReadToEndAsync()
    $script:stderrTask = $script:serverProcess.StandardError.ReadToEndAsync()
    $ready = $false
    for ($attempt = 0; $attempt -lt 80; $attempt++) {
        if ($script:serverProcess.HasExited) {
            $failure = $script:stderrTask.GetAwaiter().GetResult()
            if ([string]::IsNullOrWhiteSpace($failure)) { $failure = $script:stdoutTask.GetAwaiter().GetResult() }
            throw ('API exited during verification startup: ' + $failure)
        }
        try {
            $health = $client.GetAsync($baseUrl + '/health').GetAwaiter().GetResult()
            try { if ([int]$health.StatusCode -eq 200) { $ready = $true; break } }
            finally { $health.Dispose() }
        }
        catch { }
        Start-Sleep -Milliseconds 250
    }
    if (!$ready) { throw 'Verification API did not become healthy within 20 seconds.' }
}

function Stop-VerifyServer {
    if ($null -ne $script:serverProcess) {
        if (!$script:serverProcess.HasExited) { $script:serverProcess.Kill(); $null = $script:serverProcess.WaitForExit(5000) }
        $script:serverProcess.Dispose()
        $script:serverProcess = $null
    }
}

function Request-Api([string]$Method, [string]$Path, $Body = $null, [string]$Token = '', [int]$Expected = 200) {
    $request = New-Object System.Net.Http.HttpRequestMessage([System.Net.Http.HttpMethod]::new($Method), ($baseUrl + $Path))
    if ($Token) { $request.Headers.Authorization = New-Object System.Net.Http.Headers.AuthenticationHeaderValue('Bearer', $Token) }
    if ($null -ne $Body) {
        $request.Content = New-Object System.Net.Http.StringContent(($Body | ConvertTo-Json -Depth 12 -Compress), [Text.Encoding]::UTF8, 'application/json')
    }
    try {
        $response = $client.SendAsync($request).GetAwaiter().GetResult()
        try {
            $raw = $response.Content.ReadAsStringAsync().GetAwaiter().GetResult()
            $status = [int]$response.StatusCode
            if ($status -ne $Expected) { throw ('Expected HTTP ' + $Expected + ' for ' + $Method + ' ' + $Path + ', received ' + $status + '.') }
            $script:checks++
            if ([string]::IsNullOrWhiteSpace($raw)) { return $null }
            $result = ConvertFrom-Json -InputObject $raw
            if ($result -is [System.Array]) {
                foreach ($item in $result) { Write-Output $item }
                return
            }
            return $result
        }
        finally { $response.Dispose() }
    }
    finally { $request.Dispose() }
}

function Check([bool]$Condition, [string]$Description) {
    if (!$Condition) { throw ('Verification failed: ' + $Description) }
    $script:checks++
}

try {
    Start-VerifyServer
    $null = Request-Api GET '/api/admin/overview' $null '' 401
    $null = Request-Api POST '/api/auth/register' @{ username = 'short'; email = 'short@example.test'; password = '123' } '' 400
    $alice = Request-Api POST '/api/auth/register' @{ username = 'verify_alice'; email = 'alice@example.test'; password = $playerPassword }
    $bob = Request-Api POST '/api/auth/register' @{ username = 'verify_bob'; email = 'bob@example.test'; password = $playerPassword }
    $null = Request-Api POST '/api/auth/register' @{ username = 'VERIFY_ALICE'; email = 'other@example.test'; password = $playerPassword } '' 409
    $null = Request-Api POST '/api/auth/login' @{ username = 'verify_alice'; password = 'wrong-password' } '' 401
    $aliceAuth = Request-Api POST '/api/auth/login' @{ username = 'verify_alice'; password = $playerPassword }
    $bobAuth = Request-Api POST '/api/auth/login' @{ username = 'verify_bob'; password = $playerPassword }
    $admin = Request-Api POST '/api/auth/admin/login' @{ username = $adminUsername; password = $adminPassword }
    $a = $aliceAuth.token; $b = $bobAuth.token; $adm = $admin.token
    $null = Request-Api GET '/api/admin/overview' $null $a 403
    $null = Request-Api POST '/api/character/create' @{ name = 'InvalidStart'; classId = 1; raceId = 1; mapId = 2 } $a 400
    $aliceCharacter = Request-Api POST '/api/character/create' @{ name = 'VerifyAlice'; classId = 1; raceId = 1 } $a
    $bobCharacter = Request-Api POST '/api/character/create' @{ name = 'VerifyBob'; classId = 2; raceId = 1 } $b
    $ac = $aliceCharacter.characterId; $bc = $bobCharacter.characterId
    $characters = @(Request-Api GET '/api/character/me' $null $a)
    Check ($characters.Count -eq 1 -and $characters[0].mapId -eq 1 -and $characters[0].gold -eq 100) 'new character must start in the village with the account starter balance'
    Check (!($characters[0].PSObject.Properties.Name -contains 'passwordHash')) 'character DTO must not expose account hashes'
    $null = Request-Api GET "/api/gameplay/$bc/state" $null $a 404
    $world = Request-Api GET '/api/world/state' $null $a
    Check ($world.regions.Count -eq 4 -and $null -ne $world.regions[0].weather.cropModifier) 'world must expose regional weather and crop effects'
    $null = Request-Api POST '/api/auth/heartbeat' $null $a

    $listing = Request-Api POST '/api/community/listings' @{ characterId = $ac; itemId = 1; quantity = 2; unitPrice = 10 } $a
    $listed = @(Request-Api GET '/api/community/listings?regionId=1' $null $b)
    $matchingListings = @($listed | Where-Object id -eq $listing.listingId)
    Check ($matchingListings.Count -eq 1) ('escrow listing must appear in its region; created=' +
        (ConvertTo-Json -InputObject $listing -Compress -Depth 8) + '; listings=' +
        (ConvertTo-Json -InputObject $listed -Compress -Depth 8))
    $null = Request-Api POST "/api/community/listings/$($listing.listingId)/buy" @{ characterId = $ac; quantity = 1 } $a 400
    $null = Request-Api POST "/api/community/listings/$($listing.listingId)/buy" @{ characterId = $bc; quantity = 1 } $b
    $aState = Request-Api GET "/api/gameplay/$ac/state" $null $a
    $bState = Request-Api GET "/api/gameplay/$bc/state" $null $b
    Check ($aState.character.gold -eq 110 -and $bState.character.gold -eq 90) 'market gold must transfer atomically between accounts'
    Check ((@($aState.inventory | Where-Object itemId -eq 1 | Measure-Object quantity -Sum)[0].Sum) -eq 3) 'listed inventory must be held in escrow'
    $expensive = Request-Api POST '/api/community/listings' @{ characterId = $ac; itemId = 2; quantity = 1; unitPrice = 1000000 } $a
    $null = Request-Api POST "/api/community/listings/$($expensive.listingId)/buy" @{ characterId = $bc; quantity = 1 } $b 400
    $bAfterFailure = Request-Api GET "/api/gameplay/$bc/state" $null $b
    Check ($bAfterFailure.character.gold -eq 90) 'failed market purchase must leave wallet unchanged'
    $null = Request-Api POST "/api/community/listings/$($listing.listingId)/buy" @{ characterId = $bc; quantity = 1 } $b
    $null = Request-Api POST "/api/community/listings/$($listing.listingId)/buy" @{ characterId = $bc; quantity = 1 } $b 409
    $null = Request-Api POST "/api/community/listings/$($expensive.listingId)/cancel" @{ characterId = $ac } $a
    $null = Request-Api POST "/api/community/listings/$($expensive.listingId)/cancel" @{ characterId = $ac } $a 409
    $guild = Request-Api POST '/api/community/guilds' @{ characterId = $ac; name = 'Verification Caravan' } $a
    $null = Request-Api POST "/api/community/guilds/$($guild.guildId)/join" @{ characterId = $bc; role = 'Scout' } $b
    $null = Request-Api POST "/api/community/guilds/$($guild.guildId)/join" @{ characterId = $bc; role = 'Guard' } $b 409
    $guilds = @(Request-Api GET '/api/community/guilds' $null $a)
    Check ($guilds[0].members.Count -eq 2) 'guild must include the leader and joined scout once'

    $null = Request-Api POST "/api/admin/characters/$ac/restore" @{ itemId = 1; quantity = 3; reason = 'Verification restoration' } $adm
    $currency = Request-Api POST "/api/admin/characters/$ac/currency" @{ amount = 50; reason = 'Verification grant' } $adm
    Check ($currency.gold -eq 170) 'admin gold adjustment must persist after player market sales'
    $null = Request-Api POST "/api/admin/characters/$ac/currency" @{ amount = -1000000000; reason = 'Verification insufficient funds' } $adm 400
    $null = Request-Api PUT '/api/admin/items/1/price' @{ price = 17.5; reason = 'Verification price' } $adm
    $items = @(Request-Api GET '/api/admin/items' $null $adm)
    Check (($items | Where-Object id -eq 1).basePrice -eq 17.5) 'admin item price must update persisted catalog'
    $null = Request-Api POST '/api/admin/economy-events' @{ regionId = 2; itemId = 1; modifier = 5; eventType = 'Siege'; description = 'Verification supply shock'; startsAt = [DateTime]::UtcNow.AddMinutes(-1).ToString('o'); endsAt = [DateTime]::UtcNow.AddHours(1).ToString('o') } $adm
    $null = Request-Api PUT '/api/admin/gacha/1' @{ weight = 60; isActive = $true; reason = 'Verification rate' } $adm
    $rates = Request-Api GET '/api/player/gacha' $null $a
    Check (($rates.rewards | Measure-Object chance -Sum).Sum -gt 0.999) 'public gacha chances must normalize to one'
    $null = Request-Api POST '/api/player/gacha/pull' @{ characterId = $bc } $a 404
    $null = Request-Api POST '/api/player/gacha/pull' @{ characterId = $ac } $a

    $event = Request-Api POST '/api/admin/events' @{ name = 'Verification Festival'; description = 'LiveOps verification'; startsAt = [DateTime]::UtcNow.AddMinutes(-1).ToString('o'); endsAt = [DateTime]::UtcNow.AddHours(1).ToString('o') } $adm
    $events = @(Request-Api GET '/api/player/events' $null $a)
    Check (@($events | Where-Object id -eq $event.id).Count -eq 1) 'enabled live event must be visible without a client update'
    $null = Request-Api PUT "/api/admin/events/$($event.id)/toggle" @{ isActive = $false } $adm
    $events = @(Request-Api GET '/api/player/events' $null $a)
    Check (@($events | Where-Object id -eq $event.id).Count -eq 0) 'disabled live event must disappear immediately'
    $null = Request-Api POST '/api/admin/announcements' @{ title = 'Verification system mail'; content = 'Maintenance complete'; startAt = [DateTime]::UtcNow.AddMinutes(-1).ToString('o') } $adm
    $mail = @(Request-Api GET '/api/player/mail' $null $a)
    Check (@($mail | Where-Object title -eq 'Verification system mail').Count -eq 1) 'system mail must reach players'
    $null = Request-Api POST '/api/admin/giftcodes' @{ code = 'VERIFY_GIFT'; rewardType = 'Gold'; rewardAmount = 10; maxUsage = 10 } $adm
    $null = Request-Api POST '/api/player/giftcodes/redeem' @{ characterId = $ac; code = 'verify_gift' } $a
    $null = Request-Api POST '/api/player/giftcodes/redeem' @{ characterId = $ac; code = 'VERIFY_GIFT' } $a 409
    $null = Request-Api POST '/api/player/tickets' @{ subject = 'Wrong ownership'; message = 'Verification'; category = 'Item'; characterId = $bc } $a 404
    $ticket = Request-Api POST '/api/player/tickets' @{ subject = 'Verification missing item'; message = 'Please restore wheat'; category = 'Item'; characterId = $ac } $a
    $null = Request-Api POST "/api/admin/tickets/$($ticket.id)/reply" @{ message = 'Restored and verified'; resolve = $true } $adm
    $tickets = @(Request-Api GET '/api/player/tickets' $null $a)
    Check ($tickets[0].status -eq 2 -and $tickets[0].messages[0].message -eq 'Restored and verified') 'support reply and resolved status must reach the ticket owner'
    $otherTickets = @(Request-Api GET '/api/player/tickets' $null $b)
    Check ($otherTickets.Count -eq 0) 'players must not see another account support tickets'

    $null = Request-Api POST "/api/gameplay/$ac/cook" @{ recipeId = 1 } $a
    $null = Request-Api POST "/api/gameplay/$ac/plant" @{ cropId = 1 } $a
    $farm = Request-Api GET "/api/gameplay/$ac/state" $null $a
    Check ($farm.plots.Count -eq 1 -and $farm.plots[0].expectedYield -gt 0) 'planting must create a weather-adjusted persistent crop'
    $null = Request-Api POST "/api/gameplay/$ac/harvest" @{ plotId = $farm.plots[0].id } $a 400
    $null = Request-Api POST "/api/gameplay/$ac/learn" @{ npcId = 1; skillId = 2 } $a 400
    $null = Request-Api POST "/api/gameplay/$ac/travel" @{ mapId = 5 } $a
    $null = Request-Api POST "/api/gameplay/$ac/fight" @{ monsterId = 1 } $a
    $combat = Request-Api GET "/api/gameplay/$ac/state" $null $a
    Check ($combat.character.experience -gt 0 -and $combat.character.mapId -eq 5) 'server combat must award experience in the reachable forest'
    $null = Request-Api POST "/api/admin/accounts/$($bob.accountId)/ban" @{ reason = 'Verification cheat ban' } $adm
    $null = Request-Api GET '/api/auth/me' $null $b 401
    $null = Request-Api POST '/api/auth/login' @{ username = 'verify_bob'; password = $playerPassword } '' 403
    $null = Request-Api POST "/api/admin/accounts/$($bob.accountId)/unban" @{ reason = 'Verification unban' } $adm
    $null = Request-Api GET '/api/auth/me' $null $b 401
    $bobAuth = Request-Api POST '/api/auth/login' @{ username = 'verify_bob'; password = $playerPassword }
    $refreshed = Request-Api POST '/api/auth/refresh' @{ refreshToken = $aliceAuth.refreshToken }
    $null = Request-Api POST '/api/auth/refresh' @{ refreshToken = $aliceAuth.refreshToken } '' 401
    $null = Request-Api POST '/api/auth/logout' $null $refreshed.token
    $null = Request-Api GET '/api/auth/me' $null $refreshed.token 401
    $null = Request-Api POST '/api/auth/refresh' @{ refreshToken = $refreshed.refreshToken } '' 401
    $overview = Request-Api GET '/api/admin/overview' $null $adm
    Check ($overview.accounts -eq 2 -and $overview.characters -eq 2 -and $overview.ccu -eq 1 -and $overview.errorsToday -eq 0) 'analytics must reflect persisted players, current sessions, and server health'
    $audit = @(Request-Api GET '/api/admin/audit' $null $adm)
    Check ($audit.Count -ge 10) 'admin operations must leave an audit trail'

    Stop-VerifyServer
    Start-VerifyServer
    $adminRestart = Request-Api POST '/api/auth/admin/login' @{ username = $adminUsername; password = $adminPassword }
    $persistedItems = @(Request-Api GET '/api/admin/items' $null $adminRestart.token)
    Check (($persistedItems | Where-Object id -eq 1).basePrice -eq 17.5) 'item price must survive a full server restart'
    $persistedOverview = Request-Api GET '/api/admin/overview' $null $adminRestart.token
    Check ($persistedOverview.accounts -eq 2 -and $persistedOverview.characters -eq 2) 'accounts and characters must survive a full server restart'
    Check ((Test-Path -LiteralPath $databasePath) -and (Get-Item -LiteralPath $databasePath).Length -gt 0) 'verification must use its isolated on-disk SQLite database'
    Write-Output ('PASS: ' + $script:checks + ' HTTP and state checks; isolated SQLite persistence and server restart verified.')
    Write-Output ('Verification artifacts: ' + $runPath)
}
catch {
    Stop-VerifyServer
    $serverStdout = if ($null -ne $script:stdoutTask) { $script:stdoutTask.GetAwaiter().GetResult() } else { '' }
    $serverStderr = if ($null -ne $script:stderrTask) { $script:stderrTask.GetAwaiter().GetResult() } else { '' }
    if (![string]::IsNullOrWhiteSpace($serverStdout)) { Write-Host ('Verification server stdout:' + [Environment]::NewLine + $serverStdout) }
    if (![string]::IsNullOrWhiteSpace($serverStderr)) { Write-Host ('Verification server stderr:' + [Environment]::NewLine + $serverStderr) }
    throw
}
finally {
    Stop-VerifyServer
    $client.Dispose()
    $adminPassword = $null; $playerPassword = $null; $jwtKey = $null
}
