.PHONY:	all build clean

.PHONY:	build-linux-x64 build-osx-x64
.PHONY:	clean-linux-x64 clean-osx-x64

all:	build

build:
	$(MAKE) build-linux-x64
	$(MAKE) build-osx-x64

build-linux-x64:
	dotnet publish ./PSXPackager/PSXPackager-linux.csproj -c Release -r linux-x64 -o ./build/linux-x64 /p:DebugType=None /p:DebugSymbols=false
	cp README.MD ./build/linux-x64

build-osx-x64:
	dotnet publish ./PSXPackager/PSXPackager-linux.csproj -c Release -r osx-x64 -o ./build/osx-x64 /p:DebugType=None /p:DebugSymbols=false
	cp README.MD ./build/osx-x64

clean:	clean-linux-x64 clean-osx-x64

clean-linux-x64:
	rm -rf ./build/linux-x64

clean-osx-x64:
	rm -rf ./build/osx-x64

