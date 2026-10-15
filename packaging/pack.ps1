# Puts together the Microsoft Store package (MSIX) of SmartClip from SmartClip.exe, built first by build.cmd.
#
#   powershell -ExecutionPolicy Bypass -File packaging\pack.ps1
#
# Writes packaging\out\layout (the content of the package) and, when the Windows SDK is installed (makeappx and
# makepri), packaging\out\SmartClip-<version>.msix, ready for Partner Center: the Store signs it. Without the
# SDK the layout alone can be tried with Developer Mode on:
#   Add-AppxPackage -Register packaging\out\layout\AppxManifest.xml
# The images come from SmartClip.ico, the version from AssemblyVersion in src\Program.cs: nothing to keep in step.

param(
    [string]$Exe = (Join-Path $PSScriptRoot '..\SmartClip.exe'),
    [string]$Out = (Join-Path $PSScriptRoot 'out')
)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$root = Resolve-Path (Join-Path $PSScriptRoot '..')

# The version of the package has four numbers, and the Store wants the last one to be zero.
$found = Select-String -Path (Join-Path $root 'src\Program.cs') -Pattern 'AssemblyVersion\("(\d+)\.(\d+)\.(\d+)(?:\.(\d+))?"\)'
if (-not $found) { throw 'AssemblyVersion not found in src\Program.cs' }
$parts = $found.Matches[0].Groups
if ($parts[4].Success -and $parts[4].Value -ne '0') { throw 'The Store needs the fourth number of the version to be 0' }
$version = '{0}.{1}.{2}.0' -f $parts[1].Value, $parts[2].Value, $parts[3].Value

if (-not (Test-Path $Exe)) { throw "$Exe not found: run build.cmd first" }
$layout = Join-Path $Out 'layout'
if (Test-Path $layout) { Remove-Item $layout -Recurse -Force }
New-Item -ItemType Directory -Force (Join-Path $layout 'Assets') | Out-Null
Copy-Item $Exe (Join-Path $layout 'SmartClip.exe')

$manifest = Get-Content (Join-Path $PSScriptRoot 'AppxManifest.xml') -Raw -Encoding UTF8
$manifest = $manifest -replace '(<Identity[^>]*\sVersion=")[^"]*', "`${1}$version"
[IO.File]::WriteAllText((Join-Path $layout 'AppxManifest.xml'), $manifest, (New-Object Text.UTF8Encoding $false))
if ($manifest -match 'PLACEHOLDER') {
    Write-Warning 'Identity still has placeholder values: the package installs for testing, but the Store will refuse it (see the comment in AppxManifest.xml).'
}

# --- Images ---------------------------------------------------------------------------------------------------
# The largest frame of the icon, a PNG inside the .ico, and the small ones as they were drawn for their size.
$ico = Join-Path $root 'SmartClip.ico'
$bytes = [IO.File]::ReadAllBytes($ico)
$largest = $null
for ($i = 0; $i -lt [BitConverter]::ToUInt16($bytes, 4); $i++) {
    $entry = 6 + 16 * $i
    $side = if ($bytes[$entry] -eq 0) { 256 } else { $bytes[$entry] }
    if (-not $largest -or $side -gt $largest.Side) {
        $largest = @{ Side = $side; Length = [BitConverter]::ToInt32($bytes, $entry + 8); Offset = [BitConverter]::ToInt32($bytes, $entry + 12) }
    }
}
$stream = New-Object IO.MemoryStream(, $bytes[$largest.Offset..($largest.Offset + $largest.Length - 1)])
$big = New-Object Drawing.Bitmap ([Drawing.Image]::FromStream($stream))

