@ECHO OFF
PUSHD %~dp0

PowerShell -NoProfile -ExecutionPolicy Bypass -Command "& '.\build.ps1' %*";

REM IF %errorlevel% neq 0 PAUSE
PAUSE
