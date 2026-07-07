---
name: release-libgit2-natives
description: >-
  Runbook for cutting a new LibGit2Sharp.UiPath native release: bump the OpenSSL/libssh2 submodules,
  rebuild the prebuilt deps, rebuild the multi-RID native archive, and roll the new SHA256 pins
  through the two lockfiles up to the final NuGet publish. Use when asked to bump OpenSSL or libssh2,
  refresh the native binaries, produce a new natives archive, or ship a new LibGit2Sharp.UiPath
  version. Driven interactively — pause for human review at each hash-pin gate.
---

# Releasing LibGit2Sharp.UiPath native binaries

This skill drives the cross-repo release chain by hand, **interactively**. There is deliberately no
CI auto-PR: every hop that moves a SHA256 pin ends at a **human review gate** — you show the diff,
confirm with the user, then commit. Hash-pinning is a security boundary; a freshly-built SHA must be
reviewed before the next stage is allowed to consume it. Never auto-merge, never skip a gate.

## The two repos

| Role | Path | GitHub | Default branch |
|------|------|--------|----------------|
| Managed library (this repo) | `.` | `UiPath/libgit2sharp` | `develop` |
| Native binaries (sibling)   | `../libgit2sharp.nativebinaries` | `UiPath/libgit2sharp.nativebinaries` | `develop` |

The native-binaries repo is a **sibling directory** next to this one. All paths below are relative to
this repo's root (the managed `libgit2sharp`). The atom script is at
`./.claude/skills/release-libgit2-natives/Update-Lockfile.ps1`.

> **Branch note:** `develop` is the default/integration branch for **both** repos. Dispatch workflows
> against `develop` (`--ref develop`), or against a feature branch (`--ref <branch>`) to validate the
> whole chain before merging.

## The chain (what produces what)

```
bump openssl/libssh2 submodule + deps/vcpkg.json      (sibling repo)
        │
        ▼  build-deps.yml  (manual)
   Release  deps-openssl-<X>_libssh2-<Y>   →  6× deps-<platform>.{zip,tar.gz} + .sha256
        │
        ▼  GATE A: Update-Lockfile.ps1 → deps.lock.json  (sibling repo) — review + branch + commit + PR
        │
        ▼  build.yml  (publish=true)
   Release  natives-<version>              →  natives-<version>.zip + .sha256
        │
        ▼  GATE B: Update-Lockfile.ps1 → natives.lock.json  (this repo) — review + branch + commit + PR
        │
        ▼  fetch.natives.ps1 + CI (ci.yml) build & test all 3 platforms
        │
        ▼  (the very last step) publish LibGit2Sharp.UiPath nupkg to the uipath-internal feed
```

`build-deps.yml` runs **rarely** — only when the submodule versions change. Most releases that are
*just* a libgit2 rebuild start at `build.yml` and reuse the existing deps release.

---

## Step 0 — Bump the deps (only when changing OpenSSL/libssh2)

Skip this whole step if you are not changing the dep versions. The usual trigger is a new **OpenSSL**
patch (occasionally **libssh2**), often a CVE fix. Three things move in **lockstep**, or
`build.deps.ps1` throws on its version assertion:

1. **The submodule tag** (the authoritative pin), in `../libgit2sharp.nativebinaries`:
   ```bash
   cd ../libgit2sharp.nativebinaries
   git -C openssl fetch --tags --quiet
   git -C openssl checkout openssl-<X>      # e.g. openssl-3.6.4  (libssh2 uses tag libssh2-<Y>)
   git add openssl
   ```
2. **The `overrides` version** in `deps/vcpkg.json` — must equal the submodule version exactly.
3. **`builtin-baseline`** in `deps/vcpkg.json` — a vcpkg commit whose versions database actually
   contains that OpenSSL/libssh2 version. **This is the real friction, not the SHA copying:** if
   vcpkg's registry doesn't yet ship the exact patch at the current baseline, bump `builtin-baseline`
   to a newer vcpkg commit that does (check `microsoft/vcpkg` history / `versions/o-/openssl.json`).
   `build.deps.ps1` asserts `submodule == override == what-vcpkg-built` and fails loudly otherwise.

Commit these on a branch in the sibling repo. **Gate:** show the user the submodule + vcpkg.json diff
and confirm before pushing.

## Step 1 — Rebuild the deps (`build-deps.yml`)

