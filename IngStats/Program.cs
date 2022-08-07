using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace IngStats
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Get statistics for a ING bankaccount export(csv)");

            if (args.Length == 0)
            {
                Console.WriteLine("No file provided, example ingstats.exe \"d:\\ingexport.csv\"");
                return;
            }

            var filePath = args[0];
            if (!File.Exists(filePath))
            {
                Console.WriteLine("File not found");
                return;
            }

            var list = ReadExcel(filePath).ToArray();

            var expensesByMonth = list.Where(t => t.Amount < 0).GroupBy(t => new { t.Date.Year, t.Date.Month })
                .Select(t => new Result { Name = $"{t.Key.Year}/{t.Key.Month:D2}", Metric = t.Sum(v => Math.Abs(v.Amount)) }).OrderBy(t => t.Name);

            PrintResults("Expenses by month", expensesByMonth);

            var expensesByYear = list.Where(t => t.Amount < 0).GroupBy(t => new { t.Date.Year })
                .Select(t => new Result { Name = $"{t.Key.Year}", Metric = t.Sum(v => Math.Abs(v.Amount)) }).OrderBy(t => t.Name);

            PrintResults("Expenses by year", expensesByYear);

            var incomeByYear = list.Where(t => t.Amount > 0).GroupBy(t => new { t.Date.Year })
                .Select(t => new Result { Name = $"{t.Key.Year}", Metric = t.Sum(v => v.Amount) });

            PrintResults("Income by year", incomeByYear);

            var biggestCost = list.Where(t => t.Amount < 0).Select(t => new Result { Name = $"{t.Date} {t.Recipient} {t.Description}", Metric = t.Amount }).OrderByDescending(t => Math.Abs(t.Metric))
                .Take(10);

            var biggestRevenue = list.Where(t => t.Amount > 0).Select(t => new Result { Name = $"{t.Date} {t.Recipient} {t.Description}", Metric = t.Amount }).OrderByDescending(t => Math.Abs(t.Metric))
                .Take(10);

            PrintResults("Biggest costs", biggestCost);
            PrintResults("Biggest revenue", biggestRevenue);
            Console.ReadLine();
        }

        public static IEnumerable<Row> ReadExcel(string location)
        {
            using var reader = new StreamReader(location);
            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                if (line == null) continue;

                var values = line.Split(';');

                if (decimal.TryParse(values[6], NumberStyles.Any, new CultureInfo("nl-BE"), out var amount))
                {
                    if (DateTime.TryParseExact(values[4], "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateCreated))
                    {
                        var recipient = values[2];
                        var description = values[8];
                        yield return new Row { Amount = amount, Date = dateCreated, Recipient = recipient, Description = description };
                    }
                }
            }
        }

        public static void PrintResults(string name, IEnumerable<Result> results)
        {
            Console.WriteLine();
            Console.WriteLine($"=={name}==");
            foreach (var result in results)
            {
                Console.WriteLine("{0,-20}\t{1,5:N1}", result.Name, result.Metric);
            }
        }

        public class Result
        {
            public string Name { get; set; }
            public decimal Metric { get; set; }
        }

        public class Row
        {
            public DateTime Date { get; set; }
            public string Recipient { get; set; }
            public string Description { get; set; }
            public decimal Amount { get; set; }
        }
    }
}
