using LlamaStudio.Core.Models;

namespace LlamaStudio.Core.Interfaces;

public interface IProfileManager
{
    event Action<string>? ProfileChanged;
    void NotifyProfileChanged(string profileId);

    Task<List<ServerProfile>> GetAllProfilesAsync();
    List<ServerProfile> GetAllProfiles();
    Task<ServerProfile?> GetProfileAsync(string id);
    ServerProfile CreateProfile(string name = "New Profile");
    Task<string> SaveProfileAsync(ServerProfile profile);
    Task<bool> DeleteProfileAsync(string id);
    Task<ServerProfile> DuplicateProfileAsync(string id);
    Task<ServerProfile> ImportProfileAsync(string json);
    string ExportProfile(ServerProfile profile);
    Task SetDefaultProfileAsync(string id);
    Task<ServerProfile?> GetDefaultProfileAsync();
}
