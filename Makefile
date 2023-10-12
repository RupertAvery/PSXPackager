WINDOWS_PROPERTIES=-p:EnableWindowsTargeting=true -p:PublishTrimmed=false
SELF_CONTAINED_PROPERTIES=--self-contained -p:PublishSingleFile=true -p:SelfContained=true -p:PublishTrimmed=true -p:InvariantGlobalization=true -p:EnableCompressionInSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=None -p:DebugSymbols=false

.PHONY:	all build clean test

all:
	$(MAKE) clean
	$(MAKE) build

.PHONY: build-linux build-osx
build:
	# $(MAKE) build-win
	$(MAKE) build-linux
	$(MAKE) build-osx

.PHONY: build-gui-win-x64 build-win-x64
build-win:
	$(MAKE) build-gui-win-x64
	$(MAKE) build-win-x64

.PHONY: build-linux-x64 build-linux-arm build-linux-arm64
build-linux:
	$(MAKE) build-linux-x64
	$(MAKE) build-linux-arm
	$(MAKE) build-linux-arm64

.PHONY: build-osx-x64 build-osx-arm64
build-osx:
	$(MAKE) build-osx-x64
	$(MAKE) build-osx-arm64

build-gui-win-x64:
	dotnet publish ./PSXPackagerGUI/PSXPackagerGUI.csproj -c Release -r win-x64 -o ./build/PsxPackagerGUI $(SELF_CONTAINED_PROPERTIES) $(WINDOWS_PROPERTIES)
	cp ./Popstation.Database/Resources/gameInfo.db ./build/PsxPackagerGUI/Resources/gameInfo.db

build-win-x64:
	$(eval RID := $(subst build-,,$(@)))
	dotnet publish ./PSXPackager/PSXPackager-windows.csproj -c Release -r ${RID} -o ./build/psxpackager-${RID} $(SELF_CONTAINED_PROPERTIES) $(WINDOWS_PROPERTIES)
	cp ./Popstation.Database/Resources/gameInfo.db ./build/psxpackager-${RID}/Resources/gameInfo.db
	cp README.md ./build/psxpackager-${RID}

build-linux-x64 build-linux-arm build-linux-arm64 build-osx-x64 build-osx-arm64:
	$(eval RID := $(subst build-,,$(@)))
	dotnet publish ./PSXPackager/PSXPackager-linux.csproj   -c Release -r ${RID} -o ./build/psxpackager-${RID} $(SELF_CONTAINED_PROPERTIES)
	cp ./Popstation.Database/Resources/gameInfo.db ./build/psxpackager-${RID}/Resources/gameInfo.db
	cp README.md ./build/psxpackager-${RID}

.PHONY: clean-win clean-linux clean-osx
clean:
	# $(MAKE) clean-win
	$(MAKE) clean-linux
	$(MAKE) clean-osx

.PHONY: clean-gui-win-x64 clean-win-x64
clean-win:
	$(MAKE) clean-gui-win-x64
	$(MAKE) clean-win-x64

.PHONY: clean-linux-x64 clean-linux-arm clean-linux-arm64
clean-linux:
	$(MAKE) clean-linux-x64
	$(MAKE) clean-linux-arm
	$(MAKE) clean-linux-arm64

.PHONY: clean-osx-x64 clean-osx-arm64
clean-osx:
	$(MAKE) clean-osx-x64
	$(MAKE) clean-osx-arm64

clean-gui-win-x64:
	rm -rf ./build/PsxPackagerGUI

clean-win-x64 clean-linux-x64 clean-linux-arm clean-linux-arm64 clean-osx-x64 clean-osx-arm64:
	$(eval RID := $(subst clean-,,$(@)))
	rm -rf ./build/${RID}

test:
	dotnet test PSXPackager.Tests

