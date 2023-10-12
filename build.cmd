SET SELF_CONTAINED_PROPERTIES=--self-contained -p:PublishSingleFile=true -p:SelfContained=true -p:PublishTrimmed=true -p:InvariantGlobalization=true -p:EnableCompressionInSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=None -p:DebugSymbols=false
SET BUILD_PATH=./build/psxpackager-%1
dotnet publish ./PSXPackager/PSXPackager-linux.csproj -c Release -r %1 -o %BUILD_PATH% %SELF_CONTAINED_PROPERTIES%
cp ./Popstation.Database/Resources/gameInfo.db %BUILD_PATH%/Resources/gameInfo.db
cp README.md %BUILD_PATH%
