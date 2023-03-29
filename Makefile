.PHONY:	all build clean
all:
	$(MAKE) clean
	$(MAKE) build

.PHONY:	build-gui-win-x64 build-win-x64 build-linux-x64 build-osx-x64
build:
	$(MAKE) build-gui-win-x64
	$(MAKE) build-win-x64
	$(MAKE) build-linux-x64
	$(MAKE) build-osx-x64

.PHONY:	clean-gui-win-x64 clean-win-x64 clean-linux-x64 clean-osx-x64
clean:	clean-gui-win-x64 clean-win-x64 clean-linux-x64 clean-osx-x64


build-gui-win-x64:
	dotnet publish ./PSXPackagerGUI/PSXPackagerGUI.csproj -c Release --self-contained -r win-x64 -o ./build/PSXPackagerGUI /p:PublishSingleFile=true /p:PublishReadyToRun=false /p:DefineConstants="SEVENZIP" /p:DebugType=None /p:DebugSymbols=false /p:EnableWindowsTargeting=true
	cp -a ./libs ./build/PSXPackagerGUI
	cp README.MD ./build/PSXPackagerGUI

build-win-x64:
	dotnet publish ./PSXPackager/PSXPackager-windows.csproj -c Release --self-contained -r win-x64 -o ./build/win-x64 /p:DefineConstants="SEVENZIP" /p:DebugType=None /p:DebugSymbols=false /p:EnableWindowsTargeting=true
	cp -a ./libs ./build/win-x64
	cp README.MD ./build/win-x64

build-linux-x64:
	dotnet publish ./PSXPackager/PSXPackager-linux.csproj -c Release --self-contained -r linux-x64 -o ./build/linux-x64 /p:DebugType=None /p:DebugSymbols=false
	cp README.MD ./build/linux-x64

build-osx-x64:
	dotnet publish ./PSXPackager/PSXPackager-linux.csproj -c Release --self-contained -r osx-x64 -o ./build/osx-x64 /p:DebugType=None /p:DebugSymbols=false
	cp README.MD ./build/osx-x64


clean-gui-win-x64:
	rm -rf ./build/PsxPackagerGUI

clean-win-x64:
	rm -rf ./build/win-x64

clean-linux-x64:
	rm -rf ./build/linux-x64

clean-osx-x64:
	rm -rf ./build/osx-x64

