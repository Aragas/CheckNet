﻿using Rainmeter;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading;

namespace PluginCheckNet
{
    internal class Measure
    {
        public string ConnectionType;
        public double ReturnValue;
        public int UpdateCounter;
        public int UpdateRate;

        static Thread _networkThread;

        void Internet()
        {
            if (!NetworkInterface.GetIsNetworkAvailable())
            {
                ReturnValue = -1.0;
                return;
            }

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

            Thread.CurrentThread.Abort(); // (Aragas) Don't use that void in the main thread!!
        }

        void Network()
        {
            if (Convert.ToDouble(NetworkInterface.GetIsNetworkAvailable()) == 0)
            {
                ReturnValue = -1.0;
            }
            else
            {
                ReturnValue = 1.0;
            }

            Thread.CurrentThread.Abort(); // (Aragas) Don't use that void in the main thread!!
        }

        internal Measure()
        {
        }

        internal void Reload(API rm, ref double maxValue) // (Aragas) Removed Rainmeter.
        {
            ConnectionType = rm.ReadString("ConnectionType", "Internet");
            UpdateRate = rm.ReadInt("UpdateRate", 20);

            if (UpdateRate <= 0)
            {
                UpdateRate = 20;
            }

            switch (ConnectionType.ToUpperInvariant())
            {
                case "INTERNET":
                    if (_networkThread == null)
                    {
                        _networkThread = new Thread(Internet);
                        _networkThread.Start();
                    }
                    break;

                case "NETWORK":
                    if (_networkThread == null)
                    {
                        _networkThread = new Thread(Network);
                        _networkThread.Start();
                    }
                    break;

                default:
                    API.Log(API.LogType.Error, "CheckNet.dll: ConnectionType=" + ConnectionType + " not valid");
                    break;
            }

        }

        // (Aragas) Just reading all variables from .dll and showing in Rainmeter.
        internal double Update()
        {
            switch (ConnectionType.ToUpperInvariant())
            {
                case "NETWORK":
                    if (UpdateCounter == 0)
                    {
                        if (_networkThread.ThreadState == ThreadState.Stopped)
                            // (Aragas) We check here if it is the time to update information.
                        {
                            _networkThread = new Thread(Network);
                            _networkThread.Start();
                        }
                    }
                    break;

                case "INTERNET":
                    if (UpdateCounter == 0)
                    {
                        if (_networkThread.ThreadState == ThreadState.Stopped)
                            // (Aragas) We check here if it is the time to update information.
                        {
                            _networkThread = new Thread(Internet);
                            _networkThread.Start();
                        }
                    }
                    break;
            }

            // (Aragas) Counter must be placed in Update()
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

        // (Aragas) Recommend to put this in all samples. If is unused, juts make there return;

        internal static void Dispose()
        {
            if (_networkThread.IsAlive)
                _networkThread.Abort();
        }
    }

    static class Plugin
    {
        static Dictionary<uint, Measure> Measures = new Dictionary<uint, Measure>();

        [DllExport]
        public unsafe static void Finalize(void* data)
        {
            Measure.Dispose(); // (Aragas) Recommend to put this in all samples.
            uint id = (uint)data;
            Measures.Remove(id);
        }

        [DllExport]
        public unsafe static void Initialize(void** data, void* rm)
        {
            uint id = (uint)((void*)*data);
            Measures.Add(id, new Measure());
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
    }

}