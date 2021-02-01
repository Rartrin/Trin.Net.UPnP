namespace tPnP
{
	public interface IStaticPortMapping
	{
		public abstract int InternalPort{get;}
		public abstract string Protocol{get;}
		public abstract string InternalClient{get;}
	}
}
