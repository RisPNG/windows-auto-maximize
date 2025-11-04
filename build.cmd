@echo off

REM Define the base paths
set CSC_PATH="C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
set LIB_PATH="C:\Windows\Microsoft.NET\Framework64\v4.0.30319"
set FORMS_REFERENCE="C:\Windows\Microsoft.NET\Framework64\v4.0.30319\System.Windows.Forms.dll"
set SOURCE_FILE="auto-maximize.cs"

REM Define the output executable name
set EXE_NAME=auto-maximize

REM Specify the icon file (optional)
set ICON_FILE="1.ico"

REM Build the executable
echo Building %EXE_NAME%.exe...

REM Check if icon file exists and build accordingly
if exist %ICON_FILE% (
    echo Using icon: %ICON_FILE%
    %CSC_PATH% -lib:%LIB_PATH% -r:%FORMS_REFERENCE% -target:winexe -win32icon:%ICON_FILE% -out:"%EXE_NAME%.exe" %SOURCE_FILE%
) else (
    echo No icon file found, building without icon...
    %CSC_PATH% -lib:%LIB_PATH% -r:%FORMS_REFERENCE% -target:winexe -out:"%EXE_NAME%.exe" %SOURCE_FILE%
)

if %ERRORLEVEL% EQU 0 (
    echo.
    echo Build successful! Created %EXE_NAME%.exe
) else (
    echo.
    echo Build failed! Check error messages above.
)

PAUSE