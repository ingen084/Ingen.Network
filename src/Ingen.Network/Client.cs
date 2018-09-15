using MessagePack;
using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Ingen.Network
{
	//todo エラートラップ
	public class Client<TBase> : IDisposable
	{
		public event Action<TBase> Received;
		public event Action Disconnected;
		public event Action Connected;

		private TcpClient TcpClient { get; set; }
		private NetworkStream Stream { get; set; }

		private byte[] ReceiveBuffer { get; } = new byte[2048];
		private PacketSplitter Splitter { get; } = new PacketSplitter();
		private CancellationTokenSource TokenSource;

		private Timer HeartbeatTimer { get; set; }
		private int UnReceiveTime { get; set; }
		private int UnSendTime { get; set; }

		private const int PING_SEND_TIME = 10;
		private const int PING_TIMEOUT_TIME = 30;

		public Client()
		{
			TokenSource = new CancellationTokenSource();
			HeartbeatTimer = new Timer(s =>
			{
				UnReceiveTime++;
				UnSendTime++;
				//Console.WriteLine($"{GetHashCode()}: R:{UnReceiveTime} S:{UnSendTime}");

				if (UnReceiveTime >= PING_TIMEOUT_TIME)
				{
					Console.WriteLine("Ping Timeout");
					Disconnect();
					return;
				}
				if (UnSendTime >= PING_SEND_TIME)
				{
					Send(default(TBase)).Wait();
					return;
				}
			}, null, Timeout.Infinite, Timeout.Infinite);
		}
		public Client(TcpClient client) : this()
		{
			TcpClient = client;
			HeartbeatTimer.Change(1000, 1000);
		}
		public async Task Connect(string hostname, int port)
		{
			if (TcpClient?.Connected ?? false)
				TcpClient.Dispose();
			TcpClient = new TcpClient();
			await TcpClient.ConnectAsync(hostname, port);
			HeartbeatTimer.Change(1000, 1000);
			Connected?.Invoke();
			await Receive();
		}

		public async Task Receive()
		{
			Stream = TcpClient.GetStream();
			try
			{
				var count = 0;
				while ((count = await Stream.ReadAsync(ReceiveBuffer, 0, ReceiveBuffer.Length, TokenSource.Token)) > 0)
				{
					UnReceiveTime = 0;
					var result = Splitter.WriteAndSplit(ReceiveBuffer, count);
					Console.WriteLine("Splitted: " + BitConverter.ToString(result));
					if (result == null || result.Length == 0)
						continue;
					Received?.Invoke(LZ4MessagePackSerializer.Deserialize<TBase>(result));
				}
			}
			catch (TaskCanceledException)
			{
			}
			catch (SocketException ex)
			{
				Console.WriteLine("Receive Socket Exception: " + ex);
				Disconnect();
			}
		}
		public async Task Send(TBase data)
		{
			//todo ここなんとかならないかな？
			if (Stream == null)
				return;

			byte[] buffer;

			if (data == null)
				buffer = new byte[4];
			else
			{
				var bytes = await Task.Run(() => LZ4MessagePackSerializer.Serialize(data));

				buffer = new byte[4 + bytes.Length];
				Buffer.BlockCopy(bytes, 0, buffer, 4, bytes.Length);

				var length = BitConverter.GetBytes(bytes.Length);
				Buffer.BlockCopy(length, 0, buffer, 0, 4);
			}

			try
			{
				Console.WriteLine("Send: " + BitConverter.ToString(buffer));
				await Stream.WriteAsync(buffer, 0, buffer.Length);
			}
			catch (SocketException ex)
			{
				Console.WriteLine("Send Socket Exception: " + ex);
				Disconnect();
			}
			UnSendTime = 0;
		}

		public void Disconnect()
		{
			TokenSource.Cancel();
			HeartbeatTimer.Change(Timeout.Infinite, Timeout.Infinite);
			TcpClient.Close();
			Disconnected?.Invoke();
		}

		public void Dispose()
		{
			if (!TokenSource.IsCancellationRequested)
				Disconnect();
			TcpClient?.Dispose();
		}
	}
}
