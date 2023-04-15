del /s /q .\build\PsxPackagerGUI
dotnet publish .\PSXPackagerGUI\PSXPackagerGUI.csproj -c Release -r win-x64 -o .\build\PsxPackagerGUI --no-self-contained /p:PublishSingleFile=true /p:PublishReadyToRun=false /p:DebugType=None /p:DebugSymbols=false
xcopy .\libs .\build\PsxPackagerGUI /s /y
