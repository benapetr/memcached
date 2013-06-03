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
            /// <summary>
            /// The value.
            /// </summary>
            public string value = null;
            /// <summary>
            /// The expiry.
            /// </summary>
            public DateTime expiry;
            /// <summary>
            /// The flags.
            /// </summary>
            public int flags = 0;
            /// <summary>
            /// The update.
            /// </summary>
            public DateTime update;
            /// <summary>
            /// The cas.
            /// </summary>
            public double cas;
            private ulong size = 0;

            /// <summary>
            /// Initializes a new instance of the <see cref="memcached.Cache+Item"/> class.
            /// </summary>
            /// <param name="data">Data.</param>
            /// <param name="Expiry">Expiry.</param>
            /// <param name="Flags">Flags.</param>
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
            public ulong getSize()
            {
                if (size != 0)
                {
                    return size;
                }
                unsafe
                {
                    ulong xx = (ulong)((sizeof(DateTime) * 2) + 
					                   sizeof(int) +
					                   sizeof (ulong) +
					                   (2 * IntPtr.Size) +
					                   sizeof(double));
                
                    if (value == null)
                    {
                        size = xx;
                        return size;
                    }
                    size = xx + (ulong)(sizeof(char) * value.Length);
                    return size;
                }
            }
        }

        private Dictionary<string, Item> db = new Dictionary<string, Item>();

        private static ulong globalSize = 0;

        /// <summary>
        /// Gets the size of the global.
        /// </summary>
        /// <value>The size of the global.</value>
        public static ulong GlobalSize
        {
            get
            {
                return globalSize;
            }
        }

        private ulong size = 0;

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

        /// <summary>
        /// Gets the size of the global.
        /// </summary>
        /// <returns>The global size.</returns>
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
            size = IntPtr.Size;
            globalSize += IntPtr.Size;
        }

        /// <summary>
        /// Releases unmanaged resources and performs other cleanup operations before the <see cref="memcached.Cache"/> is
        /// reclaimed by garbage collection.
        /// </summary>
        ~Cache()
        {
            globalSize -= IntPtr.Size;
        }

        /// <summary>
        /// Clear this instance.
        /// </summary>
        public void Clear()
        {
            lock(db)
            {
                globalSize -= size - IntPtr.Size;
                db.Clear();
                size = IntPtr.Size;
            }
        }

        /// <summary>
        /// Cleans the old.
        /// </summary>
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

        /// <summary>
        /// Count this instance.
        /// </summary>
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
					ulong s6 = data.getSize();
                    size += s6;
					globalSize += s6;
                    return;
                }
                ulong s = db[id].getSize();
				ulong s2 = data.getSize();
				globalSize += s2;
				size += s2;
                size -= s;
                globalSize -= s;
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
                    ulong s2 = db[key].getSize();
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
                    ulong s2 =d.getSize();
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
                        ulong s = db[key].getSize();
                        ulong s2 = d.getSize();
						globalSize += s2;
						size += s2;
                        size = size - s;
                        globalSize -= s;
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
                    ulong s = db[key].getSize();
                    ulong s2 = d.getSize();
					globalSize += s2;
					size += s2;
                    size = size - s;
                    globalSize -= s;
                    db[key] = d;
                    db[key].update = DateTime.Now;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Touch the specified key and time.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <param name="time">Time.</param>
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

