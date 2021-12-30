using Spss.SpssMetadata;
using System;
using System.IO;
using System.Collections.Generic;
using System.Data.SQLite;
using SAVlizard;

namespace SQLiteToSAV
{
    internal class Program
    {
        private static int maxString = 2000;

        [STAThread]
        static void Main(string[] args)
        {
            args = new string[] { "Base.db" };
            bool flag = args.Length != 1 || !File.Exists(args[0]);

            if (flag)
            {
                args = new string[1];
            }

            while (flag)
            {
                Console.Write("Введённый путь не существует. Впишите существующий путь.\nПуть: ");
                args[0] = Console.ReadLine();
                flag = !File.Exists(args[0]);
            }

            List<string> tables = new List<string>();

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
                if (tables.Count>0)
                {
                    Console.WriteLine("Выберите таблицу для экспортирования:");
                    tables.ForEach(x => Console.WriteLine(x));
                    Console.Write("Ответ: ");
                    string answer = Console.ReadLine();
                    if (tables.Contains(answer))
                    {
                        Transform(connection, new List<string>() { answer });
                    }
                    else
                    {
                        Console.Write("Таблицы с таким названием не найдено. Вы хотите экспортитровать все таблицы сразу? (Y/N)\nОтвет:");
                        if (Console.ReadLine().ToUpper().Equals("Y"))
                        {
                            string newPath = "transrormed sav tables";
                            Directory.CreateDirectory(newPath);
                            Transform(connection, tables, newPath);
                        }
                    }
                }
                else
                {
                    Console.WriteLine("Не обнаружено ни одной таблицы. Завершаю работу");
                }
            }

            Console.WriteLine("Нажмите на любую клавишу для продолжения...");
            Console.ReadKey();
        }

        private static void Transform(SQLiteConnection connection, List<string> tablesToTransform, string savingPath = "")
        {
            if (!Directory.Exists(savingPath))
            {
                savingPath = Environment.CurrentDirectory;
            }
            foreach(string tableName in tablesToTransform)
            {
                SavTable savTable = new SavTable();
                #region Setting Variables
                using (SQLiteCommand command = new SQLiteCommand($"PRAGMA table_info([{tableName}]);", connection))
                {
                    using (SQLiteDataReader reader = command.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                string name = reader.GetValue(1).ToString();
                                string fullType = reader.GetValue(2).ToString();
                                string typeName = fullType.Contains('(') ? fullType.Substring(0, fullType.IndexOf('(') - 1) : fullType;
                                int first = fullType.Contains('(') ? int.Parse(fullType.Replace(" ", "").Substring(fullType.IndexOf('('), fullType.IndexOf(',') - fullType.IndexOf('(') - 1)) : 10;
                                int second = fullType.Contains('(') ? int.Parse(fullType.Replace(" ", "").Substring(fullType.IndexOf(','), fullType.IndexOf(')') - fullType.IndexOf(',') - 1)) : 10;
                                switch (typeName)
                                {
                                    case "STRING":
                                    case "TEXT":
                                    case "BLOB":
                                        savTable.AddColumn(Variable.Create(name, name, FormatType.A, maxString, 0));
                                        break;
                                    case "DATE":
                                        savTable.AddColumn(Variable.Create(name, name, FormatType.DATE, 10, 0));
                                        break;
                                    case "DATETIME":
                                        savTable.AddColumn(Variable.Create(name, name, FormatType.DATETIME, 10, 0));
                                        break;
                                    default:
                                        savTable.AddColumn(Variable.Create(name, name, FormatType.COMMA, first, second));
                                        break;
                                }
                            }
                        }
                    }
                }
                #endregion
                #region Reading-Writing records
                using (SQLiteCommand command = new SQLiteCommand($"SELECT * FROM [{tableName}]", connection))
                {
                    using (SQLiteDataReader reader = command.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                List<object?> readArray = new List<object?>();
                                for (int i = 0; i < savTable.Columns; i++)
                                {
                                    object? value = reader.GetValue(i);
                                    if (value is not null)
                                    {
                                        if (value is string || value is DateTime)
                                        {
                                            readArray.Add(value);
                                        }
                                        else
                                        {
                                            readArray.Add(Convert.ToDouble(value));
                                        }
                                    }
                                    else
                                    {
                                        readArray.Add(null);
                                    }
                                }
                                savTable.AddRow(readArray);
                            }
                        }
                    }
                }
                #endregion
                savTable.SaveAs($"{savingPath}\\{tableName}.sav");
            }
        }
    }
}
