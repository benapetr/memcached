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
using System.Threading;
using System.Net;
using System.Collections.Generic;

namespace memcached
{
    public partial class Memcache
    {
        /// <summary>
        /// Set the specified parameters, r and w.
        /// </summary>
        /// <param name="parameters">Parameters.</param>
        /// <param name="r">The red component.</param>
        /// <param name="w">The width.</param>
        private static int Set(string parameters, ref System.IO.StreamReader r, ref System.IO.StreamWriter w, User user)
        {
            string key = null;
            int flags = 0;
            int exptime = 0;
            int size = 0;
            //<command name> <key> <flags> <exptime> <bytes>
            List<string> part = new List<string>();
            part.AddRange(parameters.Split(' '));
            if (part.Count < 4)
            {
                // invalid format
				SendError (ErrorCode.MissingValues, ref w);
                return 1;
            }

            key = part[0];
            if (!int.TryParse (part[1], out flags))
            {
				SendError (ErrorCode.InvalidValues, ref w);
                return 1;
            }

            if (!int.TryParse (part[2], out exptime))
            {
				SendError (ErrorCode.InvalidValues, ref w);
                return 1;
            }

            if (!int.TryParse (part[3], out size))
            {
                if (size < 0)
                {
                    // error
					SendError (ErrorCode.InvalidValues, ref w);
                    return 3;
                }
				SendError (ErrorCode.InvalidValues, ref w);
                return 1;
            }

            // everything is ok let's go
            string chunk = r.ReadLine();
            while (chunk.Length < size)
            {
                chunk += "\n" + r.ReadLine();
            }

            if (chunk.Length > size)
            {
                // too big
				SendError (ErrorCode.InvalidValues, ref w);
                return 4;
            }

            Cache.Item Item = new Cache.Item(chunk, exptime, flags);

            lock (MainClass.GlobalCaches)
            {
                MainClass.GlobalCaches[user].Set (key, Item);
            }

            if (!parameters.EndsWith ("noreply"))
            {
                Send ("STORED", ref w);
            }

            // unknown error
            return 0;
        }

        private static void Delete(string pars, ref System.IO.StreamWriter w, ref System.IO.StreamReader r, User user)
        {
            string key = pars;
            if (key.Contains (" "))
            {
                key = key.Substring(0, key.IndexOf(" "));
            }

            Cache cache = null;
            
            lock(MainClass.GlobalCaches)
            {
                if (!MainClass.GlobalCaches.ContainsKey(user))
                {
                    SendError (ErrorCode.InternalError, ref w);
                    return;
                }
                cache = MainClass.GlobalCaches[user];
            }

            if (cache.Delete(key))
            {
                if (!pars.EndsWith ("noreply"))
                {
                    Send ("DELETED", ref w);
                }
                return;
            }
            if (!pars.EndsWith ("noreply"))
            {
                Send ("NOT_FOUND", ref w);
            }
        }

        private static int Add(string parameters, ref System.IO.StreamReader r, ref System.IO.StreamWriter w, User user)
        {
            string key = null;
            int flags = 0;
            int exptime = 0;
            int size = 0;
            //<command name> <key> <flags> <exptime> <bytes>
            List<string> part = new List<string>();
            part.AddRange(parameters.Split(' '));
            if (part.Count < 4)
            {
                // invalid format
				SendError (ErrorCode.InvalidValues, ref w);
                return 1;
            }
            
            key = part[0];
            if (!int.TryParse (part[1], out flags))
            {
				SendError (ErrorCode.InvalidValues, ref w);
                return 1;
            }
            
            if (!int.TryParse (part[2], out exptime))
            {
				SendError (ErrorCode.InvalidValues, ref w);
                return 1;
            }
            
            if (!int.TryParse (part[3], out size))
            {
                if (size < 0)
                {
                    // error
					SendError (ErrorCode.InvalidValues, ref w);
                    return 3;
                }
				SendError (ErrorCode.InvalidValues, ref w);
                return 1;
            }
            
            // everything is ok let's go
            string chunk = r.ReadLine();
            while (chunk.Length < size)
            {
                chunk += "\n" + r.ReadLine();
            }
            
            if (chunk.Length > size)
            {
                // too big
				SendError (ErrorCode.InvalidValues, ref w);
                return 4;
            }

            Cache.Item Item = new Cache.Item(chunk, exptime, flags);

            lock (MainClass.GlobalCaches)
            {
                if (MainClass.GlobalCaches[user].Add (key, Item))
                {
                    if (!parameters.EndsWith ("noreply"))
                    {
                        Send ("STORED", ref w);
                    }
                } else
                {
                    if (!parameters.EndsWith ("noreply"))
                    {
                        Send ("NOT_STORED", ref w);
                    }
                }
            }

            return 0;
        }

