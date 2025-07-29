pushd build
tar -a -c -f PsxPackagerGUI.zip PsxPackagerGUI
tar -a -c -f psxpackager-win-x64.zip psxpackager-win-x64
tar -cvzf psxpackager-linux-x64.tar.gz psxpackager-linux-x64
tar -cvzf psxpackager-linux-arm.tar.gz  psxpackager-linux-arm
tar -cvzf psxpackager-linux-arm64.tar.gz psxpackager-linux-arm64
tar -cvzf psxpackager-osx-x64.tar.gz psxpackager-osx-x64
tar -cvzf psxpackager-osx-arm64.tar.gz psxpackager-osx-arm64
popd