# Build Angular app
Write-Host "Building Angular app..."
Push-Location ..\chrome-app
npm install
npm run build
Pop-Location

# Copy Angular build output to BrowserHost/chrome-app
$chromeAppDist = "..\chrome-app\dist\chrome-app\browser"
$targetDir = "..\publish\chrome-app"
if (Test-Path $targetDir) { Remove-Item $targetDir -Recurse -Force }
Copy-Item $chromeAppDist $targetDir -Recurse

# Build and publish .NET app
Write-Host "Publishing .NET app..."
dotnet publish BrowserHost.csproj -f net9.0-windows -r win-x64 -p:PublishSingleFile=true --self-contained true -o "../publish" -c Release