        private static void Gets(string pars, ref System.IO.StreamWriter w, ref System.IO.StreamReader r, User user)
        {
            List<string> items = new List<string>();
            if (pars.Contains (" "))
            {
                items.AddRange(pars.Split (' '));
            } else
            {
                items.Add(pars);
            }

            Cache cache = null;

            lock(MainClass.GlobalCaches)
            {
                if (!MainClass.GlobalCaches.ContainsKey(user))
                {
                    SendError (ErrorCode.InternalError, ref w);
                    return;
                }
                cache = MainClass.GlobalCaches[user];
            }
            string data = "";
            foreach (string curr in items)
            {
                Cache.Item item = cache.Get (curr);
                if (item != null)
                {
                    data += "VALUE " + curr + " " + item.flags.ToString() + " " + item.value.Length.ToString() + " " + item.cas.ToString() + "\r\n" + item.value + "\r\n";
                }
            }
            Send(data + "END", ref w);
        }

        private static void Get(string pars, ref System.IO.StreamWriter w, ref System.IO.StreamReader r, User user)
        {
            List<string> items = new List<string>();
            if (pars.Contains (" "))
            {
                items.AddRange(pars.Split (' '));
            } else
            {
                items.Add(pars);
            }
            
            Cache cache = null;
            
            lock(MainClass.GlobalCaches)
            {
                if (!MainClass.GlobalCaches.ContainsKey(user))
                {
                    SendError (ErrorCode.InternalError, ref w);
                    return;
                }
                cache = MainClass.GlobalCaches[user];
            }
            foreach (string curr in items)
            {
                Cache.Item item = cache.Get (curr);
                if (item != null)
                {
                    Send("VALUE " + curr + " " + item.flags.ToString() + " " + item.value.Length.ToString() + "\r\n" + item.value, ref w);
                }
            }
            Send("END", ref w);
        }

        private static int cas(string parameters, ref System.IO.StreamReader r, ref System.IO.StreamWriter w, User user)
        {
            string key = null;
            int flags = 0;
            int exptime = 0;
            int size = 0;
			double CAS = 0;

            //<command name> <key> <flags> <exptime> <bytes>
            List<string> part = new List<string>();
            part.AddRange(parameters.Split(' '));
            if (part.Count < 5)
            {
                // invalid format
				SendError (ErrorCode.InvalidValues, ref w);
                return 1;
            }
            
            key = part[0];
            if (!int.TryParse (part[1], out flags))
            {
				SendError (ErrorCode.InvalidValues, ref w);
                return 1;
            }
            
            if (!int.TryParse (part[2], out exptime))
            {
				SendError (ErrorCode.InvalidValues, ref w);
                return 1;
            }
            
            if (!int.TryParse (part[3], out size))
            {
                if (size < 0)
                {
                    // error
					SendError (ErrorCode.InvalidValues, ref w);
                    return 3;
                }
				SendError (ErrorCode.InvalidValues, ref w);
                return 1;
            }

			if (!double.TryParse (part[4], out CAS))
			{
				SendError (ErrorCode.InvalidValues, ref w);
				return 1;
			}
            
            // everything is ok let's go
            string chunk = r.ReadLine();
            while (chunk.Length < size)
            {
                chunk += "\n" + r.ReadLine();
            }
            
            if (chunk.Length > size)
            {
                // too big
				SendError (ErrorCode.InvalidValues, ref w);
                return 4;
            }
            
            Cache.Item Item = new Cache.Item(chunk, exptime, flags);
            
            lock (MainClass.GlobalCaches)
            {
                if (MainClass.GlobalCaches[user].ReplaceCas (key, Item, CAS))
                {
                    if (!parameters.EndsWith ("noreply"))
                    {
                        Send ("STORED", ref w);
                    }
                } else
                {
                    if (!parameters.EndsWith ("noreply"))
                    {
                        Send ("NOT_STORED", ref w);
                    }
                }
            }
            
            return 0;
        }

