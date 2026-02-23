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
            Console.WriteLine("Система управления спецификациями готова к работе.");

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
                    // ТЗ: После сообщения об ошибке выводится PS>
                    Console.WriteLine($"Ошибка: {ex.Message}");
                }
            }
        }

        static void ProcessCommand(DataManager manager, string input)
        {
            // Регулярки для парсинга команд согласно ТЗ
            if (Regex.IsMatch(input, @"^Create\s+", RegexOptions.IgnoreCase))
                HandleCreate(manager, input);
            else if (Regex.IsMatch(input, @"^Open\s+", RegexOptions.IgnoreCase))
                HandleOpen(manager, input);
            else if (Regex.IsMatch(input, @"^Input\s*", RegexOptions.IgnoreCase))
                HandleInput(manager, input);
            else if (Regex.IsMatch(input, @"^Delete\s*", RegexOptions.IgnoreCase))
                HandleDelete(manager, input);
            else if (Regex.IsMatch(input, @"^Restore\s*", RegexOptions.IgnoreCase))
                HandleRestore(manager, input);
            else if (input.Equals("Truncate", StringComparison.OrdinalIgnoreCase))
                manager.Truncate();
            else if (Regex.IsMatch(input, @"^Print\s*", RegexOptions.IgnoreCase))
                HandlePrint(manager, input);
            else if (Regex.IsMatch(input, @"^Help", RegexOptions.IgnoreCase))
                HandleHelp(manager, input);
            else
                Console.WriteLine("Неизвестная команда.");
        }

        static void HandleCreate(DataManager manager, string input)
        {
            // Формат: Create имя(размер[, спецификация])
            var match = Regex.Match(input, @"^Create\s+([^(\s]+)\s*\((\d+)(?:\s*,\s*([^)]+))?\)", RegexOptions.IgnoreCase);
            if (!match.Success) throw new Exception("Формат: Create имя_файла(макс_длина_имени[, имя_спец])");

            string prodName = match.Groups[1].Value;
            ushort dataSize = ushort.Parse(match.Groups[2].Value);
            string? specName = match.Groups[3].Success ? match.Groups[3].Value.Trim() : null;
            if (specName != null && !specName.EndsWith(".prs")) specName += ".prs";
            string fullPath = prodName.EndsWith(".prd") ? prodName : prodName + ".prd";

            if (File.Exists(fullPath))
            {
                // ТЗ: Проверка сигнатуры перед подтверждением
                if (!CheckSignature(fullPath))
                    throw new Exception("Сигнатура существующего файла не соответствует заданию.");

                Console.Write($"Файл {fullPath} существует. Перезаписать? (y/n): ");
                if (Console.ReadLine()?.Trim().ToLower() != "y") return;
            }

            manager.Create(prodName.Replace(".prd", ""), dataSize, specName);
            Console.WriteLine("Файлы созданы и открыты.");
        }

        static void HandleOpen(DataManager manager, string input)
        {
            var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2) throw new Exception("Формат: Open имя_файла");
            manager.Open(parts[1]);
        }

        static void HandleInput(DataManager manager, string input)
        {
            // Вариант 1: (имя, тип)
            var matchComp = Regex.Match(input, @"Input\s*\(\s*([^,)]+)\s*,\s*([^)]+)\s*\)", RegexOptions.IgnoreCase);
            if (matchComp.Success)
            {
                manager.AddComponent(matchComp.Groups[1].Value.Trim(), matchComp.Groups[2].Value.Trim());
                return;
            }

            // Вариант 2: (родитель/ребенок)
            var matchRel = Regex.Match(input, @"Input\s*\(\s*([^/)]+)\s*/\s*([^)]+)\s*\)", RegexOptions.IgnoreCase);
            if (matchRel.Success)
            {
                manager.AddRelation(matchRel.Groups[1].Value.Trim(), matchRel.Groups[2].Value.Trim());
                return;
            }

            throw new Exception("Неверный формат Input. Ожидается (имя, тип) или (родитель/ребенок)");
        }

        static void HandleDelete(DataManager manager, string input)
        {
            var matchRel = Regex.Match(input, @"Delete\s*\(\s*([^/)]+)\s*/\s*([^)]+)\s*\)", RegexOptions.IgnoreCase);
            if (matchRel.Success)
            {
                manager.DeleteRelation(matchRel.Groups[1].Value.Trim(), matchRel.Groups[2].Value.Trim());
                return;
            }

            var matchComp = Regex.Match(input, @"Delete\s*\(\s*([^)]+)\s*\)", RegexOptions.IgnoreCase);
            if (matchComp.Success)
            {
                manager.DeleteComponent(matchComp.Groups[1].Value.Trim());
                return;
            }
            throw new Exception("Неверный формат Delete.");
        }

        static void HandleRestore(DataManager manager, string input)
        {
            var match = Regex.Match(input, @"Restore\s*\(\s*([^)]+)\s*\)", RegexOptions.IgnoreCase);
            if (!match.Success) throw new Exception("Формат: Restore (имя) или Restore (*)");

            string param = match.Groups[1].Value.Trim();
            if (param == "*") manager.RestoreAll();
            else manager.Restore(param);
        }

        static void HandlePrint(DataManager manager, string input)
        {
            var match = Regex.Match(input, @"Print\s*\(\s*([^)]+)\s*\)", RegexOptions.IgnoreCase);
            if (!match.Success) throw new Exception("Формат: Print (имя) или Print (*)");

            string param = match.Groups[1].Value.Trim();
            if (param == "*") manager.PrintAll();
            else manager.PrintComponentTree(param);
        }

        static void HandleHelp(DataManager manager, string input)
        {
            var match = Regex.Match(input, @"Help\s*(.+)?", RegexOptions.IgnoreCase);
            string? fileName = match.Groups[1].Success ? match.Groups[1].Value.Trim() : null;

            if (!string.IsNullOrEmpty(fileName))
            {
                using var sw = new StreamWriter(fileName);
                var oldOut = Console.Out;
                Console.SetOut(sw);
                manager.Help();
                Console.SetOut(oldOut);
                Console.WriteLine($"Справка сохранена в {fileName}");
            }
            else manager.Help();
        }

        private static bool CheckSignature(string path)
        {
            try
            {
                using var fs = new FileStream(path, FileMode.Open, FileAccess.Read);
                if (fs.Length < 2) return false;
                byte[] sig = new byte[2];
                fs.Read(sig, 0, 2);
                return sig[0] == 'P' && sig[1] == 'S';
            }
            catch { return false; }
        }
    }
}