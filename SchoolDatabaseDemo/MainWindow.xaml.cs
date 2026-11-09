using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace SchoolDatabaseDemo;

public partial class MainWindow : Window
{
    private readonly DatabaseService _db = new();
    private int? _selectedId;   // Id на избрания ред

    public MainWindow()
    {
        InitializeComponent();

        if (_db.DatabaseExists())
        {
            try
            {
                LoadStudents();
                StatusTextBlock.Text = "Базата е заредена.";
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = "Грешка: " + ex.Message;
            }
        }
        else
        {
            StatusTextBlock.Text = "Натиснете „Създай база“.";
        }
    }

    // ---------- Урок 11 ----------

    private void CreateDatabaseButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _db.InitializeDatabase();
            StatusTextBlock.Text = "Базата е готова.";
            LoadStudents();
        }
        catch (Exception ex)
        {
            StatusTextBlock.Text = "Грешка: " + ex.Message;
        }
    }

    private void CheckDatabaseButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            StatusTextBlock.Text = _db.CheckDatabase();
        }
        catch (Exception ex)
        {
            StatusTextBlock.Text = "Грешка: " + ex.Message;
        }
    }

    // ---------- Урок 12 ----------

    // SELECT + задаване на ItemsSource + брой записи
    private void LoadStudents(int? selectId = null)
    {
        var students = string.IsNullOrWhiteSpace(SearchBox.Text)
            ? _db.GetStudents()
            : _db.SearchStudents(SearchBox.Text.Trim());

        StudentsGrid.ItemsSource = students;
        CountTextBlock.Text = $"Брой записи: {students.Count}";

        if (selectId is int id)
            StudentsGrid.SelectedItem = students.FirstOrDefault(s => s.Id == id);
    }

    // Избор на ред: прехвърля стойностите в полетата
    private void StudentsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (StudentsGrid.SelectedItem is Student student)
        {
            _selectedId = student.Id;
            NameBox.Text = student.Name;
            ClassBox.Text = student.ClassName;
            GradeBox.Text = student.AverageGrade?
                .ToString("0.00", CultureInfo.InvariantCulture) ?? "";
        }
        else
        {
            _selectedId = null;
        }
        UpdateButtons();
    }

    // INSERT
    private void AddButton_Click(object sender, RoutedEventArgs e)
    {
        if (!TryReadInput(out var name, out var className, out var grade))
            return;

        try
        {
            _db.AddStudent(name, className, grade);
            ClearFields();
            LoadStudents();
            StatusTextBlock.Text = "Ученикът е добавен.";
        }
        catch (Exception ex)
        {
            StatusTextBlock.Text = "Грешка: " + ex.Message;
        }
    }

    // UPDATE
    private void UpdateButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedId is not int id)
        {
            StatusTextBlock.Text = "Изберете ученик от таблицата.";
            return;
        }

        if (!TryReadInput(out var name, out var className, out var grade))
            return;

        try
        {
            _db.UpdateStudent(id, name, className, grade);
            LoadStudents(id);
            StatusTextBlock.Text = "Данните са обновени.";
        }
        catch (Exception ex)
        {
            StatusTextBlock.Text = "Грешка: " + ex.Message;
        }
    }

    // DELETE с потвърждение
    private void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedId is not int id)
        {
            StatusTextBlock.Text = "Изберете ученик, когото да изтриете.";
            return;
        }

        var answer = MessageBox.Show(
            "Сигурни ли сте, че искате да изтриете избрания ученик?",
            "Потвърждение",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (answer != MessageBoxResult.Yes)
            return;

        try
        {
            _db.DeleteStudent(id);
            ClearFields();
            LoadStudents();
            StatusTextBlock.Text = "Ученикът е изтрит.";
        }
        catch (Exception ex)
        {
            StatusTextBlock.Text = "Грешка: " + ex.Message;
        }
    }

    // Допълнителна задача: „Изчисти полетата“
    private void ClearButton_Click(object sender, RoutedEventArgs e)
    {
        ClearFields();
        StatusTextBlock.Text = "Полетата са изчистени.";
    }

    // Допълнителна задача: търсене по име
    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        try
        {
            LoadStudents();
        }
        catch (Exception ex)
        {
            StatusTextBlock.Text = "Грешка: " + ex.Message;
        }
    }

    // ---------- Помощни методи ----------

    // Проверка на входа: празно име/клас и невалидна оценка
    private bool TryReadInput(out string name, out string className, out double grade)
    {
        name = NameBox.Text.Trim();
        className = ClassBox.Text.Trim();
        grade = 0;

        if (string.IsNullOrWhiteSpace(name))
        {
            StatusTextBlock.Text = "Грешка: въведете име на ученика.";
            NameBox.Focus();
            return false;
        }

        if (string.IsNullOrWhiteSpace(className))
        {
            StatusTextBlock.Text = "Грешка: въведете клас.";
            ClassBox.Focus();
            return false;
        }

        // приема и 5.50, и 5,50
        var gradeText = GradeBox.Text.Trim().Replace(',', '.');
        if (!double.TryParse(gradeText, NumberStyles.AllowDecimalPoint,
                             CultureInfo.InvariantCulture, out grade)
            || grade < 2 || grade > 6)
        {
            StatusTextBlock.Text = "Грешка: средният успех трябва да е число между 2 и 6 (напр. 5.50).";
            GradeBox.Focus();
            return false;
        }

        return true;
    }

    private void ClearFields()
    {
        NameBox.Clear();
        ClassBox.Clear();
        GradeBox.Clear();
        StudentsGrid.UnselectAll();
        _selectedId = null;
        UpdateButtons();
    }

    // Допълнителна задача: забрана на „Обнови“/„Изтрий“ без избран ред
    private void UpdateButtons()
    {
        var hasSelection = _selectedId != null;
        UpdateButton.IsEnabled = hasSelection;
        DeleteButton.IsEnabled = hasSelection;
    }
}