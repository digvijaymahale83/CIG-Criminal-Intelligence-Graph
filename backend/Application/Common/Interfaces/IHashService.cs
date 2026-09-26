namespace Application.Common.Interfaces;

public interface IHashService
{
    string ComputeSha256Hex(Stream stream);
    string ComputeSha256Hex(byte[] bytes);
}
