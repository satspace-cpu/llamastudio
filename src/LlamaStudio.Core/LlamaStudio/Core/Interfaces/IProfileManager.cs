using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LlamaStudio.Core.Models;

namespace LlamaStudio.Core.Interfaces;

public interface IProfileManager
{
	event Action<string>? ProfileChanged;

	Task NotifyProfileChanged(string profileId);

	Task<List<ServerProfile>> GetAllProfilesAsync();

	List<ServerProfile> GetAllProfiles();

	Task<ServerProfile?> GetProfileAsync(string id);

	ServerProfile CreateProfile(string name = "New Profile");

	Task<string> SaveProfileAsync(ServerProfile profile);
	void SaveProfileNow(ServerProfile profile);

	Task<bool> DeleteProfileAsync(string id);

	Task<ServerProfile> DuplicateProfileAsync(string id);

	Task<ServerProfile> ImportProfileAsync(string json);

	string ExportProfile(ServerProfile profile);

	Task SetDefaultProfileAsync(string id);

	Task<ServerProfile?> GetDefaultProfileAsync();
}

/// <summary>r94: helper to resolve the profile the user is currently working with.</summary>
public static class ActiveProfileHelper
{
	public static async Task<ServerProfile?> GetActiveProfileAsync(IProfileManager manager, ISettings settings)
	{
		if (manager == null || settings == null)
		{
			return null;
		}
		if (string.IsNullOrWhiteSpace(settings.LastSelectedProfileId))
		{
			return null;
		}
		return await manager.GetProfileAsync(settings.LastSelectedProfileId);
	}
}
