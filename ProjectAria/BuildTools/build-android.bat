@echo off
REM ============================================================
REM build-android.bat
REM Windows build script. Requires Unity + Android module.
REM Usage: build-android.bat
REM ============================================================

setlocal enabledelayedexpansion

if "%UNITY_PATH%"=="" set UNITY_PATH="C:\Program Files\Unity\Hub\Editor\2022.3.20f1\Editor\Unity.exe"
if "%OUTPUT_PATH%"=="" set OUTPUT_PATH=Builds\Android\ProjectAria.apk
if "%KEYSTORE_NAME%"=="" set KEYSTORE_NAME=user.keystore

set PROJECT_PATH=%~dp0..
for %%I in ("%PROJECT_PATH%") do set PROJECT_PATH=%%~fI

echo.
echo === Project Aria Android Build ===
echo Unity:    %UNITY_PATH%
echo Project:  %PROJECT_PATH%
echo Output:   %OUTPUT_PATH%
echo.

if not exist "%UNITY_PATH%" (
    echo ERROR: Unity not found at: %UNITY_PATH%
    echo Set UNITY_PATH env var.
    exit /b 1
)

if not exist "%PROJECT_PATH%\Assets" (
    echo ERROR: Project not found at: %PROJECT_PATH%
    exit /b 1
)

REM Generate keystore if missing
if not exist "%PROJECT_PATH%\%KEYSTORE_NAME%" (
    echo Generating dev keystore...
    "%UNITY_PATH%" -batchmode -nographics -quit -projectPath "%PROJECT_PATH%" -executeMethod ProjectAria.Editor.BuildScript.GenerateKeystore
    if errorlevel 1 (
        echo ERROR: Failed to generate keystore. Make sure JDK is on PATH.
        exit /b 1
    )
)

REM Build
for %%I in ("%OUTPUT_PATH%") do set OUTPUT_DIR=%%~dpI
if not exist "%OUTPUT_DIR%" mkdir "%OUTPUT_DIR%"

echo Building APK...
set ARIA_OUTPUT_PATH=%OUTPUT_PATH%
"%UNITY_PATH%" -batchmode -nographics -quit -projectPath "%PROJECT_PATH%" -buildTarget Android -executeMethod ProjectAria.Editor.BuildScript.BuildAndroid -logFile -
if errorlevel 1 (
    echo.
    echo BUILD FAILED. Check logs.
    exit /b 1
)

if exist "%OUTPUT_PATH%" (
    echo.
    echo BUILD SUCCEEDED: %OUTPUT_PATH%
) else (
    echo BUILD FAILED: Output not found
    exit /b 1
)
