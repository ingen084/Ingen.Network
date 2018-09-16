using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Ingen.Network
{
	public class RsaCryptoService : ICryptoService, IDisposable
	{
		private RSACryptoServiceProvider CryptoServiceProvider { get; }
		private X509Certificate2 Certificate { get; }

		public RsaCryptoService(string keyFilePath)
		{
			Certificate = new X509Certificate2(keyFilePath);
			CryptoServiceProvider = (RSACryptoServiceProvider)Certificate.PrivateKey;
		}

		public byte[] Encrypt(byte[] input)
			=> CryptoServiceProvider.Encrypt(input, true);

		public byte[] Decrypt(byte[] input)
			=> CryptoServiceProvider.Decrypt(input, true);

		public void Dispose()
		{
			CryptoServiceProvider.Dispose();
			Certificate.Dispose();
		}
	}
}
