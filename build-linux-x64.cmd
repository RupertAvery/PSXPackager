del /s /q .\build\linux-x64
dotnet publish .\PSXPackager\PSXPackager-linux.csproj -c Release -r linux-x64 -o .\build\linux-x64 /p:DebugType=None /p:DebugSymbols=false
copy README.MD .\build\linux-x64
