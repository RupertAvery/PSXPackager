pushd build
tar cvf psxpackager-linux-x64.tar psxpackager-linux-x64
tar cvf psxpackager-linux-arm.tar psxpackager-linux-arm
tar cvf psxpackager-linux-arm64.tar psxpackager-linux-arm64
tar cvf psxpackager-osx-x64.tar psxpackager-osx-x64
tar cvf psxpackager-osx-arm64.tar psxpackager-osx-arm64
gzip -9 psxpackager-linux-x64.tar
gzip -9 psxpackager-linux-arm.tar
gzip -9 psxpackager-linux-arm64.tar
gzip -9 psxpackager-osx-x64.tar
gzip -9 psxpackager-osx-arm64.tar
popd
