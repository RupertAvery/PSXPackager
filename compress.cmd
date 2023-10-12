pushd build
pushd PsxPackagerGUI
zip -r ..\PsxPackagerGUI.zip .
popd
pushd psxpackager-win-x64 
zip -r ..\psxpackager-win-x64.zip .
popd
REM tar -cvf psxpackager-linux-x64.tar psxpackager-linux-x64
REM tar -cvf psxpackager-linux-arm.tar  psxpackager-linux-arm
REM tar -cvf psxpackager-linux-arm64.tar psxpackager-linux-arm64
REM tar -cvf psxpackager-osx-x64.tar psxpackager-osx-x64
REM tar -cvf psxpackager-osx-arm64.tar psxpackager-osx-arm64
popd