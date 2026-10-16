# Fetches the Store package (MSIX) built by the Release workflow on GitHub into store\upload, beside the listing
# that make-listing.ps1 writes there, so that everything Partner Center wants is in one folder:
#
#   powershell -ExecutionPolicy Bypass -File store\get-package.ps1            (from the runs already made)
#   powershell -ExecutionPolicy Bypass -File store\get-package.ps1 -Build     (starts the workflow first)
#
# The package must be of the version in src\Program.cs: a push that changes it makes one, and so does the
# workflow started by hand (-Build), from what is pushed to main. Needs gh, logged in.

param([switch]$Build)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot
$upload = Join-Path $PSScriptRoot 'upload'
$artifact = 'SmartClip-msix'

# The name pack.ps1 gives the package: the version with four numbers, the last one zero.
$found = Select-String -Path (Join-Path $root 'src\Program.cs') -Pattern 'AssemblyVersion\("(\d+)\.(\d+)\.(\d+)(?:\.(\d+))?"\)'
if (-not $found) { throw 'AssemblyVersion not found in src\Program.cs' }
$parts = $found.Matches[0].Groups
$name = 'SmartClip-{0}.{1}.{2}.0.msix' -f $parts[1].Value, $parts[2].Value, $parts[3].Value

if ($Build) {
    gh workflow run release.yml --ref main
    if ($LASTEXITCODE -ne 0) { throw 'gh could not start the workflow' }
    Start-Sleep -Seconds 5   # until GitHub lists the new run
    $run = gh run list --workflow release.yml --event workflow_dispatch --limit 1 --json databaseId | ConvertFrom-Json
    gh run watch $run[0].databaseId --exit-status | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "the workflow failed: gh run view $($run[0].databaseId)" }
}

# From the newest run: one that only found the release already there made no package, and is passed over.
$runs = gh run list --workflow release.yml --status success --limit 15 --json databaseId | ConvertFrom-Json
$temp = Join-Path ([IO.Path]::GetTempPath()) ('SmartClip package ' + [Guid]::NewGuid())
try {
    foreach ($run in $runs) {
        if (Test-Path $temp) { Remove-Item $temp -Recurse -Force }
        # A run without the package makes gh complain: in PowerShell 5.1 that would stop everything.
        $ErrorActionPreference = 'Continue'
        gh run download $run.databaseId -n $artifact -D $temp 2>$null | Out-Null
        $ErrorActionPreference = 'Stop'
        if ($LASTEXITCODE -ne 0 -or -not (Test-Path (Join-Path $temp $name))) { continue }
        New-Item -ItemType Directory -Force $upload | Out-Null
        Get-ChildItem $upload -Filter '*.msix' | Remove-Item -Force   # an older version must not be uploaded by mistake
        Copy-Item (Join-Path $temp $name) $upload
        Write-Host "Package: $(Join-Path $upload $name) (run $($run.databaseId))"
        exit 0
    }
}
finally {
    if (Test-Path $temp) { Remove-Item $temp -Recurse -Force }
}
throw "no run has made $name yet: push the new version, or start the workflow with -Build"
