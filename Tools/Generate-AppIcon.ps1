$ErrorActionPreference = "Stop"

$assetDir = Join-Path $PSScriptRoot "..\Assets"
New-Item -ItemType Directory -Force -Path $assetDir | Out-Null

Add-Type -AssemblyName System.Drawing

function New-IconBitmap {
    param([int]$Size)

    $bitmap = New-Object System.Drawing.Bitmap $Size, $Size, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bitmap)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.Clear([System.Drawing.Color]::Transparent)

    $scale = $Size / 256.0
    $g.ScaleTransform($scale, $scale)

    $bgRect = New-Object System.Drawing.Rectangle 18, 18, 220, 220
    $bgPath = New-Object System.Drawing.Drawing2D.GraphicsPath
    $radius = 48
    $bgPath.AddArc($bgRect.X, $bgRect.Y, $radius, $radius, 180, 90)
    $bgPath.AddArc($bgRect.Right - $radius, $bgRect.Y, $radius, $radius, 270, 90)
    $bgPath.AddArc($bgRect.Right - $radius, $bgRect.Bottom - $radius, $radius, $radius, 0, 90)
    $bgPath.AddArc($bgRect.X, $bgRect.Bottom - $radius, $radius, $radius, 90, 90)
    $bgPath.CloseFigure()

    $bgBrush = New-Object System.Drawing.Drawing2D.LinearGradientBrush $bgRect, ([System.Drawing.Color]::FromArgb(18,54,74)), ([System.Drawing.Color]::FromArgb(15,122,106)), 45
    $g.FillPath($bgBrush, $bgPath)

    $panelRect = New-Object System.Drawing.Rectangle 66, 58, 124, 116
    $panelPath = New-Object System.Drawing.Drawing2D.GraphicsPath
    $panelRadius = 20
    $panelPath.AddArc($panelRect.X, $panelRect.Y, $panelRadius, $panelRadius, 180, 90)
    $panelPath.AddArc($panelRect.Right - $panelRadius, $panelRect.Y, $panelRadius, $panelRadius, 270, 90)
    $panelPath.AddArc($panelRect.Right - $panelRadius, $panelRect.Bottom - $panelRadius, $panelRadius, $panelRadius, 0, 90)
    $panelPath.AddArc($panelRect.X, $panelRect.Bottom - $panelRadius, $panelRadius, $panelRadius, 90, 90)
    $panelPath.CloseFigure()
    $panelBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(230, 14, 52, 67))
    $panelPen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(96, 224, 196)), 6
    $g.FillPath($panelBrush, $panelPath)
    $g.DrawPath($panelPen, $panelPath)

    $linePen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(220, 185, 255, 241)), 9
    $linePen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
    $linePen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
    $g.DrawLine($linePen, 84, 88, 172, 88)
    $g.DrawLine($linePen, 84, 112, 154, 112)
    $g.DrawLine($linePen, 84, 136, 164, 136)

    $goldBrush = New-Object System.Drawing.Drawing2D.LinearGradientBrush (New-Object System.Drawing.Rectangle 86, 88, 88, 104), ([System.Drawing.Color]::FromArgb(255, 224, 138)), ([System.Drawing.Color]::FromArgb(184, 121, 24)), 45
    $goldPen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, 240, 181)), 5
    $bellPath = New-Object System.Drawing.Drawing2D.GraphicsPath
    $bellPath.AddArc(98, 112, 60, 76, 180, 180)
    $bellPath.AddBezier(158, 150, 166, 170, 150, 190, 128, 190)
    $bellPath.AddBezier(128, 190, 106, 190, 90, 170, 98, 150)
    $bellPath.CloseFigure()
    $g.FillPath($goldBrush, $bellPath)
    $g.DrawPath($goldPen, $bellPath)

    $topBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(245, 190, 74))
    $g.FillEllipse($topBrush, 111, 94, 34, 28)
    $g.DrawEllipse($goldPen, 111, 94, 34, 28)

    $clapPen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, 231, 163)), 8
    $clapPen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
    $clapPen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
    $g.DrawArc($clapPen, 113, 184, 30, 24, 20, 140)

    $sparkBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(65, 255, 208))
    $g.FillEllipse($sparkBrush, 173, 59, 22, 22)
    $sparkPen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(150, 255, 223)), 5
    $g.DrawLine($sparkPen, 184, 40, 184, 31)
    $g.DrawLine($sparkPen, 184, 101, 184, 92)
    $g.DrawLine($sparkPen, 162, 70, 153, 70)
    $g.DrawLine($sparkPen, 215, 70, 206, 70)

    $g.Dispose()
    return $bitmap
}

$sizes = @(16, 24, 32, 48, 64, 128, 256)
$pngData = @()
foreach ($size in $sizes) {
    $bitmap = New-IconBitmap -Size $size
    $pngPath = Join-Path $assetDir "windows-monitor-$size.png"
    $bitmap.Save($pngPath, [System.Drawing.Imaging.ImageFormat]::Png)
    $stream = New-Object System.IO.MemoryStream
    $bitmap.Save($stream, [System.Drawing.Imaging.ImageFormat]::Png)
    $pngData += [pscustomobject]@{ Size = $size; Bytes = $stream.ToArray() }
    $stream.Dispose()
    $bitmap.Dispose()
}

$icoPath = Join-Path $assetDir "windows-monitor.ico"
$fs = [System.IO.File]::Create($icoPath)
$writer = New-Object System.IO.BinaryWriter $fs
$writer.Write([UInt16]0)
$writer.Write([UInt16]1)
$writer.Write([UInt16]$pngData.Count)

$offset = 6 + (16 * $pngData.Count)
foreach ($entry in $pngData) {
    $dimension = if ($entry.Size -eq 256) { 0 } else { $entry.Size }
    $writer.Write([byte]$dimension)
    $writer.Write([byte]$dimension)
    $writer.Write([byte]0)
    $writer.Write([byte]0)
    $writer.Write([UInt16]1)
    $writer.Write([UInt16]32)
    $writer.Write([UInt32]$entry.Bytes.Length)
    $writer.Write([UInt32]$offset)
    $offset += $entry.Bytes.Length
}

foreach ($entry in $pngData) {
    $writer.Write($entry.Bytes)
}

$writer.Dispose()
$fs.Dispose()

Write-Host "Generated $icoPath"
