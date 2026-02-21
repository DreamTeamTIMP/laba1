using System;
using laba1New;

class Program
{
    static void Main()
    {
        DataManager manager = new DataManager();
        Console.WriteLine("'Спецификации' запущена. Введите Help для справки.");

        while (true)
        {
            Console.Write("PS> ");
            string input = Console.ReadLine()?.Trim();

            if (string.IsNullOrEmpty(input)) continue;

            string[] args = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            string command = args[0].ToLower();

            try
            {
                switch (command)
                {
                    case "create":
                        if (args.Length < 2) throw new ArgumentException("Использование: Create <имя_базы>");
                        manager.Create(args[1], 20); // 20 - размер строки имени
                        break;

                    case "open":
                        if (args.Length < 2) throw new ArgumentException("Использование: Open <имя_базы>");
                        manager.Open(args[1]);
                        break;

                    case "input":
                        if (args.Length < 2) throw new ArgumentException("Использование: Input <имя> ИЛИ Input <узел>/<деталь>");

                        if (args[1].Contains("/"))
                        {
                            // Разбор команды Input Узел/Деталь
                            var parts = args[1].Split('/');
                            manager.AddRelation(parts[0], parts[1], 1); // 1 - это кратность по умолчанию
                        }
                        else
                        {
                            manager.AddProduct(args[1]);
                        }
                        break;

                    case "print":
                        if (args.Length < 2) throw new ArgumentException("Использование: Print * ИЛИ Print <имя>");

                        if (args[1] == "*")
                            manager.PrintAll();
                        else
                            manager.PrintTree(args[1]);
                        break;

                    case "delete":
                        if (args.Length < 2) throw new ArgumentException("Использование: Delete <имя>");
                        manager.DeleteProduct(args[1]);
                        break;

                    case "truncate":
                        manager.Truncate();
                        break;

                    case "help":
                        Console.WriteLine("Доступные команды:");
                        Console.WriteLine("Create <имя> - создать новую БД");
                        Console.WriteLine("Open <имя>   - открыть БД");
                        Console.WriteLine("Input <имя>  - добавить компонент");
                        Console.WriteLine("Input <уз>/<дет> - добавить деталь в узел");
                        Console.WriteLine("Print * - вывести все компоненты");
                        Console.WriteLine("Print <имя>  - вывести состав изделия");
                        Console.WriteLine("Delete <имя> - удалить компонент");
                        Console.WriteLine("Truncate     - сборка мусора");
                        Console.WriteLine("Exit         - выход");
                        break;

                    case "exit":
                        return;

                    default:
                        Console.WriteLine("Неизвестная команда. Введите Help.");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка выполнения команды: {ex.Message}");
            }
        }
    }
}