using System;
using System.IO;
using System.Text.RegularExpressions;

namespace laba1New
{
    class CLI
    {
        public static void Run()
        {
            using var manager = new DataManager();
            bool running = true;

            while (running)
            {
                Console.Write("PS> ");
                string? input = Console.ReadLine()?.Trim();
                if (string.IsNullOrEmpty(input)) continue;

                try
                {
                    running = ProcessCommand(manager, input);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка: {ex.Message}");
                }
            }
        }

        static bool ProcessCommand(DataManager manager, string input)
        {
            // Разбиваем на части по пробелам, первый элемент — команда
            string[] parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return true;

            string command = parts[0].ToLower();

            switch (command)
            {
                case "create":
                    HandleCreate(manager, input);
                    break;
                case "open":
                    HandleOpen(manager, parts);
                    break;
                case "input":
                    HandleInput(manager, input);
                    break;
                case "delete":
                    HandleDelete(manager, input);
                    break;
                case "restore":
                    HandleRestore(manager, input);
                    break;
                case "truncate":
                    manager.Truncate();
                    break;
                case "print":
                    HandlePrint(manager, input);
                    break;
                case "help":
                    HandleHelp(manager, parts);
                    break;
                case "exit":
                    return false;
                default:
                    Console.WriteLine("Неизвестная команда. Введите Help для справки.");
                    break;
            }
            return true;
        }
        static void HandleCreate(DataManager manager, string input)
        {
            var match = Regex.Match(input, @"Create\s+(\w+)\((\d+)(?:\s*,\s*([^)]+))?\)", RegexOptions.IgnoreCase);
            if (!match.Success)
            {
                Console.WriteLine("Неверный формат команды Create...");
                return;
            }

            string prodName = match.Groups[1].Value;
            if (!ushort.TryParse(match.Groups[2].Value, out ushort dataSize)) return;
            string? specName = match.Groups[3].Success ? match.Groups[3].Value.Trim() : null;

            string prodPath = prodName + ".prd";
            if (File.Exists(prodPath))
            {
                using (var fs = new FileStream(prodPath, FileMode.Open, FileAccess.Read))
                {
                    if (fs.Length >= 2)
                    {
                        byte[] sig = new byte[2];
                        fs.Read(sig, 0, 2);
                        if (System.Text.Encoding.ASCII.GetString(sig) == "PS")
                        {
                            Console.Write($"Файл {prodPath} уже существует. Перезаписать? (y/n): ");
                            if (Console.ReadLine()?.Trim().ToLower() != "y")
                            {
                                Console.WriteLine("Операция отменена.");
                                return;
                            }
                        }
                        else
                        {
                            Console.WriteLine("Ошибка: Файл существует, но имеет неверную сигнатуру.");
                            return;
                        }
                    }
                }
            }

            manager.Create(prodName, dataSize, specName);
            Console.WriteLine("Файлы созданы.");
        }
        static void HandleOpen(DataManager manager, string[] parts)
        {
            if (parts.Length < 2)
            {
                Console.WriteLine("Использование: Open имя_файла");
                return;
            }
            manager.Open(parts[1]);
        }

        static void HandleInput(DataManager manager, string input)
        {
            // Два варианта: Input (имя, тип) или Input (родитель/ребенок)
            var matchComponent = Regex.Match(input, @"Input\s*\(\s*([^,]+)\s*,\s*([^)]+)\s*\)", RegexOptions.IgnoreCase);
            if (matchComponent.Success)
            {
                string name = matchComponent.Groups[1].Value.Trim();
                string type = matchComponent.Groups[2].Value.Trim();
                manager.AddComponent(name, type);
                Console.WriteLine($"Компонент {name} добавлен.");
                return;
            }

            var matchRelation = Regex.Match(input, @"Input\s*\(\s*([^/]+)/([^)]+)\s*\)", RegexOptions.IgnoreCase);
            if (matchRelation.Success)
            {
                string parent = matchRelation.Groups[1].Value.Trim();
                string child = matchRelation.Groups[2].Value.Trim();
                // Можно указать количество через пробел после команды, но в задании нет — по умолчанию 1
                manager.AddRelation(parent, child, 1);
                Console.WriteLine($"Связь {parent} -> {child} добавлена.");
                return;
            }

            Console.WriteLine("Неверный формат команды Input.");
        }

        static void HandleDelete(DataManager manager, string input)
        {
            // Два варианта: Delete (имя) или Delete (родитель/ребенок)
            var matchComponent = Regex.Match(input, @"Delete\s*\(\s*([^/]+)\s*\)", RegexOptions.IgnoreCase);
            if (matchComponent.Success)
            {
                string name = matchComponent.Groups[1].Value.Trim();
                manager.DeleteComponent(name);
                Console.WriteLine($"Компонент {name} помечен на удаление.");
                return;
            }

            var matchRelation = Regex.Match(input, @"Delete\s*\(\s*([^/]+)/([^)]+)\s*\)", RegexOptions.IgnoreCase);
            if (matchRelation.Success)
            {
                string parent = matchRelation.Groups[1].Value.Trim();
                string child = matchRelation.Groups[2].Value.Trim();
                manager.DeleteRelation(parent, child);
                Console.WriteLine($"Связь {parent} -> {child} удалена.");
                return;
            }

            Console.WriteLine("Неверный формат команды Delete.");
        }

        static void HandleRestore(DataManager manager, string input)
        {
            var match = Regex.Match(input, @"Restore\s*\(\s*([^)]+)\s*\)", RegexOptions.IgnoreCase);
            if (!match.Success)
            {
                Console.WriteLine("Использование: Restore (имя_компонента) или Restore (*)");
                return;
            }

            string param = match.Groups[1].Value.Trim();
            if (param == "*")
            {
                manager.RestoreAll();
            }
            else
            {
                manager.Restore(param);
            }
        }

        static void HandlePrint(DataManager manager, string input)
        {
            var match = Regex.Match(input, @"Print\s*\(\s*([^)]+)\s*\)", RegexOptions.IgnoreCase);
            if (!match.Success)
            {
                Console.WriteLine("Использование: Print (*) или Print (имя_компонента)");
                return;
            }

            string param = match.Groups[1].Value.Trim();
            if (param == "*")
            {
                manager.PrintAll();
            }
            else
            {
                manager.PrintComponentTree(param);
            }
        }

        static void HandleHelp(DataManager manager, string[] parts)
        {
            if (parts.Length > 1)
            {
                // Запись справки в файл
                string fileName = parts[1];
                try
                {
                    using var writer = new StreamWriter(fileName);
                    CaptureConsoleOutput(() => manager.Help(), writer);
                    Console.WriteLine($"Справка записана в файл {fileName}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при записи в файл: {ex.Message}");
                }
            }
            else
            {
                manager.Help();
            }
        }

        // Вспомогательный метод для перенаправления вывода консоли в файл
        static void CaptureConsoleOutput(Action action, TextWriter writer)
        {
            var originalOut = Console.Out;
            Console.SetOut(writer);
            try
            {
                action();
            }
            finally
            {
                Console.SetOut(originalOut);
            }
        }
    }
}