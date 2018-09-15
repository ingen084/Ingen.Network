using System;
using System.IO;
using System.Security.Cryptography;

namespace Ingen.Network
{
	public class AesCryptoService : ICryptoService, IDisposable
	{
		public byte[] IV => CryptoServiceProvider?.IV;
		public byte[] Key => CryptoServiceProvider?.Key;

		private AesCryptoServiceProvider CryptoServiceProvider { get; }
		private ICryptoTransform Encryptor { get; }
		private ICryptoTransform Decryptor { get; }

		public AesCryptoService()
		{
			CryptoServiceProvider = new AesCryptoServiceProvider
			{
				BlockSize = 128,
				KeySize = 256,
				Mode = CipherMode.CBC,
				Padding = PaddingMode.PKCS7,
			};
			CryptoServiceProvider.GenerateIV();
			CryptoServiceProvider.GenerateKey();

			Encryptor = CryptoServiceProvider.CreateEncryptor();
			Decryptor = CryptoServiceProvider.CreateDecryptor();
		}
		public AesCryptoService(byte[] initalVector, byte[] key)
		{
			CryptoServiceProvider = new AesCryptoServiceProvider
			{
				BlockSize = 128,
				KeySize = 256,
				Mode = CipherMode.CBC,
				Padding = PaddingMode.PKCS7,
				IV = initalVector,
				Key = key
			};
			Encryptor = CryptoServiceProvider.CreateEncryptor();
			Decryptor = CryptoServiceProvider.CreateDecryptor();
		}

		//memo ブロックのサイズ超えたら問題が起きるのでは…？

		public byte[] Encrypt(byte[] input)
			=> Encryptor.TransformFinalBlock(input, 0, input.Length);

		public byte[] Decrypt(byte[] input)
			=> Decryptor.TransformFinalBlock(input, 0, input.Length);

		public void Dispose()
		{
			Decryptor.Dispose();
			Encryptor.Dispose();
			CryptoServiceProvider.Dispose();
		}
	}
}
