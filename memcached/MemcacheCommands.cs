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
            string[] part = null;
            part = parameters.Split(' ');
            if (part.Length < 4)
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
                SendError (ErrorCode.InvalidValues, ref w);
                return 1;
            }

            if (size < 0)
            {
                SendError (ErrorCode.InvalidValues, ref w);
                return 1;
            }

            if ((ulong)size > Configuration.InstanceMemoryLimitByteSize)
            {
                SendError (ErrorCode.OutOfMemory, ref w);
                return 3;
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
                if (FreeSize (MainClass.GlobalCaches[user]) < Item.getSize ())
                {
                    // we don't have enough free size let's try to free some
                    if (!MainClass.GlobalCaches[user].FreeSpace(Item.getSize ()))
                    {
                        // error
                        SendError (ErrorCode.OutOfMemory, ref w);
                        return 1;
                    }
                }
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
            string[] part = null;
            part = parameters.Split(' ');
            if (part.Length < 4)
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
                SendError (ErrorCode.InvalidValues, ref w);
                return 1;
            }

            if (size < 0)
            {
                SendError (ErrorCode.InvalidValues, ref w);
                return 1;
            }

            if ((ulong)size > Configuration.InstanceMemoryLimitByteSize)
            {
                // error
                SendError (ErrorCode.OutOfMemory, ref w);
                return 3;
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
                if (FreeSize (MainClass.GlobalCaches[user]) < Item.getSize ())
                {
                    // we don't have enough free size let's try to free some
                    if (!MainClass.GlobalCaches[user].FreeSpace(Item.getSize ()))
                    {
                        // error
                        SendError (ErrorCode.OutOfMemory, ref w);
                        return 1;
                    }
                }

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

        private static void decrement(string pars, ref System.IO.StreamWriter w, ref System.IO.StreamReader r, User user)
        {
            if (!pars.Contains (" "))
            {
                SendError (ErrorCode.InvalidValues, ref w);
                return;
            }
            
            string[] values = pars.Split (' ');
            
            if (values.Length < 2)
            {
                SendError (ErrorCode.MissingValues, ref w);
                return;
            }
            
            string key = values[0];
            int jump;
            
            if (!int.TryParse (values[1], out jump))
            {
                SendError(ErrorCode.InvalidValues, ref w);
                return;
            }
            
            Cache cache = MainClass.GlobalCaches[user];
            
            Cache.Item item = cache.Get (key, true);
            
            if (item == null)
            {
                cache.decr_misses++;
                Send ("NOT_FOUND", ref w);
                return;
            }
            
            int current;
            
            if (!int.TryParse (item.value, out current))
            {
                SendError (ErrorCode.InvalidValues, ref w);
                return;
            }
            
            current -= jump;
            cache.decr_hits++;
            cache.hardSet (key, new Cache.Item(current.ToString (), item.expiry, item.flags));
            Send (current.ToString (), ref w);
        }

        private static void Append(string pars, ref System.IO.StreamWriter w, ref System.IO.StreamReader r, User user)
        {
            if (!pars.Contains (" "))
            {
                SendError (ErrorCode.InvalidValues, ref w);
                return;
            }
            
            string[] part = pars.Split (' ');
            
            if (part.Length < 4)
            {
                SendError (ErrorCode.MissingValues, ref w);
                return;
            }
            
            string key = part[0];
            int flags = 0;
            int exptime = 0;
            int size = 0;
            //<command name> <key> <flags> <exptime> <bytes>

            if (!int.TryParse (part[1], out flags))
            {
                SendError (ErrorCode.InvalidValues, ref w);
                return;
            }
            
            if (!int.TryParse (part[2], out exptime))
            {
                SendError (ErrorCode.InvalidValues, ref w);
                return;
            }
            
            if (!int.TryParse (part[3], out size))
            {
                SendError (ErrorCode.InvalidValues, ref w);
                return;
            }

            if (size < 0)
            {
                SendError (ErrorCode.InvalidValues, ref w);
                return;
            }

            if ((ulong)size > Configuration.InstanceMemoryLimitByteSize)
            {
                // error
                SendError (ErrorCode.OutOfMemory, ref w);
                return;
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
                return;
            }
            
            Cache cache = MainClass.GlobalCaches[user];
            
            Cache.Item item = cache.Get (key, true);
            
            if (item == null)
            {
                Send ("NOT_FOUND", ref w);
                return;
            }

            Cache.Item replacement = new Cache.Item(item.value + chunk, item.expiry, item.flags);

            if (FreeSize (cache) < replacement.getSize())
            {
                // we don't have enough free size let's try to free some
                if (!cache.FreeSpace(replacement.getSize()))
                {
                    // error
                    SendError (ErrorCode.OutOfMemory, ref w);
                    return;
                }
            }
            
            cache.hardSet (key, replacement);
            if (!pars.EndsWith("noreply"))
            {
                Send ("STORED", ref w);
            }
        }

        private static void increment(string pars, ref System.IO.StreamWriter w, ref System.IO.StreamReader r, User user)
        {
            if (!pars.Contains (" "))
            {
                SendError (ErrorCode.InvalidValues, ref w);
                return;
            }

            string[] values = pars.Split (' ');

            if (values.Length < 2)
            {
                SendError (ErrorCode.MissingValues, ref w);
                return;
            }

            string key = values[0];
            int jump;

            if (!int.TryParse (values[1], out jump))
            {
                SendError(ErrorCode.InvalidValues, ref w);
                return;
            }

            Cache cache = MainClass.GlobalCaches[user];

            Cache.Item item = cache.Get (key, true);

            if (item == null)
            {
                Send ("NOT_FOUND", ref w);
                cache.incr_misses++;
                return;
            }

            int current;

            if (!int.TryParse (item.value, out current))
            {
                SendError (ErrorCode.InvalidValues, ref w);
                return;
            }

            current += jump;
            cache.incr_hits++;

            cache.hardSet (key, new Cache.Item(current.ToString (), item.expiry, item.flags));
            Send (current.ToString (), ref w);
        }

        private static void Gets(string pars, ref System.IO.StreamWriter w, ref System.IO.StreamReader r, User user)
        {
            string[] items = null;
            if (pars.Contains (" "))
            {
                items = pars.Split (' ');
            } else
            {
                items = new string[] {pars};
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
            string[] items = null;
            if (pars.Contains (" "))
            {
                items = pars.Split (' ');
            } else
            {
                items = new string[]{ pars };
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

        private static void Prepend(string pars, ref System.IO.StreamWriter w, ref System.IO.StreamReader r, User user)
        {
            if (!pars.Contains (" "))
            {
                SendError (ErrorCode.InvalidValues, ref w);
                return;
            }
            
            string[] part = pars.Split (' ');
            
            if (part.Length < 4)
            {
                SendError (ErrorCode.MissingValues, ref w);
                return;
            }
            
            string key = part[0];
            int flags = 0;
            int exptime = 0;
            int size = 0;
            //<command name> <key> <flags> <exptime> <bytes>
            
            if (!int.TryParse (part[1], out flags))
            {
                SendError (ErrorCode.InvalidValues, ref w);
                return;
            }
            
            if (!int.TryParse (part[2], out exptime))
            {
                SendError (ErrorCode.InvalidValues, ref w);
                return;
            }
            
            if (!int.TryParse (part[3], out size))
            {
                SendError (ErrorCode.InvalidValues, ref w);
                return;
            }

            if (size < 0)
            {
                SendError (ErrorCode.InvalidValues, ref w);
                return;
            }

            if ((ulong)size > Configuration.InstanceMemoryLimitByteSize)
            {
                // error
                SendError (ErrorCode.OutOfMemory, ref w);
                return;
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
                return;
            }
            
            Cache cache = MainClass.GlobalCaches[user];
            
            Cache.Item item = cache.Get (key, true);
            
            if (item == null)
            {
                Send ("NOT_FOUND", ref w);
                return;
            }

            Cache.Item replacement = new Cache.Item(item.value + chunk, item.expiry, item.flags);

            if (FreeSize (cache) < replacement.getSize())
            {
                // we don't have enough free size let's try to free some
                if (!cache.FreeSpace(replacement.getSize()))
                {
                    // error
                    SendError (ErrorCode.OutOfMemory, ref w);
                    return;
                }
            }

            cache.hardSet (key, replacement);
            
            if (!pars.EndsWith("noreply"))
            {
                Send ("STORED", ref w);
            }
        }

        private static int cas(string parameters, ref System.IO.StreamReader r, ref System.IO.StreamWriter w, User user)
        {
            string key = null;
            int flags = 0;
            int exptime = 0;
            int size = 0;
            double CAS = 0;

            //<command name> <key> <flags> <exptime> <bytes>
            string[] part = null;
            part = parameters.Split(' ');
            if (part.Length < 5)
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
                SendError (ErrorCode.InvalidValues, ref w);
                return 1;
            }

            if (size < 0)
            {
                SendError (ErrorCode.InvalidValues, ref w);
                return 1;
            }

            if ((ulong)size > Configuration.InstanceMemoryLimitByteSize)
            {
                // error
                SendError (ErrorCode.OutOfMemory, ref w);
                return 3;
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
                if (FreeSize (MainClass.GlobalCaches[user]) < Item.getSize ())
                {
                    SendError(ErrorCode.OutOfMemory, ref w);
                    return 1;
                }
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
                SendError (ErrorCode.InvalidValues, ref w);
                return 1;
            }

            if (size < 0)
            {
                SendError (ErrorCode.InvalidValues, ref w);
                return 1;
            }

            if ((ulong)size > Configuration.InstanceMemoryLimitByteSize)
            {
                // error
                SendError (ErrorCode.OutOfMemory, ref w);
                return 3;
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
                if (FreeSize (MainClass.GlobalCaches[user]) < Item.getSize ())
                {
                    // we don't have enough free size let's try to free some
                    if (!MainClass.GlobalCaches[user].FreeSpace(Item.getSize ()))
                    {
                        // error
                        SendError (ErrorCode.OutOfMemory, ref w);
                        return 1;
                    }
                }
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
                Cache cache;
                lock (MainClass.GlobalCaches)
                {
                    cache = MainClass.GlobalCaches[user];
                }
                Send ("STAT pid " + System.Diagnostics.Process.GetCurrentProcess ().Id.ToString (), ref w);
                Send ("STAT uptime " + MainClass.uptime ().ToString (), ref w);
                Send ("STAT time " + ToUnix().ToString(), ref w);
                Send ("STAT version sharp-memcached" + Configuration.Version, ref w);
                Send ("STAT pointer_size " + (IntPtr.Size * 8).ToString(), ref w);
                Send ("STAT global_memory_limit " + Configuration.GlobalMemoryLimitByteSize.ToString(), ref w);
                Send ("STAT user_memory_limit " + Configuration.InstanceMemoryLimitByteSize.ToString(), ref w);
                Send ("STAT hash_bytes_local " + cache.Size.ToString(), ref w);
                Send ("STAT user " + user.username, ref w);
                Send ("STAT hash_bytes " + Cache.GlobalSize.ToString(), ref w);
                Send ("STAT hashtables " + MainClass.GlobalCaches.Count.ToString (), ref w);
                Send ("STAT curr_items " + cache.Count().ToString(), ref w);
                Send ("STAT total_connections " + MainClass.Connections.ToString (), ref w);
                Send ("STAT curr_connections " + MainClass.OpenConnections.ToString (), ref w);
                Send ("STAT cmd_get " + cache.cmd_get.ToString(), ref w);
                Send ("STAT cmd_set " + cache.cmd_set.ToString (), ref w);
                Send ("STAT cmd_flush " + cache.cmd_flush.ToString(), ref w);
                Send ("STAT cmd_touch " + cache.cmd_touch.ToString(), ref w);
                Send ("STAT get_hits " + cache.get_hits.ToString(), ref w);
                Send ("STAT get_misses " + cache.get_misses.ToString(), ref w);
                Send ("STAT delete_misses " + cache.delete_misses.ToString(), ref w);
                Send ("STAT delete_hits " + cache.delete_hits.ToString(), ref w);
                Send ("STAT incr_misses " + cache.incr_misses.ToString(), ref w);
                Send ("STAT incr_hits " + cache.incr_hits.ToString(), ref w);
                Send ("STAT decr_misses " + cache.decr_misses.ToString(), ref w);
                Send ("STAT decr_hits " + cache.decr_hits.ToString(), ref w);
                Send ("STAT cas_hits " + cache.cas_hits.ToString(), ref w);
                Send ("STAT cas_misses " + cache.cas_misses.ToString(), ref w);

                return;
            }
        }

        private static double FreeSize(Cache cache)
        {
            double global = Configuration.GlobalMemoryLimitByteSize - Cache.GlobalSize;
            double local = Configuration.InstanceMemoryLimitByteSize - cache.Size;
            if (global < local)
            {
                return global;
            }
            return local;
        }
        
        private static void TouchData(string parameters, ref System.IO.StreamWriter Writer, User user)
        {
            string key = null;
            int exptime = 0;
            //<command name> <key> <flags> <exptime> <bytes>
            string[] part;
            part = parameters.Split(' ');
            if (part.Length < 2)
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

