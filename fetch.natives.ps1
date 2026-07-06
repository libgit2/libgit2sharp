<#
.SYNOPSIS
    Downloads the prebuilt LibGit2Sharp native payload (all RIDs) pinned in natives.lock.json,
    verifies its SHA256, and extracts it to native/.

    This replaces the old LibGit2Sharp.NativeBinaries.UiPath PackageReference: the managed build
    consumes these binaries directly. Verification is mandatory - if the lockfile SHA256 is empty or
    does not match the download, this fails hard.
.PARAMETER Force
    Re-download and re-extract even if native/ already exists.
#>

Param(
    [switch]$Force
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
# Expand-Archive/native calls below drive on their own results; don't let a non-zero native exit
# auto-throw first (PowerShell 7.4+ defaults this to $true). Harmless no-op on Windows PowerShell 5.1.
$PSNativeCommandUseErrorActionPreference = $false

$projectDirectory = Split-Path $MyInvocation.MyCommand.Path
$lockPath = Join-Path $projectDirectory 'natives.lock.json'
$nativeDirectory = Join-Path $projectDirectory 'native'
$cacheDirectory = Join-Path $nativeDirectory '_cache'

# Proxy-aware download, mirroring fetch.deps.ps1 in the nativebinaries repo.
function Invoke-Download($url, $outFile) {
    [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
    $params = @{ Uri = $url; OutFile = $outFile; UseBasicParsing = $true }
    $proxy = [System.Net.WebRequest]::GetSystemWebProxy()
    if ($proxy) {
        $proxy.Credentials = [System.Net.CredentialCache]::DefaultCredentials
        $proxyUri = $proxy.GetProxy([uri]$url)
        # GetProxy returns an empty value (or the url itself) for a direct connection; only route
        # through a proxy when it names a genuinely different endpoint.
        if ($proxyUri -and "$proxyUri" -ne "$url") {
            $params.Proxy = "$proxyUri"
            $params.ProxyUseDefaultCredentials = $true
        }
    }
    Write-Host "-> Downloading $url"
    Invoke-WebRequest @params
}

$lock = Get-Content $lockPath -Raw | ConvertFrom-Json
$expectedSha = "$($lock.sha256)".Trim().ToLower()
if (-not $expectedSha) {
    throw "natives.lock.json has no sha256. Run the nativebinaries 'build' workflow (publish=true), " +
          "then populate tag/url/sha256 here. Refusing to fetch without hash verification."
}

if ((Test-Path $nativeDirectory) -and -not $Force) {
    $marker = Join-Path $nativeDirectory '.fetched-sha256'
    if ((Test-Path $marker) -and ((Get-Content $marker -Raw).Trim().ToLower() -eq $expectedSha)) {
        Write-Host "==> Native payload already present for sha256 $expectedSha (use -Force to refresh). Skipping."
        return $nativeDirectory
    }
}

New-Item -ItemType Directory -Path $cacheDirectory -Force | Out-Null
$archive = Join-Path $cacheDirectory $lock.filename
Invoke-Download $lock.url $archive

$actualSha = (Get-FileHash -Algorithm SHA256 -Path $archive).Hash.ToLower()
if ($actualSha -ne $expectedSha) {
    Remove-Item $archive -Force
    throw "SHA256 mismatch for '$($lock.filename)'.`n  expected: $expectedSha`n  actual:   $actualSha`nAborting."
}
Write-Host "==> SHA256 verified: $actualSha"

# Wipe the payload (but keep the download cache) so stale RIDs never linger.
Get-ChildItem $nativeDirectory -Force -Exclude '_cache' -ErrorAction SilentlyContinue | Remove-Item -Recurse -Force
Expand-Archive -Path $archive -DestinationPath $nativeDirectory -Force
Set-Content -Path (Join-Path $nativeDirectory '.fetched-sha256') -Value $expectedSha -NoNewline
Write-Host "==> Extracted native payload to '$nativeDirectory'"

return $nativeDirectory