```bash
gh workflow run build-deps.yml --repo UiPath/libgit2sharp.nativebinaries --ref <branch>
# grab the run id, then:
gh run list --repo UiPath/libgit2sharp.nativebinaries --workflow build-deps.yml --limit 1 --json databaseId
gh run watch <run-id> --repo UiPath/libgit2sharp.nativebinaries --exit-status
```

It fans out over 6 platforms and publishes Release **`deps-openssl-<X>_libssh2-<Y>`** (the tag is
derived from the built versions, so it's predictable from `deps/vcpkg.json`). Each archive gets a
bare-hash `.sha256` sidecar.

## Gate A — Roll the deps pins into `deps.lock.json`

```bash
TAG=deps-openssl-<X>_libssh2-<Y>
gh release download "$TAG" --repo UiPath/libgit2sharp.nativebinaries --pattern '*.sha256' --dir "$TMPDIR/depssha"
pwsh ./.claude/skills/release-libgit2-natives/Update-Lockfile.ps1 \
    -LockfilePath ../libgit2sharp.nativebinaries/deps.lock.json -Tag "$TAG" -ShaDir "$TMPDIR/depssha"
```

**Human gate:** `git -C ../libgit2sharp.nativebinaries diff deps.lock.json`, show it, confirm, then in
the sibling repo **create a new branch, commit, push, and open a PR** (`gh pr create`) against
`develop`. Do **not** merge yourself and do **not** proceed until the user has reviewed and merged it —
the libgit2 build is about to trust these hashes.

## Step 2 — Rebuild the native archive (`build.yml`, `publish=true`)

```bash
gh workflow run build.yml --repo UiPath/libgit2sharp.nativebinaries --ref <branch> -f publish=true
gh run list --repo UiPath/libgit2sharp.nativebinaries --workflow build.yml --limit 1 --json databaseId
gh run watch <run-id> --repo UiPath/libgit2sharp.nativebinaries --exit-status
```

It fetches + SHA256-verifies the deps (no OpenSSL/libssh2 compile), builds libgit2 for all 6 RIDs,
assembles `natives-<version>.zip`, and — because `publish=true` (or on `develop`) — publishes Release
**`natives-<version>`**. `<version>` comes from **MinVer** on the nativebinaries repo, collapsed to a
single auto number `X.Y.Z-v<height>` (base tag `1.9.1-v5` + height 21 → `1.9.1-v21`; see Reference).
Find the exact tag:

```bash
gh release list --repo UiPath/libgit2sharp.nativebinaries --limit 5   # newest natives-* is yours
```

## Gate B — Roll the natives pin into `natives.lock.json` (this repo)

```bash
NTAG=natives-<version>
gh release download "$NTAG" --repo UiPath/libgit2sharp.nativebinaries --pattern '*.sha256' --dir "$TMPDIR/natsha"
pwsh ./.claude/skills/release-libgit2-natives/Update-Lockfile.ps1 \
    -LockfilePath ./natives.lock.json -Tag "$NTAG" -ShaDir "$TMPDIR/natsha"
# prove it fetches + verifies before trusting the pin:
pwsh ./fetch.natives.ps1 -Force
```

`fetch.natives.ps1` fails hard on a SHA mismatch, so a green fetch confirms the pin. **Human gate:**
show the `natives.lock.json` diff, confirm, then **create a new branch, commit, push, and open a PR**
(`gh pr create`) against `develop`. Do **not** merge yourself — the PR's `ci.yml` run exercises
windows/ubuntu/macos, and the user reviews and merges.

## Step 3 — Ship the managed package (the very last step)

**Versioning — nothing to do.** The package version is `X.Y.Z-v<height>` (e.g. `1.9.1-v22`), a single
auto-incrementing number. `<height>` is MinVer's commit count since the base tag, surfaced as `v<N>`
by the managed repo's `AdjustVersions` target and the nativebinaries `build.yml` "Resolve version"
step (both collapse MinVer's `<epoch>.<height>` into one `v<height>`). Every develop commit yields the
next `v<N>` — no manual version tagging.

**Iron rule:** the existing `X.Y.Z-vN` tag (e.g. `1.9.1-v5`) is a **permanent counting anchor**. Do
**not** cut another `-vN` tag on the same `X.Y.Z` line — the height (hence `v<N>`) would reset and the
package version would regress, which NuGet forbids (versions must rise). Cut a new base tag **only**
when the upstream base changes, once (e.g. `git tag 1.10.0-v0`), to start a fresh line. Same scheme
and anchor tag apply to the nativebinaries repo.

**Publishing to the uipath-internal feed — done interactively from here** (like the gates: there is
no CI publish job). Once the Gate B PR is merged to `develop` and CI is green, get the
`LibGit2Sharp.UiPath` nupkg — download the managed CI's **NuGet packages** artifact, or build locally
(`dotnet build -c Release` emits it under `bin/Packages/`, via `GeneratePackageOnBuild`).

**Recommended — mint a short-lived Azure DevOps token via the Azure CLI** (no stored PAT, reuses your
`az login` SSO):

```bash
FEED=https://pkgs.dev.azure.com/uipath/Public.Feeds/_packaging/UiPath-Internal/nuget/v3/index.json
# 499b84ac-... is the well-known Azure DevOps resource id
TOKEN=$(az account get-access-token --resource 499b84ac-1321-427f-aa17-267ca6975798 --query accessToken -o tsv)
dotnet nuget add source "$FEED" --name uipath-internal 2>/dev/null || true
NuGetPackageSourceCredentials_uipath-internal="Username=az;Password=$TOKEN" \
  dotnet nuget push "<path-to>.nupkg" --source uipath-internal --api-key az --skip-duplicate
```

**Fallback** — if the Azure Artifacts credential provider is already configured on the machine, just:
`nuget push <pkg> -src "$FEED" -ApiKey AzureDevops -SkipDuplicate`.

`--api-key`/`-ApiKey` is a required-but-ignored dummy (auth is the token / credential provider, not the
key). `--skip-duplicate` keeps it idempotent — and matters here: the feed may already hold a
manually-published `1.9.1-v5`, so the emitted `X.Y.Z-v<height>` must **exceed** the highest version
already on the feed (see the version-collision caveat). Human-run step — confirm the exact version with
the user before pushing. Only if the upstream base changed: `git tag X.Y.Z-v0` once, first.

---

## Reference

**Lockfile shapes** (both carry a `repo` field the atom reads to rebuild URLs):
- `deps.lock.json` (sibling repo): `{ repo, tag, platforms: { <rid>: { filename, url, sha256 } } }`,
  6 RIDs. Windows = `.zip` (dynamic DLLs); posix = `.tar.gz` (static `.a`, symlinks preserved).
- `natives.lock.json` (this repo): `{ repo, tag, filename, url, sha256 }`, one archive; `filename` is
  always `<tag>.zip`.

**MinVer** — both repos use **bare numeric base tags** (no `v` prefix) with
`--default-pre-release-identifiers preview.0`. MinVer emits `X.Y.Z-<epoch>.<height>` (e.g.
`1.9.1-v5.22`); both repos then **collapse it to `X.Y.Z-v<height>`** (e.g. `1.9.1-v22`) — a single
monotonic auto number. The base tag (`1.9.1-v5`) is a permanent anchor; never re-tag the line (Step
3's iron rule). New upstream base → one fresh `X.Y.Z-v0` tag.

**Release gating** — `build.yml`'s publish step is gated on `inputs.publish || github.ref ==
refs/heads/develop`. PR/branch runs without `publish=true` produce only a workflow artifact (no stray
release). Always pass `-f publish=true` when you actually want the durable natives Release.

**`build-deps.yml`** is manual (`workflow_dispatch`) only — never on push — so a rebuild can't
silently republish archives with fresh, unpinned SHA256s.

**Sidecar formats differ** (the atom handles both): deps sidecars are a bare hash; the natives
sidecar is `sha  filename` (sha256sum format). `Update-Lockfile.ps1` takes the first token either way.

**Testing off a branch** — `gh workflow run … --ref <branch>` dispatches the workflow as it exists on
that branch, so you can validate the whole chain before merging to `develop`.

**Gotchas**
- The deps rebuild is gated on the version existing in vcpkg's registry at the pinned baseline —
  budget time for a `builtin-baseline` bump, not just a submodule checkout.
- The libgit2 submodule is private; `build.yml` clones it via the `LIBGIT2_DEPLOY_KEY` secret
  (read-only deploy key). Nothing to do locally unless you're building libgit2 yourself.
- `gh run watch` needs the run id; logs are only fully available once the whole run completes.
