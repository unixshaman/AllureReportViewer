# AllureReportViewer
Локальный веб-сервер для просмотра Allure отчетов

1. Только для ОС Windows!
2. Решение сделано, так как большинство браузеров уже не могут обозревать файлы из локального каталога, как это было раньше.
3. Исполняемый файл AllureReportViewer.exe необходимо положить в каталог Allure-отчета, где лежит файл index.html.
4. Запустить программу:
   1. Откроется консольное приложение, с указанием URL, по которому можно обращаться к серверу.
      1. Порт подбирается автоматически, от 55555, и первый доступный открытый порт берется для URL сервера.
      2. Адрес всегда будет localhost, так как при указании любого другого ip-адреса доступного сетевого интерфейса, программе будет необходимы для запуска права администратора.
   3. Автоматически откроется браузер по умолчанию с URL, указанном в консольном окне в фоне.
   4. Можно просматривать отчет.
   5. Для закрытия, необходимо закрыть консольное окно через кнопку Закрыть.

![Пример работы](https://github.com/unixshaman/AllureReportViewer/raw/main/Example_1.png)