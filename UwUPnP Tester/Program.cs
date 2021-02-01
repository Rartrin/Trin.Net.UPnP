using System;

namespace UwUPnP.Tester
{
	public static class Program
	{
		public static void Main()
		{
			if(UPnP.IsAvailable)
			{
				Console.WriteLine($"It worked! Local: {UPnP.LocalIP}, External:{UPnP.ExternalIP}");
			}
			else
			{
				Console.WriteLine("It failed");
			}
			
			const ushort port = 25000;
			Console.WriteLine($"IsOpen({Protocol.TCP},{port}) --> {UPnP.IsOpen(Protocol.TCP, port)}");

			Console.WriteLine("Opening Port");
			UPnP.Open(Protocol.TCP, port);
			Console.WriteLine("Opened");

			Console.WriteLine($"IsOpen({Protocol.TCP},{port}) --> {UPnP.IsOpen(Protocol.TCP, port)}");

			Console.ReadKey();
			Console.WriteLine("Closing Port");
			UPnP.Close(Protocol.TCP, port);
			Console.WriteLine("Closed");
		}
	}
}
