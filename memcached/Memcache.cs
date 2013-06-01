using System;

namespace memcached
{
	public class Memcache
	{
		/// <summary>
		/// Listen this instance.
		/// </summary>
		public static void ListenUDP()
		{
			if (!Configuration.UDP)
			{
				return;
			}
		}

		public static void ListenTCP()
		{
			if (!Configuration.TCP)
			{
				return;
			}
		}
	}
}

