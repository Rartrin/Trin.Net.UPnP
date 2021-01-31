using System;

namespace UwUPnP.Tester
{
	public static class Program
	{
		public static void Main()
		{
			//if(UPnPLib.UPnP.IsUPnPAvailable())
			if(UPnP.IsAvailable)
			{
				Console.WriteLine($"It worked! Local: {UPnP.LocalIP}, External:{UPnP.ExternalIP}");
			}
			else
			{
				Console.WriteLine("It failed");
			}
			//const ushort port = 25000;
			//Console.WriteLine("Start");
			//UPnP.Open(PortType.TCP, port);

			//if(UPnP.IsMapped(PortType.TCP, port))
			//{
			//	Console.WriteLine("It worked");
			//}
			//else
			//{
			//	Console.WriteLine("It failed");
			//}

			//Console.ReadKey();
			//UPnP.Close(PortType.TCP, port);
			//Console.WriteLine("End");
		}
	}
}
