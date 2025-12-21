# PowerShell script to build the ISPF Designer on Windows
Write-Host "Building single-file executable for Windows..." -ForegroundColor Cyan

dotnet publish -c Release --self-contained true /p:PublishSingleFile=true -o Publish

if ($LASTEXITCODE -eq 0) {
    Write-Host "Build complete. Executable is in Publish/" -ForegroundColor Green
} else {
    Write-Host "Build failed!" -ForegroundColor Red
}
