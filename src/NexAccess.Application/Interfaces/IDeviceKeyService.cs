namespace NexAccess.Application.Interfaces;

public interface IDeviceKeyService
{
    string GenerateKey();
    string Hash(string key);
}
