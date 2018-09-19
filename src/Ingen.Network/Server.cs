using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Ingen.Network
{
	public class Server<TBase> : IDisposable
	{
		private TcpListener Listener { get; set; }
		private List<Client<TBase>> Clients { get; }

		public event Action<Client<TBase>> ClientConnected;

		public Server(IPEndPoint endpoint)
		{
			Listener = new TcpListener(endpoint);
			Clients = new List<Client<TBase>>();
		}

		public async Task Listen()
		{
			Listener.Start();

			while (true)
			{
				var client = new Client<TBase>(await Listener.AcceptTcpClientAsync());
				client.Disconnected += () =>
				{
					Console.WriteLine("Client Disconnected - " + client.GetHashCode());
					client?.Dispose();
					if (Clients.Contains(client))
						Clients.Remove(client);
					client = null;
				};

				Clients.Add(client);
				ClientConnected?.Invoke(client);
				Console.WriteLine("Client Connected - " + client.GetHashCode());
				client.Receive().GetAwaiter();
			}
		}

		public Task Broadcast(TBase data)
			=> Task.WhenAll(Clients.Select(c => c.Send(data)));

		public void Dispose()
		{
			Task.WhenAll(Clients.Select(c => Task.Run(() => c.Disconnect()))).Wait();
			Listener.Stop();
		}
	}
}
