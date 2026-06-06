using PANApp.DataBase;
using PANApp.Models;
using System;
using System.Collections.Generic;

namespace PANApp.Services.Implementations;

public sealed class NoteService : IDisposable
{
    private readonly SQLiteContext _dbContext;

    public NoteService()
    {
        _dbContext = new SQLiteContext();
    }

    public List<Note> GetAllNotes()
    {
        var notes = new List<Note>();

        try
        {
            using var command = _dbContext.GetConnection().CreateCommand();

            command.CommandText = "SELECT Id, Title, Description, CreatedAt FROM Notes ORDER BY CreatedAt DESC;";

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                notes.Add(new Note
                {
                    Id = Guid.Parse(reader.GetString(0)),
                    Title = reader.GetString(1),
                    Description = reader.GetString(2),
                    CreatedAt = DateTime.Parse(reader.GetString(3)).ToUniversalTime()
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] GetAllNotes: {ex.Message}");
        }

        return notes;
    }

    public Note? GetNoteById(Guid id)
    {
        try
        {
            using var command = _dbContext.GetConnection().CreateCommand();

            command.CommandText = "SELECT Id, Title, Description, CreatedAt FROM Notes WHERE Id = @id;";
            command.Parameters.AddWithValue("@id", id.ToString());

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return new Note
                {
                    Id = Guid.Parse(reader.GetString(0)),
                    Title = reader.GetString(1),
                    Description = reader.GetString(2),
                    CreatedAt = DateTime.Parse(reader.GetString(3)).ToUniversalTime()
                };
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] GetNoteById: {ex.Message}");
        }

        return null;
    }

    public Note AddNote(string title, string description)
    {
        var note = new Note
        {
            Title = title,
            Description = description,
            CreatedAt = DateTime.UtcNow
        };

        try
        {
            using var command = _dbContext.GetConnection().CreateCommand();

            command.CommandText = """
                INSERT INTO Notes (Id, Title, Description, CreatedAt)
                VALUES (@id, @title, @description, @createdAt);
                """;
            command.Parameters.AddWithValue("@id", note.Id.ToString());
            command.Parameters.AddWithValue("@title", note.Title);
            command.Parameters.AddWithValue("@description", note.Description);
            command.Parameters.AddWithValue("@createdAt", note.CreatedAt.ToString("O"));

            command.ExecuteNonQuery();
            Console.WriteLine($"[NOTE] Added: {note.Title} (Id={note.Id})");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] AddNote: {ex.Message}");
        }

        return note;
    }

    public bool UpdateNote(Guid id, string title, string description)
    {
        try
        {
            using var command = _dbContext.GetConnection().CreateCommand();

            command.CommandText = """
                UPDATE Notes
                SET Title = @title, Description = @description
                WHERE Id = @id;
                """;
            command.Parameters.AddWithValue("@id", id.ToString());
            command.Parameters.AddWithValue("@title", title);
            command.Parameters.AddWithValue("@description", description);

            var rowsAffected = command.ExecuteNonQuery();
            Console.WriteLine($"[NOTE] Updated: Id={id}, rows={rowsAffected}");
            return rowsAffected > 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] UpdateNote: {ex.Message}");
            return false;
        }
    }

    public bool DeleteNote(Guid id)
    {
        try
        {
            using var command = _dbContext.GetConnection().CreateCommand();

            command.CommandText = "DELETE FROM Notes WHERE Id = @id;";
            command.Parameters.AddWithValue("@id", id.ToString());

            var rowsAffected = command.ExecuteNonQuery();
            Console.WriteLine($"[NOTE] Deleted: Id={id}, rows={rowsAffected}");
            return rowsAffected > 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] DeleteNote: {ex.Message}");
            return false;
        }
    }

    public void Dispose()
        => _dbContext.Dispose();
}