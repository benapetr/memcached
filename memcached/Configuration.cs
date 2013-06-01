/***************************************************************************
 *   This program is free software; you can redistribute it and/or modify  *
 *   it under the terms of the GNU General Public License as published by  *
 *   the Free Software Foundation; either version 2 of the License, or     *
 *   (at your option) version 3.                                           *
 *                                                                         *
 *   This program is distributed in the hope that it will be useful,       *
 *   but WITHOUT ANY WARRANTY; without even the implied warranty of        *
 *   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the         *
 *   GNU General Public License for more details.                          *
 *                                                                         *
 *   You should have received a copy of the GNU General Public License     *
 *   along with this program; if not, write to the                         *
 *   Free Software Foundation, Inc.,                                       *
 *   51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.         *
 ***************************************************************************/


using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

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
		/// <summary>
		/// The global memory limit in MB
		/// </summary>
		public static int GlobalMemoryLimit = 512;
		public static int InstanceMemoryLimit = 64;
		public static string UserDB = "users";
		public static bool DescriptiveErrors = true;
		public static string ConfigurationFile = null;

		public static void Read()
		{
			if (ConfigurationFile == null)
			{
				return;
			}

			if (!File.Exists(ConfigurationFile))
			{
				MainClass.Log("There is no config file");
				return;
			}
			
			XmlDocument file = new XmlDocument();
			file.Load(ConfigurationFile);
			
			foreach (XmlNode item in file.ChildNodes[0])
			{
				switch(item.Name.ToLower())
				{
					case "authentication":
						Configuration.Authentication = bool.Parse(item.InnerText);
						break;
					case "descriptiveerrors":
						Configuration.DescriptiveErrors = bool.Parse(item.InnerText);
					    break;
					case "port":
						Configuration.Port = int.Parse(item.InnerText);
						break;
					case "globalmemorylimit":
						Configuration.GlobalMemoryLimit = int.Parse(item.InnerText);
						break;
					case "instancememorylimit":
						Configuration.InstanceMemoryLimit = int.Parse(item.InnerText);
						break;
					case "tcp":
						Configuration.TCP = bool.Parse(item.InnerText);
						break;
					case "udp":
						Configuration.UDP = bool.Parse(item.InnerText);
						break;
					case "userdb":
						Configuration.UserDB = item.InnerText;
						break;
					}
			}

		}
	}
}

