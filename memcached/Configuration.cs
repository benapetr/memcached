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

