using Spss.SpssMetadata;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;

namespace SQLiteToSAV
{
    internal class Program
    {
        private static int maxString = 2000;
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Неверно указаны аргументы.");
                Console.WriteLine("Нажмите на любую клавишу для продолжения...");
                Console.ReadLine();
                return;
            }

            List<string> tables = new List<string>();
            List<Variable> variables = new List<Variable>();

            using (SQLiteConnection connection = new SQLiteConnection($"Data Source={args[0]};Version=3;"))
            {
                connection.Open();
                #region Reading tables
                using (SQLiteCommand command = new SQLiteCommand("SELECT name FROM sqlite_master WHERE type = 'table';", connection))
                {
                    using (SQLiteDataReader reader = command.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                tables.Add(reader.GetValue(0).ToString());
                            }
                        }
                    }
                }
                #endregion
                #region Asking user
                Console.WriteLine("Выберите табоицу для экспортирования:");
                tables.ForEach(x => Console.WriteLine(x));
                Console.Write("Ответ: ");
                string answer = Console.ReadLine();
                #endregion

                if (tables.Contains(answer))
                {
                    #region Getting headers
                    using (SQLiteCommand command = new SQLiteCommand($"SELECT name,type FROM PRAGMA table_info({answer});", connection))
                    {
                        using (SQLiteDataReader reader = command.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    string name = reader.GetValue(0).ToString();
                                    string fullType = reader.GetValue(1).ToString();
                                    string typeName = fullType.Contains('(') ? fullType.Substring(0, fullType.IndexOf('(') - 1) : fullType;
                                    int first = fullType.Contains('(') ? int.Parse(fullType.Replace(" ", "").Substring(fullType.IndexOf('('), fullType.IndexOf(',') - fullType.IndexOf('(') - 1)) : 10;
                                    int second = fullType.Contains('(') ? int.Parse(fullType.Replace(" ", "").Substring(fullType.IndexOf(','), fullType.IndexOf(')') - fullType.IndexOf(',') - 1)) : 10;
                                    switch (typeName)
                                    {
                                        
                                        case "STRING":
                                        case "TEXT":
                                        case "BLOB":
                                            variables.Add(Variable.Create(name, name, FormatType.A, maxString, 0));
                                            break;
                                        case "DATE":
                                            variables.Add(Variable.Create(name, name, FormatType.DATE, 10, 0));
                                            break;
                                        case "DATETIME":
                                            variables.Add(Variable.Create(name, name, FormatType.DATETIME, 10, 0));
                                            break;
                                        default:
                                            variables.Add(Variable.Create(name, name, FormatType.COMMA, first, second));
                                            break;
                                    }
                                }
                            }
                        }
                    }
                    #endregion
                }
                else
                {
                    Console.WriteLine("Такой таблицы нет. Перезапустите программу и выберите другую.");
                }
            }

            Console.WriteLine("Нажмите на любую клавишу для продолжения...");
            Console.ReadKey();
        }
    }
}
