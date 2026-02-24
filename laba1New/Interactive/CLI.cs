using System;
using System.IO;
using System.Text.RegularExpressions;

namespace laba1New
{
    static class CLI
    {
        public static void Run()
        {
            using var manager = new DataManager();

            while (true)
            {
                Console.Write("PS> ");
                string? input = Console.ReadLine()?.Trim();
                if (string.IsNullOrEmpty(input)) continue;

                if (input.Equals("exit", StringComparison.OrdinalIgnoreCase)) break;

                try
                {
                    ProcessCommand(manager, input);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка: {ex.Message}");
                }
            }
        }

        private static void ProcessCommand(DataManager manager, string input)
        {
            // Команды, которые не требуют открытых файлов
            bool isCreate = Regex.IsMatch(input, @"^Create\s+", RegexOptions.IgnoreCase);
            bool isOpen = Regex.IsMatch(input, @"^Open\s+", RegexOptions.IgnoreCase);
            bool isHelp = Regex.IsMatch(input, @"^Help", RegexOptions.IgnoreCase);

            if (!isCreate && !isOpen && !isHelp)
            {
                // Для всех остальных команд файлы должны быть открыты
                if (!manager.IsOpen)
                {
                    Console.WriteLine("Ошибка: сначала откройте файлы командой Open.");
                    return;
                }
            }

            // Далее обработка команд
            if (isCreate)
                HandleCreate(manager, input);
            else if (isOpen)
                HandleOpen(manager, input);
            else if (Regex.IsMatch(input, @"^Input\s+", RegexOptions.IgnoreCase))
                HandleInput(manager, input);
            else if (Regex.IsMatch(input, @"^Delete\s+", RegexOptions.IgnoreCase))
                HandleDelete(manager, input);
            else if (Regex.IsMatch(input, @"^Restore\s+", RegexOptions.IgnoreCase))
                HandleRestore(manager, input);
            else if (input.Equals("Truncate", StringComparison.OrdinalIgnoreCase))
                manager.Truncate();
            else if (Regex.IsMatch(input, @"^Print\s+", RegexOptions.IgnoreCase))
                HandlePrint(manager, input);
            else if (isHelp)
                HandleHelp(manager, input);
            else
                Console.WriteLine("Неизвестная команда.");
        }

        private static void HandleCreate(DataManager manager, string input)
        {
            // Новый формат: Create имяфайла,максдлина [имяспец]
            var match = Regex.Match(input, @"^Create\s+(\S+)\s*,\s*(\d+)(?:\s+(\S+))?", RegexOptions.IgnoreCase);
            if (!match.Success)
                throw new Exception("Формат: Create имя_файла,макс_длина_имени [имя_файла_спецификации]");

            string prodName = match.Groups[1].Value;
            ushort dataSize = ushort.Parse(match.Groups[2].Value);
            string? specName = match.Groups[3].Success ? match.Groups[3].Value : null;

            if (specName != null && !specName.EndsWith(".prs")) specName += ".prs";
            string fullPath = prodName.EndsWith(".prd") ? prodName : prodName + ".prd";

            if (File.Exists(fullPath))
            {
                bool sigOk;
                try
                {
                    sigOk = CheckSignature(fullPath);
                }
                catch (IOException)
                {
                    throw new Exception($"Файл {fullPath} занят другим процессом. Закройте его и повторите попытку.");
                }

                if (!sigOk)
                    throw new Exception("Сигнатура существующего файла не соответствует заданию.");

                Console.Write($"Файл {fullPath} существует. Перезаписать? (y/n): ");
                if (Console.ReadLine()?.Trim().ToLower() != "y") return;
            }

            manager.Create(prodName.Replace(".prd", ""), dataSize, specName);
            Console.WriteLine("Файлы созданы.");
        }

        private static void HandleOpen(DataManager manager, string input)
        {
            var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2) throw new Exception("Формат: Open имя_файла");
            manager.Open(parts[1]);
        }

        private static void HandleInput(DataManager manager, string input)
        {
            // Удаляем "Input" из начала строки
            string args = input.Substring(5).Trim();

            // Проверяем, содержит ли аргумент "/" (это связь родитель/ребенок)
            if (args.Contains('/'))
            {
                // Формат: Input родитель/ребенок [количество]
                var parts = args.Split(new[] { '/', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 2) throw new Exception("Формат: Input родитель/ребенок [количество]");

                string parent = parts[0].Trim();
                string child = parts[1].Trim();
                ushort count = 1;

                if (parts.Length >= 3 && ushort.TryParse(parts[2], out ushort parsedCount))
                    count = parsedCount;

                manager.AddRelation(parent, child, count);
            }
            else
            {
                // Формат: Input имя тип
                var parts = args.Split(',', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 2) throw new Exception("Формат: Input имя,тип (Изделие/Узел/Деталь)");

                string name = parts[0].Trim();
                string type = string.Join(" ", parts.Skip(1)).Trim(); // на случай "тип из двух слов"

                manager.AddComponent(name, type);
            }
        }

        private static void HandleDelete(DataManager manager, string input)
        {
            // Удаляем "Delete" из начала строки
            string args = input.Substring(6).Trim();

            // Проверяем, содержит ли аргумент "/" (это связь родитель/ребенок)
            if (args.Contains('/'))
            {
                // Формат: Delete родитель/ребенок
                var parts = args.Split('/', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 2) throw new Exception("Формат: Delete родитель/ребенок");

                manager.DeleteRelation(parts[0].Trim(), parts[1].Trim());
            }
            else
            {
                // Формат: Delete имя
                if (string.IsNullOrEmpty(args)) throw new Exception("Формат: Delete имя_компонента");
                manager.DeleteComponent(args);
            }
        }

        private static void HandleRestore(DataManager manager, string input)
        {
            // Удаляем "Restore" из начала строки
            string param = input.Substring(7).Trim();

            if (string.IsNullOrEmpty(param))
                throw new Exception("Формат: Restore имя или Restore *");

            if (param == "*")
                manager.RestoreAll();
            else
                manager.Restore(param);
        }

        private static void HandlePrint(DataManager manager, string input)
        {
            // Удаляем "Print" из начала строки
            string param = input.Substring(5).Trim();

            if (string.IsNullOrEmpty(param))
                throw new Exception("Формат: Print имя или Print *");

            if (param == "*")
                manager.PrintAll();
            else
                manager.PrintComponentTree(param);
        }

        private static void HandleHelp(DataManager manager, string input)
        {
            // Удаляем "Help" из начала строки
            string fileName = input.Length > 4 ? input.Substring(4).Trim() : "";

            if (!string.IsNullOrEmpty(fileName))
            {
                using var sw = new StreamWriter(fileName);
                var oldOut = Console.Out;
                Console.SetOut(sw);
                manager.Help();
                Console.SetOut(oldOut);
                Console.WriteLine($"Справка сохранена в {fileName}");
            }
            else
                manager.Help();
        }

        private static bool CheckSignature(string path)
        {
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            if (fs.Length < 2) return false;
            byte[] sig = new byte[2];
            fs.Read(sig, 0, 2);
            return sig[0] == 'P' && sig[1] == 'S';
        }
    }
}