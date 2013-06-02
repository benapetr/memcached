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
        /// <summary>
        /// The instance memory limit.
        /// </summary>
        public static int InstanceMemoryLimit = 64;
        /// <summary>
        /// The user D.
        /// </summary>
        public static string UserDB = "users";
        /// <summary>
        /// The descriptive errors.
        /// </summary>
        public static bool DescriptiveErrors = false;
        /// <summary>
        /// The configuration file.
        /// </summary>
        public static string ConfigurationFile = null;
        public static string Path = null;
		public static bool AllowGlobalFlush = true;

        public static double GlobalMemoryLimitByteSize
        {
            get
            {
                return GlobalMemoryLimit * 1024 * 1024;
            }
        }

        public static double InstanceMemoryLimitByteSize
        {
            get
            {
                return InstanceMemoryLimit * 1024 * 1024;
            }
        }

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
                        if (Configuration.UserDB.Contains (System.IO.Path.DirectorySeparatorChar.ToString()))
                        {
                            Configuration.Path = Configuration.UserDB.Substring (0, Configuration.UserDB.LastIndexOf (System.IO.Path.DirectorySeparatorChar));
                        }
                        break;
					case "allowglobalflush":
						Configuration.AllowGlobalFlush = bool.Parse(item.InnerText);
						break;
                }
            }
        }
    }
}

