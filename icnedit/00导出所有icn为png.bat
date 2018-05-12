@echo off & setlocal EnableDelayedExpansion

md .\export
for /f "delims=" %%i in ('dir /s/b ".\*.icn"') do (
	icnedit export %%~ni.icn .\export\export_%%~ni.png
)
