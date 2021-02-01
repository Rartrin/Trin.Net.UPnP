using System;
using System.Collections.Generic;

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
			
			PrintMappings();

			const ushort port = 25000;
			Console.WriteLine($"IsOpen({Protocol.TCP},{port}) --> {UPnP.IsOpen(Protocol.TCP, port)}");

			Console.WriteLine("Opening Port");
			UPnP.Open(Protocol.TCP, port);
			Console.WriteLine("Opened");

			Console.WriteLine($"IsOpen({Protocol.TCP},{port}) --> {UPnP.IsOpen(Protocol.TCP, port)}");

			PrintMappings();

			Console.ReadKey();
			Console.WriteLine("Closing Port");
			UPnP.Close(Protocol.TCP, port);
			Console.WriteLine("Closed");
		}


		private static void PrintMappings()
		{
			Console.WriteLine("--Mappings Start--");
			try
			{
				for(int i=0;;i++)
				{
					try
					{
						var ret = UPnP.GetGenericPortMappingEntry(i);
						if(ret.Count == 0){break;}

						Console.WriteLine(i);
						foreach(var e in ret)
						{
							Console.WriteLine(e);
						}
						Console.WriteLine();
					}
					catch
					{
						break;
					}
				}
			}
			catch(Exception e)
			{
				Console.WriteLine(e);
			}
			Console.WriteLine("--Mappings End--");
		}
	}
}
