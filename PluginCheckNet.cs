using System;
using System.Net;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Threading;
using Rainmeter;

namespace PluginCheckNet
{

    internal class Measure
    {
        public string ConnectionType;
        public double ReturnValue; // Zero is by default.
        public int UpdateRate;
        public int UpdateCounter; // Zero is by default.
        public static API RM;

        internal Measure()
        {
        }

        internal void Reload(API rm, ref double maxValue) // (Aragas) Removed Rainmeter.
        {
            RM = rm;

            ConnectionType = rm.ReadString("ConnectionType", "Internet");
            ConnectionType = ConnectionType.ToLowerInvariant();
            if (ConnectionType != "network" && ConnectionType != "internet")
            {
                API.Log(API.LogType.Error, "CheckNet.dll: ConnectionType=" + ConnectionType + " not valid");
            }
            
            UpdateRate = rm.ReadInt("UpdateRate", 20);
            if (UpdateRate <= 0)
            {
                UpdateRate = 20;
            }
            
        }

        internal double Update()
        {
            if (UpdateCounter == 0)
            {
                if (ConnectionType == "network" || ConnectionType == "internet")
                {
                    if (Convert.ToDouble(NetworkInterface.GetIsNetworkAvailable()) == 0) // (Aragas) Removed Rainmeter.
                    {
                        ReturnValue = -1.0;
                    }
                    else
                    {
                        ReturnValue = 1.0;
                    }
                }

                if (ReturnValue == 1.0 && ConnectionType == "internet")
                {
                    try
                    {
                        IPAddress[] addresslist = Dns.GetHostAddresses("www.msftncsi.com");

                        if (addresslist[0].ToString().Length > 6)
                        {
                            ReturnValue = 1.0;
                        }
                        else
                        {
                            ReturnValue = -1.0;
                        }
                    }
                    catch
                    {
                        ReturnValue = -1.0;
                    }
                }
            }

                UpdateCounter = UpdateCounter + 1;
                if (UpdateCounter >= UpdateRate)
                {
                    UpdateCounter = 0;
                }

                return ReturnValue;
        }

        //internal string GetString()
        //{
        //    return "";
        //}

        //internal void ExecuteBang(string args)
        //{
        //}
    }

    public static class SkinAlive
    {
        private static Thread _checkThread;

        public static void CheckSkinAlive()
        {
            if (_checkThread == null)
            {
                _checkThread = new Thread(CheckThread);
                _checkThread.Start();
            }

            if (!_checkThread.IsAlive)
            {
                _checkThread = new Thread(CheckThread);
                _checkThread.Start();
            }
        }

        private static void CheckThread() // Must be in thread.
        {
            // (Aragas) Try catch is used because if we make ReadString to a closed RM it will make an APPCRASH.
            try
            {
                while (Measure.RM.ReadString("ConnectionType", "") != "")
                {
                    Thread.Sleep(2000);
                }
            }
            catch
            {
                Dispose();
            }
        }

        public static void Dispose()
        {
            if (_checkThread.IsAlive)
                _checkThread.Abort();
        }
    }

    public static class Plugin
    {
        [DllExport]
        public unsafe static void Initialize(void** data, void* rm)
        {
            uint id = (uint)((void*)*data);
            Measures.Add(id, new Measure());
        }

        [DllExport]
        public unsafe static void Finalize(void* data)
        {
            uint id = (uint)data;
            Measures.Remove(id);
        }

        [DllExport]
        public unsafe static void Reload(void* data, void* rm, double* maxValue)
        {
            uint id = (uint)data;
            Measures[id].Reload(new Rainmeter.API((IntPtr)rm), ref *maxValue);
        }

        [DllExport]
        public unsafe static double Update(void* data)
        {
            uint id = (uint)data;
            return Measures[id].Update();
        }

        //[DllExport]
        //public unsafe static char* GetString(void* data)
        //{
        //    uint id = (uint)data;
        //    fixed (char* s = Measures[id].GetString()) return s;
        //}

        //[DllExport]
        //public unsafe static void ExecuteBang(void* data, char* args)
        //{
        //    uint id = (uint)data;
        //    Measures[id].ExecuteBang(new string(args));
        //}

        internal static Dictionary<uint, Measure> Measures = new Dictionary<uint, Measure>();
    }
}
