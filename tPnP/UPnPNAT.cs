using System;
using System.Collections;
using UwUPnP;

namespace tPnP
{
	public sealed class UPnPNAT:IUPnPNAT
	{
		public IStaticPortMappingCollection StaticPortMappingCollection{get;} = new MappingCollection();

		private record StaticPortMapping
		(
			int InternalPort,
			string Protocol,
			string InternalClient
		):IStaticPortMapping;

		private class MappingCollection:IStaticPortMappingCollection
		{
			public IStaticPortMapping Add(int lExternalPort, string bstrProtocol, int lInternalPort, string bstrInternalClient, bool bEnabled, string bstrDescription)
			{
				UPnP.Open(Enum.Parse<Protocol>(bstrProtocol), (ushort)lExternalPort, (ushort)lInternalPort, bstrDescription);
				return new StaticPortMapping
				(
					InternalPort: lInternalPort,
					Protocol: bstrProtocol,
					InternalClient: bstrInternalClient
				);
			}
			
			public void Remove(int lExternalPort, string bstrProtocol)
			{
				UPnP.Close(Enum.Parse<Protocol>(bstrProtocol), (ushort)lExternalPort);
			}

			public IEnumerator GetEnumerator()
			{
				for(int i=0;;i++)
				{
					StaticPortMapping ret;
					try
					{
						var args = UPnP.GetGenericPortMappingEntry(i);
						ret = new StaticPortMapping
						(
							InternalPort: int.Parse(args["NewInternalPort"]),
							Protocol: args["NewProtocol"],
							InternalClient: args["NewInternalClient"]
						);
					}
					catch
					{
						yield break;
					}
					yield return ret;
				}
			}
		}
	}
}