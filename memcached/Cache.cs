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


using System.Collections.Generic;
using System;

namespace memcached
{
	public class Cache
	{
		public class Item
		{
			public string value = null;
			public DateTime expiry;
			public int flags = 0;

			public Item(string data, int Expiry, int Flags)
			{
				value = data;
				flags = Flags;
				expiry = DateTime.Now.AddSeconds (Expiry);
			}
		}
		private Dictionary<string, Item> db = new Dictionary<string, Item>();

		/// <summary>
		/// Initializes a new instance of the <see cref="memcached.Cache"/> class.
		/// </summary>
		public Cache ()
		{

		}

		private void hardSet(string id, Item data)
		{
			lock (db)
			{
				if (!db.ContainsKey (id))
				{
					db.Add(id, data);
					return;
				}
				db[id] = data;
			}
		}

		/// <summary>
		/// Set the specified key and value.
		/// </summary>
		/// <param name="key">Key.</param>
		/// <param name="value">Value.</param>
		public bool Set(string key, Item value)
		{
			hardSet (key, value);
			return true;
		}

		/// <summary>
		/// Get the specified key.
		/// </summary>
		/// <param name="key">Key.</param>
		public Item Get(string key)
		{
			lock (db)
			{
				if (db.ContainsKey(key))
				{
					return db[key];
				}
			}

			return null;
		}

		/// <summary>
		/// Delete the specified key.
		/// </summary>
		/// <param name="key">Key.</param>
		public bool Delete(string key)
		{
			lock(db)
			{
				if (db.ContainsKey(key))
				{
					db.Remove (key);
					return true;
				}
				return false;
			}
		}

		/// <summary>
		/// Add the specified key.
		/// </summary>
		/// <param name="key">Key.</param>
		/// <param name="d">D.</param>
		public bool Add(string key, Item d)
		{
			lock (db)
			{
				if (!db.ContainsKey (key))
				{
					db.Add (key, d);
					return true;
				}
			}
			return false;
		}
	}
}

