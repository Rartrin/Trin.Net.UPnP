using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace UwUPnP
{
	public enum PortType
	{
		Unknown = 0,
		TCP = 1,
		UDP = 2
	}
	public class UPnP
	{
		private static bool searching = true;
		private static Gateway defaultGateway = null;

		static UPnP()
		{
			Find();
		}

		private static Gateway Gateway
		{
			get
			{
				while(searching)
				{
					Thread.Sleep(1);
				}

				return defaultGateway;
			}
		}

		public static bool IsAvailable => Gateway is not null;

		public static void Open(PortType type, ushort port) => Gateway?.Open(type, port);

		public static void Close(PortType type, ushort port) => Gateway?.Close(type, port);

		public static bool IsMapped(PortType type, ushort port) => Gateway?.IsMapped(type, port) ?? false;

		public static IPAddress ExternalIP => Gateway?.ExternalIP;

		public static IPAddress LocalIP => Gateway?.LocalIP;


		private static readonly string[] searchMessageTypes = new[]
		{
			"urn:schemas-upnp-org:device:InternetGatewayDevice:1",
			"urn:schemas-upnp-org:service:WANIPConnection:1",
			"urn:schemas-upnp-org:service:WANPPPConnection:1"
		};

		private static void Find()
		{
			List<Task> listeners = new List<Task>();

			foreach(var ip in GetLocalIPs())
			{
				foreach(string type in searchMessageTypes)
				{
					listeners.Add(StartListener(ip, type));
				}
			}

			Task.WhenAll(listeners).ContinueWith(t => searching = false);
		}

		private static async Task StartListener(IPAddress ip, string type)
		{
			await Task.Run(() =>
			{
				IPEndPoint endPoint = IPEndPoint.Parse("239.255.255.250:1900");

				string request = string.Join
				(
					"\n",

					"M-SEARCH * HTTP/1.1",
					$"HOST: {endPoint}",
					$"ST: {type}",
					"MAN: \"ssdp:discover\"",
					"MX: 2",
					"",""//End with double newlines
				);
				byte[] req = Encoding.ASCII.GetBytes(request);

				Socket socket = new Socket(ip.AddressFamily, SocketType.Dgram, ProtocolType.Udp)//was AddressFamily.InterNetwork
				{
					ReceiveTimeout = 3000,
					SendTimeout = 3000
				};

				socket.Bind(new IPEndPoint(ip, 0));
				try
				{
					socket.SendTo(req, endPoint);
				}
				catch
				{
					return;
				}

				byte[] buffer = new byte[0x600];

				try
				{
					int count = socket.Receive(buffer);

					string recv = Encoding.ASCII.GetString(buffer, 0, count);
					Gateway gateway = new Gateway(ip, recv);
					Interlocked.CompareExchange(ref defaultGateway, gateway, null);
					searching = false;
				}
				catch(Exception e)
				{
					Console.WriteLine($"[{ip}] {e}");
					return;
				}
			});
		}

		private static IEnumerable<IPAddress> GetLocalIPs() => NetworkInterface.GetAllNetworkInterfaces().Where(IsValidInterface).SelectMany(GetValidNetworkIPs);

		private static bool IsValidInterface(NetworkInterface network)
		{
			bool virt = false; // TODO: check if virtual
			NetworkInterfaceType type = network.NetworkInterfaceType;
			return network.OperationalStatus == OperationalStatus.Up && type != NetworkInterfaceType.Loopback && type != NetworkInterfaceType.Ppp && !virt;
		}

		private static IEnumerable<IPAddress> GetValidNetworkIPs(NetworkInterface network)
		{
			return network.GetIPProperties().UnicastAddresses.Select(a => a.Address).Where(a => a.AddressFamily == AddressFamily.InterNetwork || a.AddressFamily == AddressFamily.InterNetworkV6));
		}
	}
}