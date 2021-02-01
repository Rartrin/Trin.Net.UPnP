using System.Collections;

namespace tPnP
{
	public interface IStaticPortMappingCollection:IEnumerable
	{
		public abstract void Remove(int lExternalPort, string bstrProtocol);

		public abstract IStaticPortMapping Add(int lExternalPort, string bstrProtocol, int lInternalPort, string bstrInternalClient, bool bEnabled, string bstrDescription);
	}
}