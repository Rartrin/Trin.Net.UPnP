using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Xml.Linq;

namespace UwUPnP
{
	//http://www.upnp-hacks.org/igd.html

	internal class Gateway
	{
		private readonly IPAddress client;

		private readonly string serviceType = null;
		private readonly string controlURL = null;

		public Gateway(IPAddress ip, string data)
		{
			client = ip;

			string location = GetLocation(data);

			(serviceType, controlURL) = GetInfo(location);
		}

		private static string GetLocation(string data)
		{
			var lines = data.Split('\n').Select(l=>l.Trim()).Where(l=>l.Length>0);

			foreach(string line in lines)
			{
				if(line.StartsWith("HTTP/1.") || line.StartsWith("NOTIFY *"))
					continue;

				int colonIndex = line.IndexOf(':');

				if(colonIndex<0)
					continue;

				string name = line[..colonIndex];
				string val = line.Length >= name.Length ? line[(colonIndex + 1)..].Trim() : null;

				if(name.ToLowerInvariant() == "location")
				{
					if(val.IndexOf('/', 7) == -1)// finds the first slash after http://
					{
						throw new Exception("Unsupported Gateway");
					}
					return val;
				}
			}
			throw new Exception("Unsupported Gateway");
		}

		private static (string serviceType, string controlURL) GetInfo(string location)
		{
			XDocument doc = XDocument.Load(location);
			var services =  doc.Descendants().Where(d => d.Name.LocalName == "service");

			(string serviceType,string controlURL) ret = (null,null);

			foreach(XElement service in services)
			{
				string serviceType = null;
				string controlURL = null;

				foreach(var node in service.Nodes())
				{
					if(node is not XElement ele || ele.FirstNode is not XText n)
					{
						continue;
					}

					switch(ele.Name.LocalName.Trim().ToLowerInvariant())
					{
						case "servicetype": serviceType = n.Value.Trim(); break;
						case "controlurl": controlURL = n.Value.Trim(); break;
					}
				}

				if(serviceType is not null && controlURL is not null)
				{
					if(serviceType.Contains(":wanipconnection:", StringComparison.InvariantCultureIgnoreCase) || serviceType.Contains(":wanpppconnection:", StringComparison.InvariantCultureIgnoreCase))
					{
						ret.serviceType = serviceType;
						ret.controlURL = controlURL;
					}
				}
			}

			if(ret.controlURL is null)
			{
				throw new Exception("Unsupported Gateway");
			}

			if(!ret.controlURL.StartsWith('/'))
			{
				ret.controlURL = "/" + ret.controlURL;
			}

			int slash = location.IndexOf('/', 7); // finds the first slash after http://

			ret.controlURL = location[0..slash] + ret.controlURL;

			return ret;
		}

		private static string BuildArgString(IEnumerable<(string Key, object Value)> args) => string.Concat(args.Select(a => $"<{a.Key}>{a.Value}</{a.Key}>"));

		private Dictionary<string, string> Command(string action, PortType type, int port, params (string Key, object Value)[] args)
		{
			var actionArgs = args.ToList();
			if(type != PortType.Unknown)
			{
				actionArgs.Add(("NewRemoteHost", ""));
				actionArgs.Add(("NewProtocol", type));
				actionArgs.Add(("NewExternalPort", port));
			}

			string soap = string.Concat
			(
				"<?xml version=\"1.0\"?>\n",
				"<SOAP-ENV:Envelope xmlns:SOAP-ENV=\"http://schemas.xmlsoap.org/soap/envelope/\" SOAP-ENV:encodingStyle=\"http://schemas.xmlsoap.org/soap/encoding/\">",
					"<SOAP-ENV:Body>",
						$"<m:{action} xmlns:m=\"{serviceType}\">",
							BuildArgString(actionArgs),
						$"</m:{action}>",
					"</SOAP-ENV:Body>",
				"</SOAP-ENV:Envelope>"
			);

			byte[] req = Encoding.ASCII.GetBytes(soap);

			HttpWebRequest request = (HttpWebRequest) WebRequest.Create(controlURL);

			request.Method = "POST";
			request.ContentType = "text/xml";
			//request.Connection = "Close";
			request.ContentLength = req.Length;
			request.Headers.Add("SOAPAction", $"\"{serviceType}#{action}\"");

			Stream requestStream = request.GetRequestStream();
			requestStream.Write(req);
			requestStream.Close();

			Dictionary<string, string> ret = GetResponse(request);

			if(ret.TryGetValue("errorCode", out string errorCode))
			{
				throw new Exception(errorCode);
			}

			return ret;
		}

		private static Dictionary<string, string> GetResponse(HttpWebRequest request)
		{
			using HttpWebResponse response = (HttpWebResponse)request.GetResponse();
			if(response.StatusCode != HttpStatusCode.OK)
			{
				return null;
			}

			Dictionary<string, string> ret = new Dictionary<string, string>();

			XDocument doc = XDocument.Load(response.GetResponseStream());

			if(doc is null)
			{
				throw new NullReferenceException("XML Document is null");
			}

			foreach(XNode node in doc.DescendantNodes())
			{
				if(node is XElement ele && ele.FirstNode is XText txt)
				{
					ret[ele.Name.LocalName] = txt.Value;
				}
			}

			return ret;
		}

		public IPAddress LocalIP => client;

		public IPAddress ExternalIP => Command("GetExternalIPAddress", default, default).TryGetValue("NewExternalIPAddress", out string ret) ? IPAddress.Parse(ret) : null;

		public void Open(PortType type, ushort port) => Command("AddPortMapping", type, port,
			("NewInternalClient", client),
			("NewInternalPort", port),
			("NewEnabled", 1),
			("NewPortMappingDescription", "UPnPLib"),
			("NewLeaseDuration", 0)
		);

		public void Close(PortType type, ushort port) => Command("DeletePortMapping", type, port);

		public bool IsMapped(PortType type, ushort port) => Command("GetSpecificPortMappingEntry", type, port).ContainsKey("NewInternalPort");
	}
}