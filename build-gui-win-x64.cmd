SET BUILD_PATH=.\build\PsxPackagerGUI
dotnet publish .\PSXPackagerGUI\PSXPackagerGUI.csproj -c Release -r win-x64 -o %BUILD_PATH% --no-self-contained /p:PublishSingleFile=true /p:PublishReadyToRun=false /p:DebugType=None /p:DebugSymbols=false
cp .\Popstation.Database\Resources\gameInfo.db %BUILD_PATH%\Resources\gameInfo.db
