del /s /q .\build\linux-x64
dotnet publish .\PSXPackager\PSXPackager-windows.csproj -c Release -r win-x64 -o .\build\win-x64 /p:DebugType=None /p:DebugSymbols=false
copy README.md .\build\win-x64
