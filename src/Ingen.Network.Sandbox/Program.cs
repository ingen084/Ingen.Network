﻿using MessagePack;
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
				server.ClientConnected += c => Console.WriteLine("Server-ClientConnected");
				server.Listen().GetAwaiter();
				Console.WriteLine("Hello World!");
				using (var client = new Client<IMessageBase>())
				{
					client.Received += m => Console.WriteLine($"Received: {(m as Message).StringMessage}");
					client.Connected += () => Console.WriteLine("Client-Connected");
					client.Disconnected += () => Console.WriteLine("Client-DisConnected");
					client.Connect("localhost", 1234).GetAwaiter();

					string text;
					while (!string.IsNullOrEmpty(text = Console.ReadLine()))
						server.Broadcast(new Message { StringMessage = text }).Wait();
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