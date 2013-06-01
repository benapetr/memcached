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

namespace memcached
{
	public class Terminal
	{
		private static void PrintHelp()
		{
			Console.WriteLine("Usage: memcached [-vhpmuadlc]\n\n" +
			                  "This is an advanced memcache server. See https://github.com/benapetr/memcached for more information.\n\n" +
			                  "Parameters:\n" +
			                  "  -v (--verbose): increase verbosity\n" +
			                  "  -h (--help): display help");
		}

		/// <summary>
		/// Parse the specified args.
		/// </summary>
		/// <param name="args">Arguments.</param>
		public static bool Parse(string[] args)
		{
			List<string> parameters = new List<string>();
			parameters.AddRange(args);
			foreach(string xx in parameters)
			{
				if (xx.StartsWith("-v"))
				{
					Configuration.Verbosity++;
					int curr = 2;
					while (curr < xx.Length)
					{
						if (xx[curr] == 'v')
						{
							Configuration.Verbosity++;
						}
						curr++;
					}
				}else
				{
					switch(xx)
					{
					case "--verbose":
						Configuration.Verbosity++;
						break;
					case "-h":
					case "--help":
						PrintHelp();
						return true;
					}
				}
			}
			return false;
		}
	}
}

