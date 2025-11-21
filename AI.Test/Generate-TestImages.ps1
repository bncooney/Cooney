# Generate-TestImages.ps1
# Generates PNG test images of various dimensions using ffmpeg

# Get the directory where the script is located
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$DataDir = Join-Path $ScriptDir "Data"

# Ensure Data directory exists
if (-not (Test-Path $DataDir)) {
    New-Item -ItemType Directory -Path $DataDir -Force | Out-Null
}

Write-Host "Generating test images in: $DataDir" -ForegroundColor Green

# Define test image dimensions (width x height)
$imageDimensions = @(
    @{Width=640; Height=480; Name="VGA"},
    @{Width=800; Height=600; Name="SVGA"},
    @{Width=1024; Height=768; Name="XGA"},
    @{Width=1280; Height=720; Name="HD"},
    @{Width=1920; Height=1080; Name="FullHD"},
    @{Width=2560; Height=1440; Name="2K"},
    @{Width=3840; Height=2160; Name="4K"},
    @{Width=512; Height=512; Name="Square_512"},
    @{Width=1024; Height=1024; Name="Square_1024"},
    @{Width=300; Height=200; Name="Small"},
    @{Width=400; Height=300; Name="Medium_Small"}
)

$successCount = 0
$failCount = 0

foreach ($dim in $imageDimensions) {
    $outputFile = Join-Path $DataDir "$($dim.Name)_$($dim.Width)x$($dim.Height).png"

    Write-Host "Creating: $($dim.Name) ($($dim.Width)x$($dim.Height))..." -NoNewline

    # Generate a colored gradient image with text overlay using ffmpeg
    # This creates a visually interesting test image with the dimensions labeled
    $ffmpegArgs = @(
        "-f", "lavfi",
        "-i", "color=c=0x4A90E2:s=$($dim.Width)x$($dim.Height):d=1",
        "-f", "lavfi",
        "-i", "color=c=0xE94B3C:s=$($dim.Width)x$($dim.Height):d=1",
        "-filter_complex", "[0:v][1:v]blend=all_expr='A*(1-T)+B*T':shortest=1,drawtext=fontsize=48:fontcolor=white:x=(w-text_w)/2:y=(h-text_h)/2:text='$($dim.Width)x$($dim.Height)'",
        "-frames:v", "1",
        "-y",
        $outputFile
    )

    try {
        $process = Start-Process -FilePath "ffmpeg" -ArgumentList $ffmpegArgs -NoNewWindow -Wait -PassThru -RedirectStandardError "$env:TEMP\ffmpeg_error.txt"

        if ($process.ExitCode -eq 0 -and (Test-Path $outputFile)) {
            $fileSize = (Get-Item $outputFile).Length
            Write-Host " OK ($([math]::Round($fileSize/1KB, 2)) KB)" -ForegroundColor Green
            $successCount++
        } else {
            Write-Host " FAILED" -ForegroundColor Red
            $failCount++
        }
    } catch {
        Write-Host " ERROR: $_" -ForegroundColor Red
        $failCount++
    }
}

Write-Host "`nGeneration complete!" -ForegroundColor Cyan
Write-Host "Success: $successCount" -ForegroundColor Green
Write-Host "Failed: $failCount" -ForegroundColor $(if ($failCount -gt 0) { "Red" } else { "Gray" })
Write-Host "`nTest images saved to: $DataDir" -ForegroundColor Cyan