		private static int Replace(string parameters, ref System.IO.StreamReader r, ref System.IO.StreamWriter w, User user)
		{
			string key = null;
			int flags = 0;
			int exptime = 0;
			int size = 0;
			//<command name> <key> <flags> <exptime> <bytes>
			List<string> part = new List<string>();
			part.AddRange(parameters.Split(' '));
			if (part.Count < 4)
			{
				// invalid format
				return 1;
			}
			
			key = part[0];
			if (!int.TryParse (part[1], out flags))
			{
				SendError (ErrorCode.InvalidValues, ref w);
				return 1;
			}
			
			if (!int.TryParse (part[2], out exptime))
			{
				SendError (ErrorCode.InvalidValues, ref w);
				return 1;
			}
			
			if (!int.TryParse (part[3], out size))
			{
				if (size < 0)
				{
					// error
					SendError (ErrorCode.InvalidValues, ref w);
					return 3;
				}
				SendError (ErrorCode.InvalidValues, ref w);
				return 1;
			}
			
			// everything is ok let's go
			string chunk = r.ReadLine();
			while (chunk.Length < size)
			{
				chunk += "\n" + r.ReadLine();
			}
			
			if (chunk.Length > size)
			{
				// too big
				SendError (ErrorCode.ValueTooBig, ref w);
				return 4;
			}
			
			Cache.Item Item = new Cache.Item(chunk, exptime, flags);
			
			lock (MainClass.GlobalCaches)
			{
				if (MainClass.GlobalCaches[user].Replace (key, Item))
				{
					if (!parameters.EndsWith ("noreply"))
					{
						Send ("STORED", ref w);
					}
				} else
				{
					if (!parameters.EndsWith ("noreply"))
					{
						Send ("NOT_STORED", ref w);
					}
				}
			}
			
			return 0;
		}

        private static double ToUnix()
        {
            return (DateTime.Now - new DateTime(1970, 1, 1).ToLocalTime()).TotalSeconds;
        }

        private static void Stats(string parameters, ref System.IO.StreamWriter w, User user)
        {
            if (parameters == "")
            {
                Send ("STAT pid " + System.Diagnostics.Process.GetCurrentProcess ().Id.ToString (), ref w);
                Send ("STAT uptime " + MainClass.uptime ().ToString (), ref w);
                Send ("STAT time " + ToUnix().ToString(), ref w);
                Send ("STAT version sharp memcached " + Configuration.Version, ref w);
                Send ("STAT pointer_size " + IntPtr.Size.ToString(), ref w);
                Send ("STAT global_memory_limit " + Configuration.GlobalMemoryLimitByteSize.ToString(), ref w);
                Send ("STAT user_memory_limit " + Configuration.InstanceMemoryLimitByteSize.ToString(), ref w);
                Send ("STAT hash_bytes_local " + MainClass.GlobalCaches[user].Size.ToString(), ref w);
                Send ("STAT user " + user.username, ref w);
                Send ("STAT hash_bytes " + Cache.GlobalSize ().ToString(), ref w);
                Send ("STAT hashtables " + MainClass.GlobalCaches.Count.ToString (), ref w);
				Send ("STAT count " + MainClass.GlobalCaches[user].Count().ToString(), ref w);
                Send ("STAT connections " + MainClass.Connections.ToString (), ref w);
                Send ("STAT open_connections " + MainClass.OpenConnections.ToString (), ref w);
                return;
            }
        }

        private static void TouchData(string parameters, ref System.IO.StreamWriter Writer, User user)
        {
            string key = null;
            int exptime = 0;
            //<command name> <key> <flags> <exptime> <bytes>
            List<string> part = new List<string>();
            part.AddRange(parameters.Split(' '));
            if (part.Count < 2)
            {
                // invalid format
                SendError(ErrorCode.MissingValues, ref Writer);
                return;
            }
            
            key = part[0];

            if (!int.TryParse (part[1], out exptime))
            {
                SendError(ErrorCode.MissingValues, ref Writer);
                return;
            }

            lock(MainClass.GlobalCaches)
            {
                if (!MainClass.GlobalCaches.ContainsKey(user))
                {
                    // this should never happen
                    MainClass.DebugLog("There is no cache for user " + user.username);
                    SendError(ErrorCode.InternalError, ref Writer);
                    return;
                }
                if (MainClass.GlobalCaches[user].Touch (key, exptime))
                {
                    if (!parameters.EndsWith ("noreply"))
                    {
                        Send ("TOUCHED", ref Writer);
                    }
                }
                else
                {
                    if (!parameters.EndsWith ("noreply"))
                    {
                        Send("NOT_FOUND", ref Writer);
                    }
                }
            }
        }
    }
}

