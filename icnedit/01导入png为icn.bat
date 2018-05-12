@echo off & setlocal EnableDelayedExpansion
for /f "delims=" %%i in ('dir /s/b ".\*.icn"') do (
	icnedit import %%~ni.icn .\export\export_%%~ni.png
)
