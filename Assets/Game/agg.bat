set OUTPUT_FILE=all_cs_code.txt
set COUNT=0

:: Удаляем старый файл, если он существует
if exist "%OUTPUT_FILE%" del "%OUTPUT_FILE%"

echo Собираем все .cs файлы из текущей папки и подпапок...
echo ==================================================== >> "%OUTPUT_FILE%"
echo Сборка C# кода от %date% %time% >> "%OUTPUT_FILE%"
echo ==================================================== >> "%OUTPUT_FILE%"
echo. >> "%OUTPUT_FILE%"

:: Рекурсивно ищем все .cs файлы
for /r %%f in (*.cs) do (
    set /a COUNT+=1
    echo [%%f] - Файл №!COUNT! >> "%OUTPUT_FILE%"
    echo ---------------------------------------- >> "%OUTPUT_FILE%"
    type "%%f" >> "%OUTPUT_FILE%"
    echo. >> "%OUTPUT_FILE%"
    echo ======================================== >> "%OUTPUT_FILE%"
    echo. >> "%OUTPUT_FILE%"
    echo. >> "%OUTPUT_FILE%"
)

echo Готово! Найдено и скопировано !COUNT! файлов.
echo Результат сохранён в %OUTPUT_FILE%
pause
