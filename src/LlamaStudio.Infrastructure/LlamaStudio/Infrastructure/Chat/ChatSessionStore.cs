using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using LlamaStudio.Core.Interfaces;
using LlamaStudio.Core.Models;

namespace LlamaStudio.Infrastructure.Chat;

public class ChatSessionStore
{
	private readonly ISettings _settings;

	private readonly ILogService _log;

	private static readonly byte[] _dpapiEntropy = Encoding.UTF8.GetBytes("LlamaStudio_SessionEncryption_v1");

	private string ChatsDirectory => Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LlamaStudio"), "chats");

	public ChatSessionStore(ISettings settings, ILogService log)
	{
		_settings = settings;
		_log = log;
	}

	public async Task<List<ChatSession>> LoadAllAsync()
	{
		List<ChatSession> sessions = new List<ChatSession>();
		if (!Directory.Exists(ChatsDirectory))
		{
			return sessions;
		}
		string[] files = Directory.GetFiles(ChatsDirectory, "*.json");
		string[] array = files;
		foreach (string file in array)
		{
			try
			{
				string text = await File.ReadAllTextAsync(file);
				string json = ((!IsEncrypted(text)) ? text : DecryptData(text));
				ChatSession chatSession = JsonSerializer.Deserialize<ChatSession>(json);
				if (chatSession != null)
				{
					sessions.Add(chatSession);
				}
			}
			catch (Exception exception)
			{
				_log.Error(exception, "Failed to load chat session from " + file, "ChatStore");
			}
		}
		sessions.Sort((ChatSession a, ChatSession b) => (a.Order == b.Order) ? b.UpdatedAt.CompareTo(a.UpdatedAt) : a.Order.CompareTo(b.Order));
		return sessions;
	}

	public async Task SaveSessionAsync(ChatSession session)
	{
		try
		{
			if (!Directory.Exists(ChatsDirectory))
			{
				Directory.CreateDirectory(ChatsDirectory);
			}
			session.UpdatedAt = DateTime.Now;
			string text = JsonSerializer.Serialize(session, new JsonSerializerOptions
			{
				WriteIndented = true
			});
			string path = Path.Combine(ChatsDirectory, session.Id + ".json");
			string contents = ((!_settings.EncryptSessions) ? text : EncryptData(text));
			await File.WriteAllTextAsync(path, contents);
		}
		catch (Exception exception)
		{
			_log.Error(exception, "Failed to save chat session " + session.Id, "ChatStore");
			throw;
		}
	}

	public async Task DeleteSessionAsync(string sessionId)
	{
		try
		{
			string path = Path.Combine(ChatsDirectory, sessionId + ".json");
			if (File.Exists(path))
			{
				File.Delete(path);
			}
		}
		catch (Exception exception)
		{
			_log.Error(exception, "Failed to delete chat session " + sessionId, "ChatStore");
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

	private bool IsEncrypted(string content)
	{
		return content.TrimStart().StartsWith("LS_ENC:", StringComparison.Ordinal);
	}

	private string EncryptData(string plainText)
	{
		try
		{
			byte[] inArray = ProtectedData.Protect(Encoding.UTF8.GetBytes(plainText), _dpapiEntropy, DataProtectionScope.CurrentUser);
			return "LS_ENC:" + Convert.ToBase64String(inArray);
		}
		catch (Exception exception)
		{
			_log.Error(exception, "Failed to encrypt session data, saving as plaintext", "ChatStore");
			return plainText;
		}
	}

	private string DecryptData(string encryptedContent)
	{
		try
		{
			byte[] bytes = ProtectedData.Unprotect(Convert.FromBase64String(encryptedContent.Substring("LS_ENC:".Length).Trim()), _dpapiEntropy, DataProtectionScope.CurrentUser);
			return Encoding.UTF8.GetString(bytes);
		}
		catch (Exception exception)
		{
			_log.Error(exception, "Failed to decrypt session data", "ChatStore");
			throw;
		}
	}
}
