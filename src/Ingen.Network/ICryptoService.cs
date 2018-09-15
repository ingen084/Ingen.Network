namespace Ingen.Network
{
	public interface ICryptoService
	{
		byte[] Encrypt(byte[] input);
		byte[] Decrypt(byte[] input);
	}
}
