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
            private static double unique = 0;
            public string value = null;
            public DateTime expiry;
            public int flags = 0;
            public DateTime update;
            public double cas;
			private double size = 0;

            public Item(string data, int Expiry, int Flags)
            {
                value = data;
                flags = Flags;
                expiry = DateTime.Now.AddSeconds (Expiry);
                update = DateTime.Now;
                if (Expiry == 0)
                {
                    expiry = DateTime.MaxValue;
                }
                lock (MainClass.GlobalUser)
                {
                    unique++;
                    cas = unique;
                }
            }

            /// <summary>
            /// Gets the size.
            /// </summary>
            /// <returns>The size.</returns>
            public double getSize()
            {
				if (size != 0)
				{
					return size;
				}
                unsafe
                {
                    double xx = (sizeof(DateTime) * 2) + sizeof(int) + (2 * IntPtr.Size) + (sizeof(double) * 2);
                
                    if (value == null)
                    {
						size = xx;
						return size;
                    }
					size = xx + (sizeof(char) * value.Length);
					return size;
                }
            }
        }

        private Dictionary<string, Item> db = new Dictionary<string, Item>();

		private static double globalSize = 0;

		public static double GlobalSize
		{
			get
			{
				return globalSize;
			}
		}

        private double size = 0;

        /// <summary>
        /// Gets the size
        /// </summary>
        /// <value>The size.</value>
        public double Size
        {
            get
            {
                return size;
            }
        }

        public static double getGlobalSize()
        {
            double s = 0;
            lock (MainClass.GlobalCaches)
            {
                foreach (Cache cache in MainClass.GlobalCaches.Values)
                {
                    s = s + cache.Size;
                }
            }
            return s;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="memcached.Cache"/> class.
        /// </summary>
        public Cache ()
        {
            size = 4;
			globalSize += 4;
        }

		~Cache()
		{
			globalSize -= 4;
		}

        public void Clear()
        {
            lock(db)
            {
				globalSize -= size - 4;
                db.Clear();
                size = 4;
            }
        }

        public void CleanOld()
        {
            lock (db)
            {
                List<string> rm = new List<string>();
                foreach (KeyValuePair<string,Item> item in db)
                {
                    if (item.Value.expiry < DateTime.Now)
                    {
                        rm.Add(item.Key);
                    }
                }
                foreach (string item in rm)
                {
                    db.Remove(item);
                }
            }
        }

		public int Count()
		{
			lock (db)
			{
				return db.Count;
			}
		}

        private void hardSet(string id, Item data)
        {
            lock (db)
            {
                if (!db.ContainsKey (id))
                {
                    db.Add(id, data);
                    size += data.getSize();
                    return;
                }
				double s = db[id].getSize();
                size = size - s;
				globalSize -= s;
				double s2 = data.getSize();
				globalSize += s2;
				size += s2;
                db[id].update = DateTime.Now;
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
					double s2 = db[key].getSize();
                    size -= s2;
					globalSize -= s2;
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
					double s2 =d.getSize();
                    size += s2;
                    db.Add (key, d);
					globalSize += s2;
                    return true;
                }
            }
            return false;
        }

		/// <summary>
		/// Replace the specified key and d.
		/// </summary>
		/// <param name="key">Key.</param>
		/// <param name="d">D.</param>
		public bool ReplaceCas(string key, Item d, double CAS)
		{
			lock (db)
			{
				if (db.ContainsKey (key))
				{
					if (db[key].cas == CAS)
					{
						double s = db[key].getSize();
						double s2 = d.getSize();
						size = size - s;
						globalSize -= s;
						globalSize += s2;
						size += s2;
						db[key] = d;
						db[key].update = DateTime.Now;
						return true;
					}
				}
			}
			return false;
		}

        /// <summary>
        /// Replace the specified key and d.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <param name="d">D.</param>
        public bool Replace(string key, Item d)
        {
            lock (db)
            {
                if (db.ContainsKey (key))
                {
					double s = db[key].getSize();
					double s2 = d.getSize();
					size = size - s;
					globalSize -= s;
					globalSize += s2;
					size += s2;
                    db[key] = d;
                    db[key].update = DateTime.Now;
                    return true;
                }
            }
            return false;
        }

        public bool Touch(string key, int time)
        {
            lock (db)
            {
                if (db.ContainsKey (key))
                {
                    if (time == 0)
                    {
                        db[key].expiry = DateTime.MaxValue;
                        return true;
                    }
                    db[key].expiry = DateTime.Now.AddSeconds (time);
                    return true;
                }
            }
            return false;
        }
    }
}

