WINDOWS_PROPERTIES=-p:EnableWindowsTargeting=true -p:PublishTrimmed=false
SELF_CONTAINED_PROPERTIES=-p:PublishSingleFile=true -p:SelfContained=true -p:PublishTrimmed=true -p:EnableCompressionInSingleFile=true -p:DebugType=None -p:DebugSymbols=false

.PHONY:	all build clean test

all:
	$(MAKE) clean
	$(MAKE) build

.PHONY:	build-gui-win-x64 build-win-x64 build-linux-x64 build-linux-arm build-linux-arm64 build-osx-x64 build-osx-arm64
build:
	$(MAKE) build-gui-win-x64
	$(MAKE) build-win-x64
	$(MAKE) build-linux-x64
	$(MAKE) build-linux-arm
	$(MAKE) build-linux-arm64
	$(MAKE) build-osx-x64
	$(MAKE) build-osx-arm64

.PHONY:	clean-gui-win-x64 clean-win-x64 clean-linux-x64 clean-linux-arm clean-linux-arm64 clean-osx-x64 clean-osx-arm64
clean:	clean-gui-win-x64 clean-win-x64 clean-linux-x64 clean-linux-arm clean-linux-arm64 clean-osx-x64 clean-osx-arm64


build-gui-win-x64:
	dotnet publish ./PSXPackagerGUI/PSXPackagerGUI.csproj -c Release -r win-x64 -o ./build/PsxPackagerGUI $(SELF_CONTAINED_PROPERTIES) $(WINDOWS_PROPERTIES)
	cp -a ./libs/* ./build/PsxPackagerGUI

build-win-x64:
	$(eval RID := $(subst build-,,$(@)))
	dotnet publish ./PSXPackager/PSXPackager-windows.csproj -c Release -r ${RID} -o ./build/${RID} $(SELF_CONTAINED_PROPERTIES) $(WINDOWS_PROPERTIES)
	cp -a ./libs/* ./build/${RID}
	cp README.md ./build/${RID}

build-linux-x64 build-linux-arm build-linux-arm64 build-osx-x64 build-osx-arm64:
	$(eval RID := $(subst build-,,$(@)))
	dotnet publish ./PSXPackager/PSXPackager-linux.csproj   -c Release -r ${RID} -o ./build/${RID} $(SELF_CONTAINED_PROPERTIES)
	cp README.md ./build/${RID}

clean-gui-win-x64:
	rm -rf ./build/PsxPackagerGUI

clean-win-x64 clean-linux-x64 clean-linux-arm clean-linux-arm64 clean-osx-x64 clean-osx-arm64:
	$(eval RID := $(subst clean-,,$(@)))
	rm -rf ./build/${RID}

test:
	dotnet test PSXPackager.Tests

