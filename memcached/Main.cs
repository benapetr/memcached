using System;
using System.Threading;

namespace memcached
{
	class MainClass
	{
		/// <summary>
		/// Log the specified text.
		/// </summary>
		/// <param name="text">Text.</param>
		public static void Log(string text)
		{
			Console.WriteLine(DateTime.Now.ToString() + ": " + text);
		}

		/// <summary>
		/// Debug log.
		/// </summary>
		/// <param name="text">Text.</param>
		/// <param name="verbosity">Verbosity.</param>
		public static void DebugLog(string text, int verbosity = 1)
		{
			if (Configuration.Verbosity >= verbosity)
			{
				Log ("DEBUG: " + text);
			}
		}

		public static void Main (string[] args)
		{
			if (!Terminal.Parse (args))
			{
				Log ("Starting sharp memcached server version " + Configuration.Version);
				if (Configuration.Verbosity > 0)
				{
					DebugLog ("Verbosity: " + Configuration.Verbosity.ToString());
				}
				// create a new thread for tcp and start it
				Thread tcp = new Thread(Memcache.ListenTCP);
				tcp.Name = "tcp listener";
				tcp.Start();
				Thread udp = new Thread(Memcache.ListenUDP);
				udp.Name = "udp listener";
				udp.Start();
			}
		}
	}
}
