# Prepares the folder to import into Partner Center (Store listings > Import listings > Import folder), with the
# texts of every language and their pictures:
#
#   powershell -ExecutionPolicy Bypass -File store\make-listing.ps1 [<listing exported from Partner Center>.csv]
#
# The texts come from store\README.md, where each language has its section ("## English (en-US)" and so on) with,
# in this order, code blocks for name, short description, description, features, search terms and what is new in
# this version (that one optional), then the table of the screenshot captions; what all listings share is in
# "## In tutte le schede". The rows of the file
# come from the export, so that Partner Center gets back exactly the fields it knows. Writes
# store\upload\SmartClip-listing: the .csv and a copy of the pictures, at the paths the .csv names. Beside it
# get-package.ps1 puts the package: store\upload holds all there is to give Partner Center.
# Without an export, the .csv written the time before serves as one: it is read before the folder is written anew.

param([string]$Exported)
$ErrorActionPreference = 'Stop'
$store = $PSScriptRoot
$rootName = 'SmartClip-listing'   # the paths in the file start with the name of the folder that is imported
$out = Join-Path $store "upload\$rootName"
if (-not $Exported) { $Exported = Join-Path $out 'listing.csv' }
if (-not (Test-Path $Exported)) { throw "$Exported not found: export the listing from Partner Center and pass it" }

$readme = [IO.File]::ReadAllText((Join-Path $store 'README.md')) -replace "`r`n", "`n"
$fence = '```'

# The part of the README under a "## " heading, up to the next one.
function Section([string]$heading) {
    $start = $readme.IndexOf("`n## $heading")
    if ($start -lt 0) { throw "README: section '$heading' not found" }
    $next = $readme.IndexOf("`n## ", $start + 4)
    if ($next -lt 0) { $next = $readme.Length }
    return $readme.Substring($start, $next - $start)
}

function Blocks([string]$text) {
    return [regex]::Matches($text, "$fence\n(.*?)\n$fence", 'Singleline') | ForEach-Object { $_.Groups[1].Value }
}

# What every listing shares: the value between backticks after its label.
$shared = Section 'In tutte le schede'
function Shared([string]$label) {
    $m = [regex]::Match($shared, [regex]::Escape($label) + '\*\*: `([^`]+)`')
    if (-not $m.Success) { throw "README: '$label' not found" }
    return $m.Groups[1].Value
}
$common = @{
    CopyrightTrademarkInformation = Shared 'Copyright e marchi'
    AdditionalLicenseTerms        = Shared 'Termini di licenza aggiuntivi'
    DevStudio                     = Shared 'Sviluppato da'
}

# Each language: its column in the file and the folder of its pictures.
$languages = [ordered]@{}
foreach ($m in [regex]::Matches($readme, "\n## [^\n]*\(([a-z]{2})-([A-Z]{2})\)[^\n]*")) {
    $code = $m.Groups[1].Value
    $column = ($code + '-' + $m.Groups[2].Value).ToLower()
    $text = Section ($m.Value.Trim().Substring(3))
    $blocks = @(Blocks $text)
    if ($blocks.Count -lt 5) { throw "README: '$column' has $($blocks.Count) code blocks, not 5" }
    $values = @{
        Title            = $blocks[0]
        ShortDescription = $blocks[1]
        Description      = $blocks[2]
    }
    if ($blocks.Count -gt 5) { $values['ReleaseNotes'] = $blocks[5] }
    $features = @($blocks[3] -split "`n" | Where-Object { $_.Trim() })
    for ($i = 0; $i -lt $features.Count; $i++) { $values["Feature$($i + 1)"] = $features[$i] }
    $terms = @($blocks[4] -split "`n" | Where-Object { $_.Trim() })
    for ($i = 0; $i -lt $terms.Count; $i++) { $values["SearchTerm$($i + 1)"] = $terms[$i] }
    $shots = [regex]::Matches($text, '\| `(\d-[a-z-]+\.png)` \| ([^|\n]+?) \|')
    if ($shots.Count -eq 0) { throw "README: '$column' has no screenshot captions" }
    for ($i = 0; $i -lt $shots.Count; $i++) {
        $values["DesktopScreenshot$($i + 1)"] = "$rootName/images/$code/" + $shots[$i].Groups[1].Value
        $values["DesktopScreenshotCaption$($i + 1)"] = $shots[$i].Groups[2].Value.Trim()
    }
    $values['StoreLogo300x300'] = "$rootName/images/tile-300.png"
    foreach ($k in $common.Keys) { $values[$k] = $common[$k] }
    $languages[$column] = @{ Code = $code; Values = $values }
}
if ($languages.Count -eq 0) { throw 'README: no language sections found' }

# The rows of the export, filled in; the empty rows at its end are left out.
function Cell([string]$value) {
    if ($value -match '[",\r\n]') { return '"' + $value.Replace('"', '""') + '"' }
    return $value
}
# Read as CSV, not line by line: a filled-in listing has descriptions on several lines, between quotes.
Add-Type -AssemblyName Microsoft.VisualBasic
$parser = New-Object Microsoft.VisualBasic.FileIO.TextFieldParser($Exported, [Text.Encoding]::UTF8)
$parser.SetDelimiters(',')
$parser.HasFieldsEnclosedInQuotes = $true
$parser.TrimWhiteSpace = $false
$records = New-Object System.Collections.Generic.List[object]
while (-not $parser.EndOfData) { $records.Add($parser.ReadFields()) }
$parser.Close()
$header = $records[0]
$columns = @($header[4..($header.Count - 1)] | Where-Object { $_ })
foreach ($column in $languages.Keys) { if ($columns -notcontains $column) { throw "the export has no column '$column'" } }
$rows = New-Object System.Collections.Generic.List[string]
$rows.Add(($header[0..3] + $columns) -join ',')
foreach ($parts in $records.GetRange(1, $records.Count - 1)) {
    if (-not $parts[0]) { continue }
    $cells = @($parts[0], $parts[1], $parts[2], '')   # Field, ID, Type, and nothing in "default"
    foreach ($column in $columns) {
        $value = $null
        if ($languages.Contains($column)) { $value = $languages[$column].Values[$parts[0]] }
        if ($null -eq $value) { $value = $parts[4 + [array]::IndexOf($columns, $column)] }   # as exported
        $cells += Cell ($value -replace "`r?`n", "`r`n")
    }
    $rows.Add($cells -join ',')
}

if (Test-Path $out) { Remove-Item $out -Recurse -Force }
New-Item -ItemType Directory -Force (Join-Path $out 'images') | Out-Null
Copy-Item (Join-Path $store 'images\tile-300.png') (Join-Path $out 'images')
foreach ($language in $languages.Values) {
    Copy-Item (Join-Path $store "images\$($language.Code)") (Join-Path $out 'images') -Recurse
}
[IO.File]::WriteAllText((Join-Path $out 'listing.csv'), (($rows -join "`r`n") + "`r`n"), (New-Object Text.UTF8Encoding $true))
Write-Host "Folder to import: $out"
Write-Host ("Languages: " + ($languages.Keys -join ', '))
