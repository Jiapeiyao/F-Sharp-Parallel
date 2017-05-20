@echo off
set LOG=_A1-performance.log
echo. > %LOG%

echo. >> %LOG%
echo *** Medium default-partitions (0) >> %LOG%
echo *** Medium default-partitions (0) 
.\A1.exe m-1000-300000-1.txt /* 0 0 >> %LOG%

tail -60 %LOG%
timeout /t 10

echo. >> %LOG%
echo *** Medium explicit-partitions (24) >> %LOG%
echo *** Medium explicit-partitions (24)
.\A1.exe m-1000-300000-1.txt /* 24 0 >> %LOG%

tail -60 %LOG%
timeout /t 10

echo. >> %LOG%
echo *** Large default-partitions (0) >> %LOG%
echo *** Large default-partitions (0) 
.\A1.exe m-1000-1000000-1.txt /* 0 0 >> %LOG%

tail -60 %LOG%
timeout /t 10

echo. >> %LOG%
echo *** Large explicit-partitions (24) >> %LOG%
echo *** Large explicit-partitions (24) 
.\A1.exe m-1000-1000000-1.txt /* 24 0 >> %LOG%

tail -60 %LOG%
timeout /t 10

echo. >> %LOG%
echo *** Very Large default-partitions (0) >> %LOG%
echo *** Very Large default-partitions (0) 
.\A1.exe m-1000-10000000-1.txt /* 0 0 >> %LOG%

tail -60 %LOG%
timeout /t 10

echo. >> %LOG%
echo *** Very Large explicit-partitions (24) >> %LOG%
echo *** Very Large explicit-partitions (24) 
.\A1.exe m-1000-10000000-1.txt /* 24 0 >> %LOG%

tail -60 %LOG%
timeout /t 10

echo. >> %LOG%
echo *** Very Large (alt) default-partitions (0) >> %LOG%
echo *** Very Large (alt) default-partitions (0)
.\A1.exe m-10000-1000000-1.txt /* 0 0 >> %LOG%

tail -60 %LOG%
timeout /t 10

echo. >> %LOG%
echo *** Very Large (alt) explicit-partitions (24) >> %LOG%
echo *** Very Large (alt) explicit-partitions (24) 
.\A1.exe m-10000-1000000-1.txt /* 24 0 >> %LOG%

tail -60 %LOG%
pause

