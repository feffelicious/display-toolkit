<#
.SYNOPSIS
    Regenerates the installer's artwork: License.rtf from LICENSE, and the WiX dialog bitmaps from the app icon.
    Run it after changing either; the results are committed.
#>
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$root = Resolve-Path (Join-Path $PSScriptRoot '..\..')

# --- License.rtf: the MIT license, one paragraph per block of text.
$text = Get-Content (Join-Path $root 'LICENSE') -Raw
$escaped = $text.Replace('\', '\\').Replace('{', '\{').Replace('}', '\}') -replace '[^\x00-\x7F]', { '\u{0}?' -f [int][char]$_.Value }
$paragraphs = ($escaped -split "(\r?\n){2,}" | Where-Object { $_.Trim() }) | ForEach-Object { ($_ -replace '\r?\n', ' ').Trim() + '\par' }
$rtf = "{\rtf1\ansi\deff0{\fonttbl{\f0 Segoe UI;}}\fs18`r`n" + ($paragraphs -join "`r`n\par`r`n") + "`r`n}"
Set-Content (Join-Path $PSScriptRoot 'License.rtf') $rtf -Encoding ascii -NoNewline

# --- The 256 px icon image from the .ico (stored as PNG).
$ico = [IO.File]::ReadAllBytes((Join-Path $root 'src\DisplayToolkit.App\Assets\DisplayToolkit.ico'))
$count = [BitConverter]::ToInt16($ico, 4)
$icon = $null
for ($i = 0; $i -lt $count; $i++) {
    $entry = 6 + (16 * $i)
    if ($ico[$entry] -eq 0) {
        $length = [BitConverter]::ToInt32($ico, $entry + 8)
        $offset = [BitConverter]::ToInt32($ico, $entry + 12)
        $icon = [Drawing.Image]::FromStream([IO.MemoryStream]::new($ico, $offset, $length))
    }
}

function Save-Bitmap([int]$width, [int]$height, [scriptblock]$draw, [string]$name) {
    $bitmap = [Drawing.Bitmap]::new($width, $height, [Drawing.Imaging.PixelFormat]::Format24bppRgb)
    $graphics = [Drawing.Graphics]::FromImage($bitmap)
    $graphics.SmoothingMode = 'AntiAlias'
    $graphics.InterpolationMode = 'HighQualityBicubic'
    $graphics.Clear([Drawing.Color]::White)
    & $draw $graphics
    $bitmap.Save((Join-Path $PSScriptRoot $name), [Drawing.Imaging.ImageFormat]::Bmp)
    $graphics.Dispose(); $bitmap.Dispose()
}

# Welcome and finish pages: a soft panel on the left with the icon; the text sits on the white right side.
Save-Bitmap 493 312 {
    param($g)
    $panel = [Drawing.Rectangle]::new(0, 0, 164, 312)
    $g.FillRectangle([Drawing.Drawing2D.LinearGradientBrush]::new($panel, [Drawing.Color]::FromArgb(236, 242, 255), [Drawing.Color]::FromArgb(243, 236, 255), 90), $panel)
    $g.DrawImage($icon, 34, 100, 96, 96)
} 'Dialog.bmp'

# The other pages: white banner, title text on the left, a small icon on the right.
Save-Bitmap 493 58 {
    param($g)
    $g.DrawImage($icon, 441, 11, 36, 36)
} 'Banner.bmp'

Write-Host 'Wrote License.rtf, Dialog.bmp and Banner.bmp'
