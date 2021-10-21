using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace AllureReportViewer
{
    /// <summary>
    /// Класс для поднятия http-сервера
    /// </summary>
    class HttpServer
    {
        public static HttpListener listener;
        public static int requestCount = 0;

        /// <summary>
        /// Отправка страницы с ошибкой в ответ пользователю
        /// </summary>
        /// <param name="resp">Объект для о</param>
        /// <param name="Code">Http-код ошибки</param>
        private static void SendError(HttpListenerResponse resp, int Code)
        {
            // Получаем строку вида "200 OK"
            // HttpStatusCode хранит в себе все статус-коды HTTP/1.1
            string CodeStr = Code.ToString() + " " + ((HttpStatusCode)Code).ToString();
            // Код простой HTML-странички
            string Html = "<html><body><h1>" + CodeStr + "</h1></body></html>";

            // Приведем строку к виду массива байт
            byte[] Buffer = Encoding.UTF8.GetBytes(Html);

            // Посылаем заголовки
            resp.ContentType = "text/html";
            resp.ContentEncoding = Encoding.UTF8;
            resp.ContentLength64 = Html.Length;

            // Отправим его клиенту
            resp.OutputStream.Write(Buffer, 0, Buffer.Length);
            
            // Закроем соединение
            resp.Close();
        }

        /// <summary>
        /// Метод для обработки входящих соединений
        /// </summary>
        /// <returns></returns>
        public static async Task HandleIncomingConnections()
        {
            bool runServer = true;

            // Пока пользователь не посетит страницу `shutdown`, слушаем входящие запросы
            while (runServer)
            {
                // Ожидание входящих соединений
                HttpListenerContext ctx = await listener.GetContextAsync();

                // Получение объектов запроса и ответа из соединения
                HttpListenerRequest req = ctx.Request;
                HttpListenerResponse resp = ctx.Response;

                string requestUri = req.Url.AbsolutePath;

                // Вывод информации о подключении
                Console.WriteLine("Подключение #: {0}", ++requestCount);
                Console.WriteLine(req.Url.ToString());
                Console.WriteLine(req.HttpMethod);
                Console.WriteLine(req.UserHostName);
                Console.WriteLine(req.UserAgent);
                Console.WriteLine();

                // Если входящий запрос - это POST-запрос на адрес shutdown, то завершение работы сервера
                if ((req.HttpMethod == "POST") && (requestUri == "/shutdown"))
                {
                    Console.WriteLine("Выключение http-сервера");
                    runServer = false;
                }

                // Если в строке содержится двоеточие, передадим ошибку 400
                // Это нужно для защиты от URL типа http://example.com/../../file.txt
                if (requestUri.IndexOf("..") >= 0)
                {
                    SendError(resp, 400);
                    continue;
                }

                // Если строка запроса оканчивается на "/", то добавим к ней index.html
                if (requestUri.EndsWith("/"))
                {
                    requestUri += "index.html";
                }


                // Буфер для хранения принятых от клиента данных
                byte[] Buffer = new byte[1024];
                // Переменная для хранения количества байт, принятых от клиента
                int Count;

                // Путь до возвращаемого файла с операционной системы
                string FilePath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + requestUri.Replace("/", "\\");

                // Если в папке www не существует данного файла, посылаем ошибку 404
                if (!File.Exists(FilePath))
                {
                    SendError(resp, 404);
                    continue;
                }

                // Получаем расширение файла из строки запроса
                string Extension = requestUri.Substring(requestUri.LastIndexOf('.'));
                // Тип содержимого
                string ContentType = "";

                // Пытаемся определить тип содержимого по расширению файла
                switch (Extension)
                {
                    case ".htm":
                    case ".html":
                        ContentType = "text/html";
                        break;
                    case ".css":
                        ContentType = "text/css";
                        break;
                    case ".js":
                        ContentType = "application/javascript";
                        break;
                    case ".json":
                        ContentType = "application/json";
                        break;
                    case ".jpg":
                        ContentType = "image/jpeg";
                        break;
                    case ".jpeg":
                    case ".png":
                    case ".svg":
                    case ".ico":
                    case ".gif":
                        ContentType = "image/" + Extension.Substring(1);
                        break;
                    default:
                        if (Extension.Length > 1)
                        {
                            ContentType = "application/" + Extension.Substring(1);
                        }
                        else
                        {
                            ContentType = "application/unknown";
                        }
                        break;
                }

                // Открываем файл, обрабатывая все возможные ошибки
                FileStream fs;
                try
                {
                    fs = new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                }
                catch (Exception)
                {
                    // Если случилась ошибка, посылаем клиенту ошибку 500
                    SendError(resp, 500);
                    continue;
                }

                // Посылаем заголовки
                resp.ContentType = ContentType;
                resp.ContentEncoding = Encoding.UTF8;
                resp.ContentLength64 = fs.Length;

                // Пока не достигнут конец файла
                while (fs.Position < fs.Length)
                {
                    // Читаем данные из файла
                    Count = fs.Read(Buffer, 0, Buffer.Length);
                    // И передаем их клиенту
                    resp.OutputStream.Write(Buffer, 0, Count);
                }

                // Закроем файл и соединение
                fs.Close();
                resp.Close();
            }
        }


        public static void Main(string[] args)
        {
            // Это магическое число, просто так понравилось и все
            int port = 55555;

            // Код далее нужен для того, чтобы можно было определить занятость порта,
            // и при необходимости инкрементом подобрать следующий доступный.
            // Данный код также решает проблему запуска нескольких экземпляров программы (отчетов)
            // без необходимости действий со стороны пользователя.
            int? port2;

            port2 = IpUtilities.GetAvailablePort(55555, 65000);
            if (port2 == null)
            {
                Console.WriteLine("Все доступные порты заняты! Выход из программы!");
                Console.ReadLine();
            }
            else
            {
                port = (int)port2;
            }

            // ВАЖНО: тут адрес должен быть именно localhost, в противном случае
            // программа не будет запускаться без прав администратора, что
            // потенциально будет отпугивать пользователей.
            // А в корпоративной среде этим в принципе не получиться воспользоваться,
            // так как прав Администратора почти ни у кого нет.
            string url = "http://localhost:" + port + "/";

            // Создание http-сервера и начало прослушивания входящий соединений
            listener = new HttpListener();
            listener.Prefixes.Add(url);
            listener.Start();
            Console.WriteLine("Ожидание соединений по адресу {0}", url);
            System.Diagnostics.Process.Start(url);

            // Handle requests
            Task listenTask = HandleIncomingConnections();
            listenTask.GetAwaiter().GetResult();

            // Закрытие http-сервера
            listener.Close();
        }
    }

    /// <summary>
    /// Класс для работы с сетью
    /// </summary>
    public static class IpUtilities
    {
        private const ushort MIN_PORT = 1;
        private const ushort MAX_PORT = UInt16.MaxValue;

        /// <summary>
        /// Функция получения первого доступного открытого порта
        /// </summary>
        /// <param name="lowerPort">Порт, с которого начинать сканирование</param>
        /// <param name="upperPort">Порт, до которого сканировать</param>
        /// <returns></returns>
        public static int? GetAvailablePort(ushort lowerPort = MIN_PORT, ushort upperPort = MAX_PORT)
        {
            var ipProperties = IPGlobalProperties.GetIPGlobalProperties();
            var usedPorts = new HashSet<int>(Enumerable.Empty<int>()
                .Concat(ipProperties.GetActiveTcpConnections().Select(c => c.LocalEndPoint.Port))
                .Concat(ipProperties.GetActiveTcpListeners().Select(l => l.Port))
                .Concat(ipProperties.GetActiveUdpListeners().Select(l => l.Port)));
            for (int port = lowerPort; port <= upperPort; port++)
            {
                if (!usedPorts.Contains(port)) return port;
            }
            return null;
        }
    }
}
