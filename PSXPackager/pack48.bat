@echo off

:: this script needs https://www.nuget.org/packages/ilmerge

:: set your target executable name (typically [projectname].exe)
SET APP_NAME=PSXPackager.exe

:: Set build, used for directory. Typically Release or Debug
SET ILMERGE_BUILD=Release

:: Set platform, typically x64
SET ILMERGE_PLATFORM=net48
:: set your NuGet ILMerge Version, this is the number from the package manager install, for example:
:: PM> Install-Package ilmerge -Version 3.0.29
:: to confirm it is installed for a given project, see the packages.config file
SET ILMERGE_VERSION=3.0.29

:: the full ILMerge should be found here:
SET ILMERGE_PATH=%USERPROFILE%\.nuget\packages\ilmerge\%ILMERGE_VERSION%\tools\net452
:: dir "%ILMERGE_PATH%"\ILMerge.exe


XCOPY /Y /S /E Bin\%ILMERGE_BUILD%\%ILMERGE_PLATFORM%\ Bin\ILMerge\
XCOPY /Y ..\README.md Bin\ILMerge\
DEL Bin\ILMerge\*.pdb

echo Merging %APP_NAME% ...

:: add project DLL's starting with replacing the FirstLib with this project's DLL
"%ILMERGE_PATH%"\ILMerge.exe Bin\ILMerge\%APP_NAME%  ^
  /lib:Bin\ILMerge ^
  /out:Bin\ILMerge\PSXPackagerPacked.exe ^
  CommandLine.dll ^
  DiscUtils.Core.dll ^
  DiscUtils.Iso9660.dll ^
  DiscUtils.Streams.dll ^
  ICSharpCode.SharpZipLib.dll ^
  Popstation.dll ^
  SevenZipSharp.dll ^
  System.ValueTuple.dll

echo Cleaning up...

DEL Bin\ILMerge\*.dll
DEL Bin\ILMerge\PSXPackager.exe
REN Bin\ILMerge\PSXPackagerPacked.exe PSXPackager.exe

 
ECHO Done
PAUSE

:Done  
  
