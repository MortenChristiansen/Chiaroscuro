# Build Angular app
Write-Host "Building Angular app..."
Push-Location ..\chrome-app
npm install
npm run build
Pop-Location

# Copy Angular build output to BrowserHost/chrome-app
$chromeAppDist = "..\chrome-app\dist\chrome-app\browser"
$targetDir = "chrome-app"
if (Test-Path $targetDir) { Remove-Item $targetDir -Recurse -Force }
Copy-Item $chromeAppDist $targetDir -Recurse

# Create terminal folder for client-side rendered route (uses xterm.js which requires browser)
$terminalDir = Join-Path $targetDir "terminal"
New-Item -ItemType Directory -Path $terminalDir -Force | Out-Null
Copy-Item (Join-Path $targetDir "index.csr.html") (Join-Path $terminalDir "index.html")

# Build and publish .NET app
Write-Host "Publishing .NET app..."
dotnet publish BrowserHost.csproj -f net10.0-windows -r win-x64 -p:PublishSingleFile=true --self-contained true -o "../publish" -c Release
