@echo off
chcp 65001 >nul
title Food Street Guide MAUI — Build Tool
color 0A

echo.
echo  ╔══════════════════════════════════════════════════════════╗
echo  ║   🍜 FOOD STREET GUIDE  —  .NET MAUI BUILD TOOL        ║
echo  ║   Android APK  +  Windows EXE                          ║
echo  ╚══════════════════════════════════════════════════════════╝
echo.

dotnet --version >nul 2>&1
IF %ERRORLEVEL% NEQ 0 ( echo Khong co dotnet! & pause & exit /b 1 )
FOR /F "tokens=*" %%v IN ('dotnet --version') DO SET VER=%%v
echo  dotnet %VER%
echo.

echo Chon nen tang:
echo  [1] Android APK
echo  [2] Windows EXE
echo  [3] Ca hai
echo.
set /p CHOICE=Nhap so (1/2/3): 

echo.
echo  Restore packages...
dotnet restore FoodStreetMAUI.csproj --nologo -v q
IF %ERRORLEVEL% NEQ 0 ( echo Restore that bai! & pause & exit /b 1 )

IF "%CHOICE%"=="1" GOTO BUILD_ANDROID
IF "%CHOICE%"=="3" GOTO BUILD_ANDROID
GOTO BUILD_WINDOWS

:BUILD_ANDROID
echo.
echo  Build Android APK...
dotnet publish FoodStreetMAUI.csproj ^
    -f net10.0-android ^
    -c Release ^
    -o dist\android ^
    --nologo -v q
IF %ERRORLEVEL% NEQ 0 (
    echo.
    echo  Android build that bai!
    echo  Kiem tra: Android SDK da cai chua?
    echo  Tai tai: https://developer.android.com/studio
    echo.
)  ELSE (
    echo  APK: dist\android\
)
IF "%CHOICE%"=="1" GOTO DONE

:BUILD_WINDOWS
echo.
echo  Build Windows EXE...
dotnet publish FoodStreetMAUI.csproj ^
    -f net10.0-windows10.0.19041.0 ^
    -c Release ^
    -o dist\windows ^
    --nologo -v q
IF %ERRORLEVEL% NEQ 0 (
    echo  Windows build that bai!
) ELSE (
    echo  EXE: dist\windows\
)

:DONE
echo.
echo  Hoan tat!
IF EXIST dist explorer dist
pause
