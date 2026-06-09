using LlamaStudio.Core.Interfaces;
using LlamaStudio.Core.Models;
using System.Text.Json;

namespace LlamaStudio.Infrastructure.Chat;

public class ChatSessionStore
{
    readonly ISettings _settings;
    readonly ILogService _log;

    public ChatSessionStore(ISettings settings, ILogService log)
    {
        _settings = settings;
        _log = log;
    }

    string ChatsDirectory
    {
        get
        {
            var baseDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LlamaStudio");
            return Path.Combine(baseDir, "chats");
        }
    }

   public async Task<List<ChatSession>> LoadAllAsync()
    {
        var sessions = new List<ChatSession>();

        if (!Directory.Exists(ChatsDirectory))
            return sessions;

        var files = Directory.GetFiles(ChatsDirectory, "*.json");

        foreach (var file in files)
        {
            try
            {
                var json = await File.ReadAllTextAsync(file);
                var session = JsonSerializer.Deserialize<ChatSession>(json);
                if (session != null)
                    sessions.Add(session);
            }
            catch (Exception ex)
            {
                _log.Error(ex, $"Failed to load chat session from {file}", "ChatStore");
            }
        }

        sessions.Sort((a, b) => a.Order != b.Order ? a.Order.CompareTo(b.Order) : b.UpdatedAt.CompareTo(a.UpdatedAt));
        return sessions;
    }

    public async Task SaveSessionAsync(ChatSession session)
    {
        try
        {
            if (!Directory.Exists(ChatsDirectory))
                Directory.CreateDirectory(ChatsDirectory);

            session.UpdatedAt = DateTime.Now;
            var json = JsonSerializer.Serialize(session, new JsonSerializerOptions { WriteIndented = true });
            var path = Path.Combine(ChatsDirectory, $"{session.Id}.json");
            await File.WriteAllTextAsync(path, json);
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"Failed to save chat session {session.Id}", "ChatStore");
            throw;
        }
    }

    public async Task DeleteSessionAsync(string sessionId)
    {
        try
        {
            var path = Path.Combine(ChatsDirectory, $"{sessionId}.json");
            if (File.Exists(path))
                File.Delete(path);
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"Failed to delete chat session {sessionId}", "ChatStore");
            throw;
        }
    }

    public async Task ReorderSessionsAsync(List<ChatSession> sessions)
    {
        for (int i = 0; i < sessions.Count; i++)
        {
            sessions[i].Order = i;
            await SaveSessionAsync(sessions[i]);
        }
    }
}
