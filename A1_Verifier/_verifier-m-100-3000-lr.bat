echo off
setlocal enabledelayedexpansion 
set PROGRAM=.\a1.exe
set RESHAPER=.\reshaper.exe

set FNAME=m-100-3000-lr
set N1=100
set N2=3000
set K=0
set V=1
call :VERIFY

set FNAME=m-100-3000-lr
set N1=100
set N2=3000
set K=24
set V=1
call :VERIFY

exit /B %ERRORLEVEL%

:VERIFY
set LOGNAME-SEQ=log-!FNAME!-SEQ-!K!-!V!
set OUTNAME-SEQ=out-!LOGNAME-SEQ!

echo.
echo ... !LOGNAME-SEQ!
if exist %PROGRAM% %PROGRAM% !FNAME!.txt /SEQ !K! !V! > !LOGNAME-SEQ!.txt
%RESHAPER% !LOGNAME-SEQ!.txt !N1! !N2! -1 > !OUTNAME-SEQ!.txt
pause

set LOGNAME-PAR-NAIVE=log-!FNAME!-PAR-NAIVE-!K!-!V!
set OUTNAME-PAR-NAIVE=out-!LOGNAME-PAR-NAIVE!

echo.
echo ... !LOGNAME-PAR-NAIVE!
if exist %PROGRAM% %PROGRAM% !FNAME!.txt /PAR-NAIVE !K! !V! > !LOGNAME-PAR-NAIVE!.txt
%RESHAPER% !LOGNAME-PAR-NAIVE!.txt !N1! !N2! -1 > !OUTNAME-PAR-NAIVE!.txt

echo.
FC !OUTNAME-SEQ!.txt !OUTNAME-PAR-NAIVE!.txt
pause

for %%A in (PAR-RANGE ASYNC-RANGE MAILBOX-RANGE AKKA-RANGE) DO (
set LOGNAME=log-!FNAME!-%%A-!K!-!V!
set OUTNAME=out-!LOGNAME!

echo.
echo ... !LOGNAME!
if exist %PROGRAM% %PROGRAM% !FNAME!.txt /%%A !K! !V! > !LOGNAME!.txt
%RESHAPER% !LOGNAME!.txt !N1! !N2! !K! > !OUTNAME!.txt

echo.
FC %OUTNAME-SEQ%.txt !OUTNAME!.txt
pause
)

exit /B 0
