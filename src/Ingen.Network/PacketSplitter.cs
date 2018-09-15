﻿using System;

namespace Ingen.Network
{
	public class PacketSplitter
	{
		private readonly object _lockObject = new object();
		private byte[] PendingBytes;
		private int PacketSize;

		public byte[] WriteAndSplit(byte[] bytes, int byteCount)
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

				if (buffer.Length >= PacketSize)
				{
					PendingBytes = null;
					byte[] result = new byte[PacketSize];
					Buffer.BlockCopy(buffer, 0, result, 0, PacketSize);
					return result;
				}
				PendingBytes = buffer;
				return null;
			}
		}
	}
}