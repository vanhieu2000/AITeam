@echo off
rem Bam dup: lay ban moi nhat, build ra ban-chay-thu\ roi mo app de test.
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0tools\ban-chay-thu.ps1"
if errorlevel 1 pause
