using System.ComponentModel.DataAnnotations;

namespace Lab_01.Data;

// A single to-do item. This is an EF Core entity: one row per task in the Tasks table.
public class TaskItem
{
    // Primary key. EF Core treats an int "Id" as an auto-incrementing identity column.
    public int Id { get; set; }

    // [MaxLength(100)] tells EF to create the column as nvarchar(100) when it builds
    // the schema. Business validation of the title lives in TaskValidator.
    [MaxLength(100)]
    public string Title { get; set; } = string.Empty;

    public bool IsDone { get; set; }

    // Always stored in UTC so the value means the same thing regardless of server timezone.
    public DateTime CreatedAt { get; set; }
}
