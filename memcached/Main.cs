using System;
using System.Threading;
using System.Collections.Generic;
using System.IO;

namespace memcached
{
    class MainClass
    {
        public static bool isRunning = true;
        public static Dictionary<User, Cache> GlobalCaches = new Dictionary<User, Cache>();
        public static User GlobalUser = null;
        private static DateTime st;
        public static double BytesSent = 0;
        public static double BytesReceived = 0;
        public static int Connections = 0;
        public static int OpenConnections = 0;

        /// <summary>
        /// Log the specified text.
        /// </summary>
        /// <param name="text">Text.</param>
        public static void Log(string text)
        {
            Console.WriteLine(DateTime.Now.ToString() + ": " + text);
        }

        /// <summary>
        /// Debug log.
        /// </summary>
        /// <param name="text">Text.</param>
        /// <param name="verbosity">Verbosity.</param>
        public static void DebugLog(string text, int verbosity = 1)
        {
            if (Configuration.Verbosity >= verbosity)
            {
                Log ("DEBUG: " + text);
            }
        }

        public static void exceptionHandler(Exception exception)
        {
            Log ("EXCEPTION: " + exception.Message + "\n\n" + exception.StackTrace + "\n\n" + exception.Source);
        }

        public static void LoadUsers()
        {
            if (!File.Exists(Configuration.UserDB))
            {
                DebugLog("There is no user db to load users from");
                return;
            }
            List<string> lines = new List<string>();
            lines.AddRange(File.ReadAllLines(Configuration.UserDB));
            lock (GlobalCaches)
            {
                foreach (string i in lines)
                {
                    if (i.Contains (":"))
                    {
                        string name = i.Substring(0, i.IndexOf (":"));
                        string pw = i.Substring(i.IndexOf (":") + 1);
                        User user = new User(name);
                        user.password = pw;
                        GlobalCaches.Add (user, new Cache());
                        DebugLog("User: " + name);
                    }
                    else
                    {
                        DebugLog("Invalid record: " + i);
                    }
                }
            }
        }

        public static void GC()
        {
            while(isRunning)
            {
                lock (GlobalCaches)
                {
                    foreach (Cache db in GlobalCaches.Values)
                    {
                        db.CleanOld ();
                    }
                }
                Thread.Sleep(2000);
            }
        }

        public static long uptime()
        {
            return (DateTime.Now - st).Seconds;
        }

        public static void Main (string[] args)
        {
            if (!Terminal.Parse (args))
            {
                Log ("Starting sharp memcached server version " + Configuration.Version);
                st = DateTime.Now;
                if (Configuration.Verbosity > 0)
                {
                    DebugLog ("Verbosity: " + Configuration.Verbosity.ToString());
                }
                if (!Configuration.UDP && !Configuration.TCP)
                {
                    Log ("ERROR: you must enable either tcp or udp");
                    return;
                }
                Thread cleaner = new Thread(GC);
                cleaner.Start();
                // we have 1 shared cache for everyone
                GlobalUser = new User(":global");
                GlobalCaches.Add (GlobalUser, new Cache());
                LoadUsers();
                // create a new thread for tcp and start it
                Thread tcp = new Thread(Memcache.ListenTCP);
                tcp.Name = "tcp listener";
                tcp.Start();
                Thread udp = new Thread(Memcache.ListenUDP);
                udp.Name = "udp listener";
                udp.Start();
                while (isRunning)
                {
                    Thread.Sleep(100);
                }
            }
        }
    }
}
