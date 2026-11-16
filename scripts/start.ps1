param([switch]$SetupAdmin)
$ErrorActionPreference = 'Stop'
$project = Join-Path $PSScriptRoot '../GameWorld.Api'
$previousUsername = $env:BootstrapAdmin__Username
$previousPassword = $env:BootstrapAdmin__Password
try {
    if ($SetupAdmin) {
        $username = Read-Host 'Tên tài khoản quản trị (ví dụ: admin)'
        $secret = Read-Host 'Mật khẩu quản trị mới (ít nhất 12 ký tự)' -AsSecureString
        $password = [System.Net.NetworkCredential]::new('', $secret).Password
        if ([string]::IsNullOrWhiteSpace($username) -or $password.Length -lt 12) { throw 'Cần tên quản trị và mật khẩu ít nhất 12 ký tự.' }
        $env:BootstrapAdmin__Username = $username
        $env:BootstrapAdmin__Password = $password
        $password = $null
    }
    & dotnet run --project $project --launch-profile http
    if ($LASTEXITCODE -ne 0) { throw 'Máy chủ chưa khởi động được. Xem thông báo .NET phía trên.' }
} finally {
    $env:BootstrapAdmin__Username = $previousUsername
    $env:BootstrapAdmin__Password = $previousPassword
}
