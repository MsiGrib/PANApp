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

    public List<Note> GetNotesByModule(string profileId, string moduleId)
    {
        var notes = new List<Note>();

        try
        {
            using var command = _dbContext.GetConnection().CreateCommand();
            command.CommandText = """
                SELECT Id, NoteNumber, Title, Description, CreatedAt, ProfileId, ModuleId 
                FROM Notes 
                WHERE ProfileId = @profileId AND ModuleId = @moduleId
                ORDER BY NoteNumber DESC;
                """;
            command.Parameters.AddWithValue("@profileId", profileId);
            command.Parameters.AddWithValue("@moduleId", moduleId);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                notes.Add(ReadNote(reader));
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] GetNotesByModule: {ex.Message}");
        }

        return notes;
    }

    public List<Note> GetNotesByProfile(string profileId)
    {
        var notes = new List<Note>();

        try
        {
            using var command = _dbContext.GetConnection().CreateCommand();
            command.CommandText = """
                SELECT Id, NoteNumber, Title, Description, CreatedAt, ProfileId, ModuleId 
                FROM Notes 
                WHERE ProfileId = @profileId
                ORDER BY NoteNumber DESC;
                """;
            command.Parameters.AddWithValue("@profileId", profileId);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                notes.Add(ReadNote(reader));
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] GetNotesByProfile: {ex.Message}");
        }

        return notes;
    }

    public int GetNoteCountByModule(string profileId, string moduleId)
    {
        try
        {
            using var command = _dbContext.GetConnection().CreateCommand();
            command.CommandText = """
                SELECT COUNT(*) FROM Notes 
                WHERE ProfileId = @profileId AND ModuleId = @moduleId;
                """;
            command.Parameters.AddWithValue("@profileId", profileId);
            command.Parameters.AddWithValue("@moduleId", moduleId);

            return Convert.ToInt32(command.ExecuteScalar());
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] GetNoteCountByModule: {ex.Message}");
            return 0;
        }
    }

    public int GetNoteCountByProfile(string profileId)
    {
        try
        {
            using var command = _dbContext.GetConnection().CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM Notes WHERE ProfileId = @profileId;";
            command.Parameters.AddWithValue("@profileId", profileId);

            return Convert.ToInt32(command.ExecuteScalar());
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] GetNoteCountByProfile: {ex.Message}");
            return 0;
        }
    }

    public Note AddNoteToModule(string profileId, string moduleId, string moduleName, string title, string description)
    {
        var noteNumber = GetNextNoteNumber(profileId);

        var note = new Note
        {
            Title = title,
            Description = description,
            CreatedAt = DateTime.UtcNow,
            ProfileId = profileId,
            ModuleId = moduleId,
            ModuleName = moduleName,
            NoteNumber = noteNumber
        };

        try
        {
            using var command = _dbContext.GetConnection().CreateCommand();
            command.CommandText = """
                INSERT INTO Notes (Id, NoteNumber, Title, Description, CreatedAt, ProfileId, ModuleId)
                VALUES (@id, @noteNumber, @title, @description, @createdAt, @profileId, @moduleId);
                """;
            command.Parameters.AddWithValue("@id", note.Id.ToString());
            command.Parameters.AddWithValue("@noteNumber", note.NoteNumber);
            command.Parameters.AddWithValue("@title", note.Title);
            command.Parameters.AddWithValue("@description", note.Description);
            command.Parameters.AddWithValue("@createdAt", note.CreatedAt.ToString("O"));
            command.Parameters.AddWithValue("@profileId", profileId);
            command.Parameters.AddWithValue("@moduleId", moduleId);

            command.ExecuteNonQuery();
            Console.WriteLine($"[NOTE] Added #{note.NoteNumber} to module {moduleId}: {note.Title}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] AddNoteToModule: {ex.Message}");
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

    private long GetNextNoteNumber(string profileId)
    {
        try
        {
            using var command = _dbContext.GetConnection().CreateCommand();
            command.CommandText = """
                SELECT COALESCE(MAX(NoteNumber), 0) + 1 
                FROM Notes WHERE ProfileId = @profileId;
                """;
            command.Parameters.AddWithValue("@profileId", profileId);

            var result = command.ExecuteScalar();
            return result != null ? Convert.ToInt64(result) : 1;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] GetNextNoteNumber: {ex.Message}");
            return 1;
        }
    }

    private static Note ReadNote(Microsoft.Data.Sqlite.SqliteDataReader reader)
        => new Note
        {
            Id = Guid.Parse(reader.GetString(0)),
            NoteNumber = reader.GetInt64(1),
            Title = reader.GetString(2),
            Description = reader.GetString(3),
            CreatedAt = DateTime.Parse(reader.GetString(4)).ToUniversalTime(),
            ProfileId = reader.GetString(5),
            ModuleId = reader.GetString(6),
            ModuleName = string.Empty
        };

    public void Dispose()
        => _dbContext.Dispose();
}