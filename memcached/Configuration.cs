using System;

namespace memcached
{
	public class Configuration
	{
		/// <summary>
		/// verbosity
		/// </summary>
		public static int Verbosity = 0;
		/// <summary>
		/// The version.
		/// </summary>
		public static string Version = "1.0.0";
		/// <summary>
		/// The authentication.
		/// </summary>
		public static bool Authentication = true;
		/// <summary>
		/// The port.
		/// </summary>
		public static int Port = 11211;
		/// <summary>
		/// TCP
		/// </summary>
		public static bool TCP = true;
		/// <summary>
		/// UDP
		/// </summary>
		public static bool UDP = false;
	}
}

