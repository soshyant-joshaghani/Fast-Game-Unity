# Fetch or update sibling Colyseus UPM package (io.colyseus.sdk) for fast-game-unity.
# Usage:
#   .\Scripts\fetch-colyseus.ps1           # install / sync to Scripts\colyseus.lock.json ref
#   .\Scripts\fetch-colyseus.ps1 -Update   # fetch latest from remote default branch tip
#   .\Scripts\fetch-colyseus.ps1 -Ref abc1234
param(
    [switch]$Update,
    [string]$Ref = ""
)

$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent $PSScriptRoot
$LockPath = Join-Path $PSScriptRoot "colyseus.lock.json"
$Lock = Get-Content -Raw -Encoding UTF8 $LockPath | ConvertFrom-Json

$Repo = $Lock.repo
$TargetRef = if ($Ref) { $Ref } elseif ($Update) { "" } else { $Lock.ref }
$SourceSub = $Lock.sourceSubpath
$Dest = Join-Path $Root $Lock.dest
$VersionFile = Join-Path $Dest ".colyseus-version"
$Tmp = Join-Path $env:TEMP ("colyseus-unity-sdk-" + [guid]::NewGuid().ToString("n"))

function Write-VersionFile([string]$Path, [string]$Sha, [string]$RemoteRef) {
    @{
        repo = $Repo
        ref = $RemoteRef
        commit = $Sha
        package = "io.colyseus.sdk"
        fetchedAt = (Get-Date).ToUniversalTime().ToString("o")
    } | ConvertTo-Json | Set-Content -Encoding UTF8 $Path
}

function Copy-Tree([string]$From, [string]$To) {
    if (-not (Test-Path $From)) {
        throw "Missing path in upstream repo: $From"
    }
    if (Test-Path $To) {
        Remove-Item -Recurse -Force $To
    }
    $parent = Split-Path -Parent $To
    if (-not (Test-Path $parent)) {
        New-Item -ItemType Directory -Path $parent | Out-Null
    }
    Copy-Item -Recurse -Force $From $To
}

function Install-NativeWebSocket {
    $nw = $Lock.nativeWebSocket
    $nwTmp = Join-Path $env:TEMP ("NativeWebSocket-" + [guid]::NewGuid().ToString("n"))
    try {
        Write-Host "Fetching NativeWebSocket ($($nw.ref))..."
        git clone --branch $nw.ref --depth 1 $nw.repo $nwTmp
        $from = Join-Path $nwTmp $nw.sourceSubpath
        $to = Join-Path $Dest $nw.destSubpath
        if (Test-Path $to) {
            Remove-Item -Recurse -Force $to
        }
        Copy-Item -Recurse -Force $from $to
    }
    finally {
        if (Test-Path $nwTmp) {
            Remove-Item -Recurse -Force $nwTmp
        }
    }
}

function Fetch-ColyseusUnity {
    if (-not (Get-Command git -ErrorAction SilentlyContinue)) {
        Write-Error "git is required"
    }

    try {
        Write-Host "Cloning $Repo ..."
        if ($TargetRef) {
            git clone --depth 1 --branch $TargetRef $Repo $Tmp
        } else {
            git clone $Repo $Tmp
        }
        $sha = (git -C $Tmp rev-parse HEAD).Trim()
        $src = Join-Path $Tmp $SourceSub
        Write-Host "Installing io.colyseus.sdk -> $Dest"
        Copy-Tree $src $Dest
        Install-NativeWebSocket
        $resolvedRef = if ($TargetRef) { $TargetRef } elseif ($Update) { $Lock.ref } else { "HEAD" }
        Write-VersionFile $VersionFile $sha $resolvedRef
        Write-Host "OK io.colyseus.sdk @ $sha"
    }
    finally {
        if (Test-Path $Tmp) {
            Remove-Item -Recurse -Force $Tmp
        }
    }
}

Fetch-ColyseusUnity
Write-Host ""
Write-Host "Unity manifest references file:io.colyseus.sdk - reopen the project in Unity Hub."
