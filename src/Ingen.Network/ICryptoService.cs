namespace Ingen.Network
{
	public interface ICryptoService
	{
		byte[] Crypt(byte[] input);
		byte[] Decrypt(byte[] input);
	}
}
