using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Ingen.Network
{
	public class RsaCryptoService : ICryptoService, IDisposable
	{
		private RSACryptoServiceProvider CryptoServiceProvider { get; }
		private X509Certificate2 Certificate { get; }

		/// <param name="keyFilePath">pemファイル</param>
		public RsaCryptoService(string keyFilePath)
		{
			RSAParameters parameter;
			using (var reader = new StreamReader(keyFilePath))
			{
				var pemReader = new Org.BouncyCastle.OpenSsl.PemReader(reader);
				var pem = pemReader.ReadObject();

				if (pem is AsymmetricCipherKeyPair privateKeyPair)
				{
					var rsaParameters = privateKeyPair?.Private as RsaPrivateCrtKeyParameters;

					parameter = new RSAParameters
					{
						D = CheckAndRemoveHeadNull(rsaParameters.Exponent?.ToByteArray()),
						DP = CheckAndRemoveHeadNull(rsaParameters.DP?.ToByteArray()),
						DQ = CheckAndRemoveHeadNull(rsaParameters.DQ?.ToByteArray()),
						Exponent = CheckAndRemoveHeadNull(rsaParameters.PublicExponent?.ToByteArray()),
						InverseQ = CheckAndRemoveHeadNull(rsaParameters.QInv?.ToByteArray()),
						Modulus = CheckAndRemoveHeadNull(rsaParameters.Modulus?.ToByteArray()),
						P = CheckAndRemoveHeadNull(rsaParameters.P?.ToByteArray()),
						Q = CheckAndRemoveHeadNull(rsaParameters.Q?.ToByteArray()),
					};
				}
				else if (pem is RsaKeyParameters publicKeyPair)
					parameter = new RSAParameters
					{
						Exponent = CheckAndRemoveHeadNull(publicKeyPair.Exponent?.ToByteArray()),
						Modulus = CheckAndRemoveHeadNull(publicKeyPair.Modulus?.ToByteArray()),
					};
				else
					throw new Exception("鍵の解析に失敗しました。");
			}
			CryptoServiceProvider = new RSACryptoServiceProvider();
			CryptoServiceProvider.ImportParameters(parameter);
		}
		private byte[] CheckAndRemoveHeadNull(byte[] input)
		{
			if (input == null)
				return null;

			if (input.Length > 0 && input[0] == 0)
			{
				var output = new byte[input.Length - 1];
				Buffer.BlockCopy(input, 1, output, 0, output.Length);
				return output;
			}

			return input;
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
