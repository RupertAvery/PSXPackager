SET BUILD_PATH=.\build\psxpackager-win-x64
dotnet publish .\PSXPackager\PSXPackager-windows.csproj -c Release -r win-x64 -o %BUILD_PATH% /p:DebugType=None /p:DebugSymbols=false
cp .\Popstation.Database\Resources\gameInfo.db %BUILD_PATH%\Resources\gameInfo.db
cp README.md %BUILD_PATH%
