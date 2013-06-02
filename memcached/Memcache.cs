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
        /// Listen this instance.
        /// </summary>
        public static void ListenUDP()
        {
            if (!Configuration.UDP)
            {
                return;
            }
            try
            {
                MainClass.DebugLog("UDP is not supported in this version");
            } catch (Exception fail)
            {
                MainClass.exceptionHandler(fail);
            }
        }

        private static void Send(string text, ref System.IO.StreamWriter w)
        {
            w.WriteLine(text);
            w.Flush();
        }

        private static void SendError(ErrorCode code, ref System.IO.StreamWriter writer)
        {
            if (!Configuration.DescriptiveErrors)
            {
                writer.WriteLine ("ERROR");
                writer.Flush();
                return;
            }
            switch (code)
            {
                case ErrorCode.AuthenticationFailed:
                    writer.WriteLine ("ERROR01");
                    writer.Flush();
                    return;
                case ErrorCode.AuthenticationRequired:
                    writer.WriteLine ("ERROR02");
                    writer.Flush();
                    return;
                case ErrorCode.InternalError:
                    writer.WriteLine ("ERROR00");
                    writer.Flush();
                    return;
                case ErrorCode.OutOfMemory:
                    writer.WriteLine ("ERROR04");
                    writer.Flush();
                    return;
                case ErrorCode.UnknownRequest:
                    writer.WriteLine ("ERROR03");
                    writer.Flush();
                    return;
                case ErrorCode.InvalidValues:
                    writer.WriteLine ("ERROR05");
                    writer.Flush();
                    return;
                case ErrorCode.MissingValues:
                    writer.WriteLine ("ERROR06");
                    writer.Flush();
                    return;
                case ErrorCode.ValueTooBig:
                    writer.WriteLine ("ERROR07");
                    writer.Flush();
                    return;
            }
            writer.WriteLine ("ERROR00");
            writer.Flush();
        }

        private static User Authenticate(string parameters)
        {
            if (parameters.Contains (":"))
            {
                string user = parameters.Substring (0, parameters.IndexOf (":"));
                string pswd = parameters.Substring(parameters.IndexOf(":") + 1);
                lock (MainClass.GlobalCaches)
                {
                    foreach (User xx in MainClass.GlobalCaches.Keys)
                    {
                        if (xx.username == user)
                        {
                            if (xx.password == pswd)
                            {
                                return xx;
                            } else
                            {
                                return null;
                            }
                        }
                    }
                }
            }
            return null;
        }

        private static void HandleClient(object data)
        {
            try
            {
                System.Net.Sockets.TcpClient connection = (System.Net.Sockets.TcpClient) data;
                MainClass.DebugLog("Incoming connection from: " + connection.Client.RemoteEndPoint.ToString());
                MainClass.Connections++;
                MainClass.OpenConnections++;
                System.Net.Sockets.NetworkStream ns = connection.GetStream();
                System.IO.StreamReader Reader = new System.IO.StreamReader(ns, System.Text.Encoding.UTF8);
                System.IO.StreamWriter Writer = new System.IO.StreamWriter(ns);
                string text;
                bool Authenticated = false;
                string username = null;
                User _U = MainClass.GlobalUser;
                // give the user access to global cache
                Cache cache = MainClass.GlobalCaches[MainClass.GlobalUser];
                // save the reference to global cache because we might need it in future
                Cache globalCache = cache;
                while (!Reader.EndOfStream)
                {
                    text = Reader.ReadLine();
                    string command = text;
                    string parameters = "";
                    if (text.Contains (" "))
                    {
                        command = command.Substring(0, command.IndexOf (" "));
                        command = command.ToLower();
                    }
                    if (text.Length > command.Length)
                    {
                        parameters = text.Substring (command.Length + 1);
                    }
                    // user isn't logged in and authentication is required
                    if (Configuration.Authentication && !Authenticated)
                    {
                        switch (command)
                        {
                        case "set":
                        case "get":
                        case "gget":
                        case "gset":
                        case "add":
                        case "replace":
                        case "append":
                        case "prepend":
                        case "cas":
                        case "gets":
                        case "delete":
                        case "incr":
                        case "decr":
                        case "flush_all":
                        case "touch":
                        case "slabs":
                            SendError (ErrorCode.AuthenticationRequired, ref Writer);
                            continue;
                        }
                    }
                    switch (command)
                    {
                    case "version":
                        Send ("VERSION sharp-memcached " + Configuration.Version, ref Writer);
                        continue;
                    case "authenticate":
                        User user = Authenticate (parameters);
                        if (user != null)
                        {
                            Send ("SUCCESS", ref Writer);
                            username = user.username;
                            cache = MainClass.GlobalCaches[user];
                            _U = user;
                            Authenticated = true;
                        } else
                        {
                            SendError (ErrorCode.AuthenticationFailed, ref Writer);
                        }
                        continue;
                    case "add":
                        int adrv = Add (parameters, ref Reader, ref Writer, _U);
                        if (adrv != 0)
                        {
                            if (Configuration.DescriptiveErrors)
                            {
                                switch (adrv)
                                {
                                    case 1:
                                    case 3:
                                        SendError (ErrorCode.InvalidValues, ref Writer);
                                        break;
                                    case 4:
                                        SendError (ErrorCode.ValueTooBig, ref Writer);
                                        break;
                                }
                                continue;
                            }
                            Send ("ERROR", ref Writer);
                        }
                        continue;
                    case "gset":
                        int getr = Set (parameters, ref Reader, ref Writer, MainClass.GlobalUser);
                        if (getr != 0)
                        {
                            if (Configuration.DescriptiveErrors)
                            {
                                switch (getr)
                                {
                                case 1:
                                case 3:
                                    SendError (ErrorCode.InvalidValues, ref Writer);
                                    break;
                                case 4:
                                    SendError (ErrorCode.ValueTooBig, ref Writer);
                                    break;
                                }
                                continue;
                            }
                            Send ("ERROR", ref Writer);
                        }
                        continue;
                    case "set":
                        int setr = Set (parameters, ref Reader, ref Writer, _U);
                        if (setr != 0)
                        {
                            if (Configuration.DescriptiveErrors)
                            {
                                switch (setr)
                                {
                                case 1:
                                case 3:
                                    SendError (ErrorCode.InvalidValues, ref Writer);
                                    break;
                                case 4:
                                    SendError (ErrorCode.ValueTooBig, ref Writer);
                                    break;
                                }
                                continue;
                            }
                            Send ("ERROR", ref Writer);
                        }
                        continue;
                    case "get":
                    case "gets":
                        Get(parameters, ref Writer, ref Reader, _U);
                        continue;
                    case "gget":
                        Get(parameters, ref Writer, ref Reader, MainClass.GlobalUser);
                        continue;
                    case "delete":
                        Delete(parameters, ref Writer, ref Reader, _U);
                        continue;
                    case "replace":
                        Replace(parameters, ref Reader, ref Writer, _U);
                        continue;
                    case "stat":
                        Stats(parameters, ref Writer, _U);
                        continue;
                    case "touch":
                        TouchData(parameters, ref Writer, _U);
                        continue;
                    case "incr":
                    case "append":
                    case "prepend":
                    case "cas":
                    case "slabs":
                        SendError(ErrorCode.NotImplemented, ref Writer);
                        continue;
                    case "quit":
                        connection.Close ();
                        MainClass.OpenConnections--;
                        return;
                    case "flush_all":
                        cache.Clear();
                        if (!parameters.EndsWith ("noreply"))
                        {
                            Send ("OK", ref Writer);
                        }
                        continue;
                    }
                    SendError (ErrorCode.UnknownRequest, ref Writer);
                }
            } catch (Exception fail)
            {
                MainClass.exceptionHandler(fail);
            }
            MainClass.OpenConnections--;
        }

        public static void ListenTCP()
        {
            if (!Configuration.TCP)
            {
                return;
            }
            try
            {
                MainClass.DebugLog("Listening on TCP port " + Configuration.Port);
                System.Net.Sockets.TcpListener server = new System.Net.Sockets.TcpListener(IPAddress.Any, Configuration.Port);
                server.Start();

                while(MainClass.isRunning)
                {
                    try
                    {
                        System.Net.Sockets.TcpClient connection = server.AcceptTcpClient();
                        Thread _client = new Thread(HandleClient);
                        _client.Start(connection);

                    } catch (Exception fail)
                    {
                        MainClass.exceptionHandler(fail);
                    }
                }
            } catch (Exception fail)
            {
                MainClass.exceptionHandler(fail);
            }
        }

        public enum ErrorCode
        {
            UnknownRequest,
            InternalError,
            AuthenticationRequired,
            AuthenticationFailed,
            OutOfMemory,
            InvalidValues,
            ValueTooBig,
            MissingValues,
            NotImplemented,
        }
    }
}

