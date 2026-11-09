using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Data.Sqlite;

namespace SchoolDatabaseDemo;

public class DatabaseService
{
    private const string DbFile = "school.db";
    private const string ConnectionString = "Data Source=" + DbFile;

    public bool DatabaseExists() => File.Exists(DbFile);

    // Връща отворена връзка (урок 12, слайд 4)
    private SqliteConnection OpenConnection(bool create = false)
    {
        if (!create && !DatabaseExists())
            throw new InvalidOperationException(
                "Базата още не е създадена. Натиснете „Създай база“.");

        var connection = new SqliteConnection(ConnectionString);
        connection.Open();
        return connection;
    }

    // Урок 11: създава таблиците Students и Subjects
    public void InitializeDatabase()
    {
        using var connection = OpenConnection(create: true);

        using (var command = connection.CreateCommand())
        {
            command.CommandText =
                @"CREATE TABLE IF NOT EXISTS Students (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL,
                    ClassName TEXT NOT NULL,
                    AverageGrade REAL,
                    Email TEXT
                );";
            command.ExecuteNonQuery();
        }

        // Ако school.db е от стар вариант без Email, добавяме колоната
        if (!ColumnExists(connection, "Students", "Email"))
        {
            using var alter = connection.CreateCommand();
            alter.CommandText = "ALTER TABLE Students ADD COLUMN Email TEXT;";
            alter.ExecuteNonQuery();
        }

        using (var command = connection.CreateCommand())
        {
            command.CommandText =
                @"CREATE TABLE IF NOT EXISTS Subjects (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL UNIQUE
                );";
            command.ExecuteNonQuery();
        }
    }

    private static bool ColumnExists(SqliteConnection connection, string table, string column)
    {
        using var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA table_info({table});";
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            if (string.Equals(reader.GetString(1), column, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    // Урок 11, допълнителна задача: бутон „Провери база“
    public string CheckDatabase()
    {
        if (!DatabaseExists())
            return "Файлът school.db не е намерен. Натиснете „Създай база“.";

        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText =
            "SELECT name FROM sqlite_master " +
            "WHERE type = 'table' AND name NOT LIKE 'sqlite_%' ORDER BY name";

        var tables = new List<string>();
        using (var reader = command.ExecuteReader())
        {
            while (reader.Read())
                tables.Add(reader.GetString(0));
        }

        return tables.Count == 0
            ? "school.db съществува, но няма таблици."
            : "school.db е наличен. Таблици: " + string.Join(", ", tables);
    }

    // SELECT
    public List<Student> GetStudents()
    {
        var students = new List<Student>();

        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText =
            "SELECT Id, Name, ClassName, AverageGrade FROM Students ORDER BY Id";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            students.Add(new Student
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                ClassName = reader.GetString(2),
                AverageGrade = reader.IsDBNull(3) ? null : reader.GetDouble(3)
            });
        }
        return students;
    }

    // Търсене по име. Филтрираме в C#, защото LIKE в SQLite
    // не различава малки/главни само за латински букви, не и за кирилица.
    public List<Student> SearchStudents(string text) =>
        GetStudents()
            .Where(s => s.Name.Contains(text, StringComparison.CurrentCultureIgnoreCase))
            .ToList();

    // INSERT
    public void AddStudent(string name, string className, double grade)
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText =
            @"INSERT INTO Students (Name, ClassName, AverageGrade)
              VALUES ($name, $class, $grade)";
        command.Parameters.AddWithValue("$name", name);
        command.Parameters.AddWithValue("$class", className);
        command.Parameters.AddWithValue("$grade", grade);
        command.ExecuteNonQuery();
    }

    // UPDATE
    public void UpdateStudent(int id, string name, string className, double grade)
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText =
            @"UPDATE Students SET
                Name = $name,
                ClassName = $class,
                AverageGrade = $grade
              WHERE Id = $id";
        command.Parameters.AddWithValue("$name", name);
        command.Parameters.AddWithValue("$class", className);
        command.Parameters.AddWithValue("$grade", grade);
        command.Parameters.AddWithValue("$id", id);
        command.ExecuteNonQuery();
    }

    // DELETE
    public void DeleteStudent(int id)
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM Students WHERE Id = $id";
        command.Parameters.AddWithValue("$id", id);
        command.ExecuteNonQuery();
    }
}