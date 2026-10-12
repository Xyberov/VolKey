# Генерирует Assets\VolKey.ico (16–256 px). Запуск: powershell -File tools\make-icon.ps1
Add-Type -AssemblyName System.Drawing

$sizes = 16, 20, 24, 32, 40, 48, 64, 256
$output = Join-Path $PSScriptRoot "..\Assets\VolKey.ico"

function New-IconBitmap([int]$size) {
    $s = [float]$size
    $bitmap = New-Object System.Drawing.Bitmap $size, $size, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bitmap)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality

    # Фон — скруглённый квадрат с вертикальным градиентом.
    $r = $s * 0.24
    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $path.AddArc(0, 0, $r * 2, $r * 2, 180, 90)
    $path.AddArc($s - $r * 2, 0, $r * 2, $r * 2, 270, 90)
    $path.AddArc($s - $r * 2, $s - $r * 2, $r * 2, $r * 2, 0, 90)
    $path.AddArc(0, $s - $r * 2, $r * 2, $r * 2, 90, 90)
    $path.CloseFigure()
    $rect = New-Object System.Drawing.RectangleF 0, 0, $s, $s
    $brush = New-Object System.Drawing.Drawing2D.LinearGradientBrush $rect, ([System.Drawing.Color]::FromArgb(58, 150, 221)), ([System.Drawing.Color]::FromArgb(0, 84, 158)), 90
    $g.FillPath($brush, $path)

    # Знак «следующий трек»: два треугольника и черта.
    $white = [System.Drawing.Brushes]::White
    $top = $s * 0.30; $bottom = $s * 0.70; $middle = $s * 0.50
    $g.FillPolygon($white, [System.Drawing.PointF[]]@(
        (New-Object System.Drawing.PointF ($s * 0.19), $top),
        (New-Object System.Drawing.PointF ($s * 0.19), $bottom),
        (New-Object System.Drawing.PointF ($s * 0.46), $middle)))
    $g.FillPolygon($white, [System.Drawing.PointF[]]@(
        (New-Object System.Drawing.PointF ($s * 0.44), $top),
        (New-Object System.Drawing.PointF ($s * 0.44), $bottom),
        (New-Object System.Drawing.PointF ($s * 0.71), $middle)))
    $g.FillRectangle($white, $s * 0.71, $top, [Math]::Max($s * 0.09, 1.5), $bottom - $top)

    $brush.Dispose(); $path.Dispose(); $g.Dispose()
    return $bitmap
}

function Get-DibBytes([System.Drawing.Bitmap]$bitmap) {
    $size = $bitmap.Width
    $data = $bitmap.LockBits((New-Object System.Drawing.Rectangle 0, 0, $size, $size),
        [System.Drawing.Imaging.ImageLockMode]::ReadOnly, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $pixels = New-Object byte[] ($size * $size * 4)
    [System.Runtime.InteropServices.Marshal]::Copy($data.Scan0, $pixels, 0, $pixels.Length)
    $bitmap.UnlockBits($data)

    $maskRow = [int][Math]::Floor(($size + 31) / 32) * 4
    $stream = New-Object System.IO.MemoryStream
    $w = New-Object System.IO.BinaryWriter $stream
    $w.Write([int]40); $w.Write([int]$size); $w.Write([int]($size * 2))
    $w.Write([int16]1); $w.Write([int16]32); $w.Write([int]0)
    $w.Write([int]($size * $size * 4 + $maskRow * $size))
    $w.Write([int]0); $w.Write([int]0); $w.Write([int]0); $w.Write([int]0)
    for ($y = $size - 1; $y -ge 0; $y--) { $w.Write($pixels, $y * $size * 4, $size * 4) }
    $w.Write((New-Object byte[] ($maskRow * $size)))
    $w.Flush()
    return $stream.ToArray()
}

$images = foreach ($size in $sizes) {
    $bitmap = New-IconBitmap $size
    if ($size -ge 256) {
        $png = New-Object System.IO.MemoryStream
        $bitmap.Save($png, [System.Drawing.Imaging.ImageFormat]::Png)
        $bytes = $png.ToArray()
    } else {
        $bytes = Get-DibBytes $bitmap
    }
    $bitmap.Dispose()
    , @($size, $bytes)
}

New-Item -ItemType Directory -Force (Split-Path $output) | Out-Null
$file = [System.IO.File]::Create($output)
$w = New-Object System.IO.BinaryWriter $file
$w.Write([int16]0); $w.Write([int16]1); $w.Write([int16]$images.Count)
$offset = 6 + 16 * $images.Count
foreach ($image in $images) {
    $size = $image[0]; $bytes = $image[1]
    $dim = if ($size -ge 256) { 0 } else { $size }
    $w.Write([byte]$dim); $w.Write([byte]$dim); $w.Write([byte]0); $w.Write([byte]0)
    $w.Write([int16]1); $w.Write([int16]32); $w.Write([int]$bytes.Length); $w.Write([int]$offset)
    $offset += $bytes.Length
}
foreach ($image in $images) { $w.Write([byte[]]$image[1]) }
$w.Close()
Write-Host "Saved $output"
