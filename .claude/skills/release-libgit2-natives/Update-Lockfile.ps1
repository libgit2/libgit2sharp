<#
.SYNOPSIS
    Rewrites a LibGit2Sharp dependency lockfile (deps.lock.json or natives.lock.json) to point at a
    new GitHub Release: sets the tag, recomputes each asset URL, and writes the SHA256(s).

    This is the single shared "atom" of the release chain. The release-libgit2-natives skill calls it
    at both hand-off points (deps -> deps.lock.json, natives -> natives.lock.json). It never talks to
    the network or to git; you hand it a tag and the SHA256 sidecar(s) the workflow already produced,
    and it edits one JSON file. Review the diff and commit yourself.

.DESCRIPTION
    Two lockfile shapes are handled, auto-detected from the JSON:

      * multi-platform (deps.lock.json, in the sibling libgit2sharp.nativebinaries repo): has a
        top-level "platforms" object. Each platform keeps its existing "filename"
        (deps-<platform>.zip / .tar.gz), and its "url" + "sha256" are refreshed. A SHA256 must be
        supplied for every platform present in the file (via -ShaDir), or it throws.

      * flat (natives.lock.json, in this repo): has a top-level "filename". The filename is derived
        as "<tag>.zip" (the natives archive is always named after its tag), and "url" + "sha256" are
        refreshed.

    The base repo ("UiPath/...") is read from the lockfile's own "repo" field, so URLs stay correct
    without being passed in. SHA256 values come either from -Sha256 (single, flat lockfiles only) or
    from -ShaDir, a directory of "<filename>.sha256" sidecars as published on the Release / uploaded
    as workflow artifacts. Sidecars may be a bare hash (deps) or "sha  filename" sha256sum format
    (natives); the first whitespace-delimited token is taken as the hash.

.PARAMETER LockfilePath
    Path to the lockfile to rewrite (deps.lock.json or natives.lock.json).

.PARAMETER Tag
    The GitHub Release tag the assets live under (e.g. 'deps-openssl-3.6.3_libssh2-1.11.1' or
    'natives-1.9.1-v5.21').

.PARAMETER ShaDir
    Directory containing '<filename>.sha256' sidecar files. Get them with:
        gh release download <tag> --repo <repo> --pattern '*.sha256' --dir <ShaDir>
    Required for multi-platform lockfiles; optional for flat ones (use -Sha256 instead).

.PARAMETER Sha256
    A single SHA256 hash, for flat (natives) lockfiles only. Convenient when you already have the hash
    from the workflow output. Ignored for multi-platform lockfiles.

.EXAMPLE
    # Deps hand-off (6 platforms): download the sidecars, then rewrite the sibling repo's lockfile.
    gh release download deps-openssl-3.6.3_libssh2-1.11.1 --repo UiPath/libgit2sharp.nativebinaries `
        --pattern '*.sha256' --dir $env:TEMP/depssha
    ./.claude/skills/release-libgit2-natives/Update-Lockfile.ps1 `
        -LockfilePath ../libgit2sharp.nativebinaries/deps.lock.json `
        -Tag deps-openssl-3.6.3_libssh2-1.11.1 -ShaDir $env:TEMP/depssha

.EXAMPLE
    # Natives hand-off (single archive): pass the hash straight through to this repo's lockfile.
    ./.claude/skills/release-libgit2-natives/Update-Lockfile.ps1 -LockfilePath ./natives.lock.json `
        -Tag natives-1.9.1-v5.22 -Sha256 1a40ac67ac14b1e099b4d3a0823019e164fcde9211e3c40c95c6b355267f7440
#>

[CmdletBinding()]
Param(
    [Parameter(Mandatory)][string]$LockfilePath,
    [Parameter(Mandatory)][string]$Tag,
    [string]$ShaDir = '',
    [string]$Sha256 = ''
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if (-not (Test-Path $LockfilePath)) { throw "Lockfile not found: $LockfilePath" }

$lock = Get-Content $LockfilePath -Raw | ConvertFrom-Json
$repo = "$($lock.repo)".Trim()
if (-not $repo) { throw "Lockfile '$LockfilePath' has no 'repo' field; cannot build asset URLs." }

function Get-ShaFor($filename) {
    # A single explicit hash always wins (flat lockfiles). Otherwise read the sidecar.
    if ($Sha256) { return $Sha256.Trim().ToLower() }
    if (-not $ShaDir) {
        throw "No -Sha256 and no -ShaDir given; cannot resolve the SHA256 for '$filename'."
    }
    $sidecar = Join-Path $ShaDir "$filename.sha256"
    if (-not (Test-Path $sidecar)) {
        throw "SHA256 sidecar not found for '$filename' (looked for '$sidecar'). " +
              "Did 'gh release download --pattern *.sha256' fetch it?"
    }
    # Sidecars are either a bare hash (deps) or 'sha  filename' (natives); take the first token.
    $raw = (Get-Content $sidecar -Raw).Trim()
    $hash = ($raw -split '\s+')[0].ToLower()
    if ($hash -notmatch '^[0-9a-f]{64}$') {
        throw "Sidecar '$sidecar' did not contain a 64-char SHA256 (got '$hash')."
    }
    return $hash
}

function New-AssetUrl($filename) {
    return "https://github.com/$repo/releases/download/$Tag/$filename"
}

$lock.tag = $Tag

if ($lock.PSObject.Properties.Name -contains 'platforms') {
    # --- multi-platform (deps.lock.json) ------------------------------------------------------
    if (-not $ShaDir) { throw "Multi-platform lockfile needs -ShaDir (one <filename>.sha256 per platform)." }
    foreach ($p in $lock.platforms.PSObject.Properties) {
        $entry = $p.Value
        $filename = "$($entry.filename)".Trim()
        if (-not $filename) { throw "Platform '$($p.Name)' has no 'filename' in the lockfile." }
        $entry.url = New-AssetUrl $filename
        $entry.sha256 = Get-ShaFor $filename
        Write-Host "  $($p.Name): $($entry.sha256)"
    }
}
elseif ($lock.PSObject.Properties.Name -contains 'filename') {
    # --- flat (natives.lock.json) -------------------------------------------------------------
    # The natives archive is always named after its tag.
    $filename = "$Tag.zip"
    $lock.filename = $filename
    $lock.url = New-AssetUrl $filename
    $lock.sha256 = Get-ShaFor $filename
    Write-Host "  ${filename}: $($lock.sha256)"
}
else {
    throw "Unrecognised lockfile shape (no 'platforms' and no 'filename'): $LockfilePath"
}

# Depth covers the nested platforms object; pwsh 7 pretty-prints and does not escape '/'. The first
# rewrite may reformat the file once; subsequent runs touch only tag/url/sha256 lines.
$lock | ConvertTo-Json -Depth 10 | Set-Content -Path $LockfilePath -Encoding utf8
Write-Host "==> Updated $LockfilePath -> tag $Tag"
