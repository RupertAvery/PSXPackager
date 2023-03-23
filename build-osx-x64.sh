rm -rf ./build/osx-x64
dotnet publish ./PSXPackager/PSXPackager-linux.csproj -c Release -r osx-x64 -o ./build/osx-x64 /p:DebugType=None /p:DebugSymbols=false
cp README.MD ./build/osx-x64
