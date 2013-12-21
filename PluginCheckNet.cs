using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading;
using Rainmeter;

namespace PluginCheckNet
{
    internal class Measure
    {
        public string ConnectionType;
        public int UpdateCounter;
        public int UpdateRate;

        public bool NetworkAvailable;
        public bool InternetAvailable;


        private IntPtr _skinHandle;
        private string _finishAction;
        private static Thread _networkThread;
        private static RulyCanceler _canceler;

        public void FinishAction()
        {
            if (!String.IsNullOrEmpty(_finishAction))
            {
                API.Execute(_skinHandle, _finishAction);
            }
        }

        private void CheckConnection(string type, RulyCanceler c)
        {
            while (true)
            {
                c.ThrowIfCancellationRequested();

                #region code

                if (type == "NETWORK")
                {
                    NetworkAvailable = NetworkInterface.GetIsNetworkAvailable();
                }

                if (type == "INTERNET" && NetworkInterface.GetIsNetworkAvailable())
                {
                    try
                    {
                        IPAddress[] addresslist = Dns.GetHostAddresses("www.msftncsi.com");

                        InternetAvailable = (addresslist[0].ToString().Length > 6);
                    }
                    catch
                    {
                        InternetAvailable = false;
                    }
                }

                #endregion

                FinishAction();

                API.Log(API.LogType.Error, "ThreadIsClosed");
                c.Cancel();
            }

        }

        internal Measure()
        {
        }

        internal void Reload(Rainmeter.API rm, ref double maxValue)
        {
            ConnectionType = rm.ReadString("ConnectionType", "INTERNET").ToUpperInvariant();
            _skinHandle = rm.GetSkin();
            _finishAction = rm.ReadString("FinishAction", "");

            if (ConnectionType != "NETWORK" && ConnectionType != "INTERNET")
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
            UpdateCounter = UpdateCounter + 1;
            if (UpdateCounter >= UpdateRate)
            {
                UpdateCounter = 0;
            }

            switch (ConnectionType)
            {
                #region Network
                case "NETWORK":
                    if (UpdateCounter == 0)
                    {
                        //We check here to see if all existing instances of the thread have stopped,
                        //and start a new one if so.
                        if (_networkThread == null || _networkThread.ThreadState == ThreadState.Stopped)
                        {
                            _canceler = new RulyCanceler();
                            _networkThread = new Thread(() =>
                            {
                                try
                                {
                                    CheckConnection(ConnectionType, _canceler);
                                }
                                catch (OperationCanceledException)
                                {
                                }
                            });
                            _networkThread.Start();
                        }
                    }
                    return NetworkAvailable ? 1.0 : -1.0;
                    break;
                #endregion

                #region Internet
                case "INTERNET":
                    if (UpdateCounter == 0)
                    {
                        //We check here to see if all existing instances of the thread have stopped,
                        //and start a new one if so.
                        if (_networkThread == null || _networkThread.ThreadState == ThreadState.Stopped)
                        {
                            _canceler = new RulyCanceler();
                            _networkThread = new Thread(() =>
                            {
                                try
                                {
                                    CheckConnection(ConnectionType, _canceler);
                                }
                                catch (OperationCanceledException)
                                {
                                }
                            });
                            _networkThread.Start();
                        }
                    }
                    return InternetAvailable ? 1.0 : -1.0;
                    break;
                #endregion
            }

            return 0.0;
        }

        //internal string GetString()
        //{
        //    return "";
        //}

        //internal void ExecuteBang(string args)
        //{
        //}

        //it is recommended that this Dispose() function be in all examples,
        //and called in Finalize().

        internal static void Dispose()
        {
            if (_networkThread.IsAlive)
                _canceler.Cancel();
        }
    }

    class RulyCanceler
    {
        object _cancelLocker = new object();
        bool _cancelRequest;
        bool IsCancellationRequested
        {
            get { lock (_cancelLocker) return _cancelRequest; }
        }

        public void Cancel() { lock (_cancelLocker) _cancelRequest = true; }

        public void ThrowIfCancellationRequested()
        {
            if (IsCancellationRequested) throw new OperationCanceledException();
        }
    }

    static class Plugin
    {
        static Dictionary<uint, Measure> Measures = new Dictionary<uint, Measure>();

        [DllExport]
        public unsafe static void Finalize(void* data)
        {
            Measure.Dispose();
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