# The icon on a transparent image of the given size, 'share' of its shorter side. A size the .ico has
# a frame of is taken as it was drawn: sharper than one shrunk from the largest.
function Save-Image([string]$name, [int]$width, [int]$height, [double]$share) {
    $side = [int][Math]::Round([Math]::Min($width, $height) * $share)
    $source = $big
    if ($side -lt $largest.Side) {
        $frame = New-Object Drawing.Icon($ico, $side, $side)
        if ($frame.Width -eq $side) { $source = $frame.ToBitmap() }
    }
    $image = New-Object Drawing.Bitmap($width, $height)
    $g = [Drawing.Graphics]::FromImage($image)
    $g.InterpolationMode = [Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.PixelOffsetMode = [Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $g.DrawImage($source, [int](($width - $side) / 2), [int](($height - $side) / 2), $side, $side)
    $g.Dispose()
    $image.Save((Join-Path $layout "Assets\$name"), [Drawing.Imaging.ImageFormat]::Png)
    $image.Dispose()
}

# Each image at 100% under its plain name, the larger scales qualified: makepri picks the right one. The
# tiles stop at 200%, beyond which the icon would have to be enlarged past its largest frame.
$tile = 0.6   # the icon on a tile of Start leaves it room around
foreach ($scale in 100, 125, 150, 200, 400) {
    $suffix = if ($scale -eq 100) { '' } else { ".scale-$scale" }
    $k = $scale / 100
    Save-Image "Square44x44Logo$suffix.png" (44 * $k) (44 * $k) 1
    Save-Image "StoreLogo$suffix.png" (50 * $k) (50 * $k) 1
    if ($scale -gt 200) { continue }
    Save-Image "Square150x150Logo$suffix.png" (150 * $k) (150 * $k) $tile
    Save-Image "Wide310x150Logo$suffix.png" (310 * $k) (150 * $k) $tile
}
# Taskbar, Start list and file dialogs ask for exact sizes; "unplated" is the same icon without a backplate.
foreach ($size in 16, 20, 24, 32, 40, 48, 64, 256) {
    Save-Image "Square44x44Logo.targetsize-$size.png" $size $size 1
    Save-Image "Square44x44Logo.targetsize-${size}_altform-unplated.png" $size $size 1
}

# --- Package --------------------------------------------------------------------------------------------------
$kits = Join-Path ${env:ProgramFiles(x86)} 'Windows Kits\10\bin'
$tools = Get-ChildItem $kits -Directory -Filter '10.*' -ErrorAction SilentlyContinue | Sort-Object { [Version]$_.Name } -Descending |
    ForEach-Object { Join-Path $_.FullName 'x64' } | Where-Object { Test-Path (Join-Path $_ 'makeappx.exe') } | Select-Object -First 1
if (-not $tools) {
    Write-Warning 'Windows SDK not found (makeappx, makepri): only the layout was written.'
    Write-Host "Layout: $layout"
    exit 0
}
$config = Join-Path $Out 'priconfig.xml'
& (Join-Path $tools 'makepri.exe') createconfig /cf $config /dq en-US /pv 10.0.0 /o | Out-Null
if ($LASTEXITCODE -ne 0) { throw 'makepri createconfig failed' }
# The configuration it writes splits the scales into resource packages of their own, which only a bundle has:
# in a single package their indexes would be left unread, and Windows would enlarge the 100% images. One index
# for every scale instead.
$priConfig = New-Object Xml.XmlDocument
$priConfig.Load($config)
$split = $priConfig.SelectSingleNode('//packaging')
if ($split) { [void]$split.ParentNode.RemoveChild($split) }
$priConfig.Save($config)
& (Join-Path $tools 'makepri.exe') new /pr $layout /cf $config /mn (Join-Path $layout 'AppxManifest.xml') /of (Join-Path $layout 'resources.pri') /o | Out-Null
if ($LASTEXITCODE -ne 0) { throw 'makepri new failed' }
$msix = Join-Path $Out "SmartClip-$version.msix"
& (Join-Path $tools 'makeappx.exe') pack /d $layout /p $msix /o | Out-Null
if ($LASTEXITCODE -ne 0) { throw 'makeappx pack failed' }
Write-Host "Package: $msix"
