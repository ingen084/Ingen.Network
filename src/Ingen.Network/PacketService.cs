using System;

namespace Ingen.Network
{
	public class PacketService
	{
		public ICryptoService CryptoService { get; set; }

		private readonly object _lockObject = new object();
		private byte[] PendingBytes;
		private int PacketSize;

		public byte[] ParseAndSplitPacket(byte[] bytes, int byteCount)
		{
			lock (_lockObject)
			{
				if (PendingBytes == null) //初回だった場合は長さを読む
					if (byteCount < 4 || (PacketSize = BitConverter.ToInt32(bytes, 0)) == 0) //ping
						return new byte[] { };

				//合成用バッファ
				byte[] buffer = new byte[(PendingBytes?.Length ?? -4) + byteCount];

				if (PendingBytes != null) //新しいバッファに今までの内容を復元する
					Buffer.BlockCopy(PendingBytes, 0, buffer, 0, PendingBytes.Length);
				//バッファの中身をコピーする
				Buffer.BlockCopy(bytes, PendingBytes == null ? 4 : 0, buffer, PendingBytes?.Length ?? 0, byteCount - (PendingBytes == null ? 4 : 0));

				Console.WriteLine("PacketSize: " + PacketSize + " Received:" + byteCount + " BufferLength:" + buffer.Length);

				try
				{
					if (buffer.Length >= PacketSize)
					{
						PendingBytes = null;
						byte[] result = new byte[PacketSize];
						Buffer.BlockCopy(buffer, 0, result, 0, PacketSize);
						if (CryptoService != null)
							return CryptoService.Decrypt(result);
						return result;
					}
					PendingBytes = buffer;
				}
				catch (Exception ex)
				{
					;
				}
				return null;
			}
		}
		public byte[] MakePacket(byte[] contents)
		{
			if (CryptoService != null)
				contents = CryptoService.Encrypt(contents);

			var buffer = new byte[4 + contents.Length];
			Buffer.BlockCopy(contents, 0, buffer, 4, contents.Length);

			var length = BitConverter.GetBytes(contents.Length);
			Buffer.BlockCopy(length, 0, buffer, 0, 4);

			return buffer;
		}
	}
}
