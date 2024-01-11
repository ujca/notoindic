cd fonts\ttf

dir NotoSans*-Regular.ttf /B >sans.txt
dir NotoSerif*-Regular.ttf /B >serif.txt

pyftmerge --input-file=sans.txt --output-file=..\..\NotoSansIndic.ttf --import-file=..\..\sans.ttx --drop-tables=vhea,vmtx --verbose
pyftmerge --input-file=serif.txt --output-file=..\..\NotoSerifIndic.ttf --import-file=..\..\serif.ttx --drop-tables=vhea,vmtx --verbose

cd ..\..