using System;
using System.IO;
using System.Security.Cryptography;

namespace Ingen.Network
{
	public class AesCryptoService : ICryptoService, IDisposable
	{
		public byte[] IV => CryptoService?.IV;
		public byte[] Key => CryptoService?.Key;

		private AesManaged CryptoService { get; }
		private ICryptoTransform Encryptor { get; }
		private ICryptoTransform Decryptor { get; }

		public AesCryptoService()
		{
			CryptoService = new AesManaged
			{
				BlockSize = 128,
				KeySize = 256,
				Mode = CipherMode.CBC,
				Padding = PaddingMode.PKCS7,
			};
			CryptoService.GenerateIV();
			CryptoService.GenerateKey();

			Encryptor = CryptoService.CreateEncryptor();
			Decryptor = CryptoService.CreateDecryptor();
		}
		public AesCryptoService(byte[] initalVector, byte[] key)
		{
			CryptoService = new AesManaged
			{
				BlockSize = 128,
				KeySize = 256,
				Mode = CipherMode.CBC,
				Padding = PaddingMode.PKCS7,
				IV = initalVector,
				Key = key
			};
			Encryptor = CryptoService.CreateEncryptor();
			Decryptor = CryptoService.CreateDecryptor();
		}

		public byte[] Encrypt(byte[] input)
		{
			using (var outputStream = new MemoryStream())
			using (var cStream = new CryptoStream(outputStream, Encryptor, CryptoStreamMode.Write))
			{
				cStream.Write(input, 0, input.Length);
				cStream.FlushFinalBlock();
				return outputStream.ToArray();
			}
		}

		public byte[] Decrypt(byte[] input)
		{
			using (var outputStream = new MemoryStream())
			using (var cStream = new CryptoStream(outputStream, Decryptor, CryptoStreamMode.Write))
			{
				cStream.Write(input, 0, input.Length);
				cStream.FlushFinalBlock();
				return outputStream.ToArray();
			}
		}

		public void Dispose()
		{
			Decryptor.Dispose();
			Encryptor.Dispose();
			CryptoService.Dispose();
		}
	}
}
