using MessagePack;
using System;
using System.IO;
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
		private PacketService PacketService { get; } = new PacketService();
		private CancellationTokenSource TokenSource;

		public ICryptoService CryptoService
		{
			get => PacketService?.CryptoService;
			set => PacketService.CryptoService = value;
		}

		private Timer HeartbeatTimer { get; set; }
		private int UnReceiveTime { get; set; }
		private int UnSendTime { get; set; }

		private const int PING_SEND_TIME = 10;
		private const int PING_TIMEOUT_TIME = 30;

		public Client()
		{
			HeartbeatTimer = new Timer(s =>
			{
				UnReceiveTime++;
				UnSendTime++;

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
			TokenSource = new CancellationTokenSource();
		}
		public async Task Connect(string hostname, int port)
		{
			if (TcpClient?.Connected ?? false)
				TcpClient.Dispose();
			TcpClient = new TcpClient();
			await TcpClient.ConnectAsync(hostname, port);
			Connected?.Invoke();
			TokenSource = new CancellationTokenSource();
			await Receive();
		}

		internal async Task Receive()
		{
			try
			{
				Stream = TcpClient.GetStream();
				HeartbeatTimer.Change(1000, 1000);
				UnReceiveTime = 0;
				UnSendTime = 0;

				var count = 0;
				while ((count = await Stream.ReadAsync(ReceiveBuffer, 0, ReceiveBuffer.Length, TokenSource.Token)) > 0)
				{
					UnReceiveTime = 0;
					//Console.WriteLine("Receive: " + BitConverter.ToString(ReceiveBuffer, 0, count));
					var result = PacketService.ParseAndSplitPacket(ReceiveBuffer, count);
					//Console.WriteLine("Splitted: " + BitConverter.ToString(result));
					if (result == null || result.Length == 0)
						continue;
					Received?.Invoke(LZ4MessagePackSerializer.Deserialize<TBase>(result));
				}
			}
			catch (Exception ex) when (ex is TaskCanceledException || ex is IOException || ex is SocketException)
			{
				Disconnect();
			}
			catch (Exception ex)
			{
				Console.WriteLine("Receive General Exception: " + ex);
				Disconnect();
			}
			finally
			{
				HeartbeatTimer.Change(Timeout.Infinite, Timeout.Infinite);
			}
		}
		public async Task Send(TBase data)
		{
			try
			{
				//todo ここなんとかならないかな？
				if (!(Stream?.CanWrite ?? false))
				{
					Console.WriteLine("書き込み不可");
					Disconnect();
					return;
				}

				byte[] buffer;

				if (data == null)
					buffer = new byte[2];
				else
					buffer = PacketService.MakePacket(await Task.Run(() => LZ4MessagePackSerializer.Serialize(data)));

				await Stream.WriteAsync(buffer, 0, buffer.Length);
				UnSendTime = 0;
			}
			catch (Exception ex) when (ex is IOException || ex is SocketException)
			{
				Disconnect();
			}
			catch (Exception ex)
			{
				Console.WriteLine("Send General Exception: " + ex);
				Disconnect();
			}
		}

		public void Disconnect()
		{
			TokenSource.Cancel();
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
