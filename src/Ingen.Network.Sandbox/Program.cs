using MessagePack;
using System;
using System.Net;

namespace Ingen.Network.Sandbox
{
	class Program
	{
		static void Main(string[] args)
		{
			using (var server = new Server<IMessageBase>(new IPEndPoint(IPAddress.Loopback, 1234)))
			{
				var serverCryptoService = new AesCryptoService();
				var clientCryptoService = new AesCryptoService(serverCryptoService.IV, serverCryptoService.Key);

				server.ClientConnected += c =>
				{
					Console.WriteLine("Server-Connected");
					c.CryptoService = serverCryptoService;
					c.Received += m => Console.WriteLine($"Server-Received: {(m as Message).StringMessage}");
				};
				server.Listen().GetAwaiter();
				Console.WriteLine("Hello World!");
				using (var client = new Client<IMessageBase>())
				{
					client.Received += m => Console.WriteLine($"Received: {(m as Message).StringMessage}");
					client.Connected += () =>
					{
						Console.WriteLine("Client-Connected");
						client.CryptoService = clientCryptoService;
					};
					client.Disconnected += () => Console.WriteLine("Client-Disconnected");
					client.Connect("localhost", 1234).GetAwaiter();

					string text;
					while (!string.IsNullOrEmpty(text = Console.ReadLine()))
					{
						if (text.StartsWith(" "))
						{
							server.Broadcast(new Message { StringMessage = text }).Wait();
							continue;
						}
						client.Send(new Message { StringMessage = text }).Wait();
					}
				}
			}
		}
	}

	[Union(0, typeof(Message))]
	public interface IMessageBase
	{
	}
	[MessagePackObject]
	public class Message : IMessageBase
	{
		[Key(0)]
		public string StringMessage { get; set; }
	}
}
