using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading;
using Rainmeter;

namespace PluginCheckNet
{
    internal class Measure
    {
        public string Type;

        public int UpdateCounter;
        public int UpdateRate;

        private bool CaseOne;
        private bool CaseTwo;


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

        private void TypeCheck(string type, RulyCanceler c)
        {
            while (true)
            {
                c.ThrowIfCancellationRequested();

                #region Check

                if (type == "CASEONE")
                {
                    CaseOne = NetworkInterface.GetIsNetworkAvailable();
                }

                if (type == "CASETWO")
                {
                    // If Network is broken - we don't need to check internet.
                    if (NetworkInterface.GetIsNetworkAvailable())
                    {
                        try
                        {
                            IPAddress[] addresslist = Dns.GetHostAddresses("www.msftncsi.com");

                            CaseTwo = (addresslist[0].ToString().Length > 6);
                        }
                        catch
                        {
                            CaseTwo = false;
                        }
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
            Type = rm.ReadString("ConnectionType", "").ToUpperInvariant();
            _skinHandle = rm.GetSkin();
            _finishAction = rm.ReadString("FinishAction", "");

            // Switch is better because we can have a lot of options. 
            // All logic is in Update(), so we just need to check that this option in acceptable.
            switch (Type)
            {
                #region CaseOne
                case "CASEONE":
                    if (_networkThread == null)
                    {
                        _canceler = new RulyCanceler();
                        _networkThread = new Thread(() =>
                        {
                            try
                            {
                                TypeCheck(Type, _canceler);
                            }
                            catch (OperationCanceledException) { }
                        });
                        _networkThread.Start();
                    }
                    break;
                #endregion

                #region CaseTwo
                case "CASETWO":
                    if (_networkThread == null)
                    {
                        _canceler = new RulyCanceler();
                        _networkThread = new Thread(() =>
                        {
                            try
                            {
                                TypeCheck(Type, _canceler);
                            }
                            catch (OperationCanceledException) { }
                        });
                        _networkThread.Start();
                    }
                    break;
                #endregion

                default:
                    API.Log(API.LogType.Error, "CheckNet.dll: Type=" + Type + " not valid");
                    break;
            }

            UpdateRate = rm.ReadInt("UpdateRate", 20);
            if (UpdateRate <= 0)
            {
                UpdateRate = 20;
            }
        }

        internal double Update()
        {
            switch (Type)
            {
                #region CaseOne
                case "CASEONE":
                    if (UpdateCounter == 0)
                    {
                        if (_networkThread.ThreadState == ThreadState.Stopped)
                        {
                            _canceler = new RulyCanceler();
                            _networkThread = new Thread(() =>
                            {
                                try
                                {
                                    TypeCheck(Type, _canceler);
                                }
                                catch (OperationCanceledException) {}
                            });
                            _networkThread.Start();
                        }
                    }
                    return CaseOne ? 1.0 : -1.0;
                    break;
                #endregion

                #region CaseTwo
                case "CASETWO":
                    if (UpdateCounter == 0)
                    {
                        if (_networkThread.ThreadState == ThreadState.Stopped)
                        {
                            _canceler = new RulyCanceler();
                            _networkThread = new Thread(() =>
                            {
                                try
                                {
                                    TypeCheck(Type, _canceler);
                                }
                                catch (OperationCanceledException) {}
                            });
                            _networkThread.Start();
                        }
                    }
                    return CaseTwo ? 1.0 : -1.0;
                    break;
                #endregion
            }

            UpdateCounter = UpdateCounter + 1;
            if (UpdateCounter >= UpdateRate)
            {
                UpdateCounter = 0;
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
            if (_networkThread.IsAlive && _networkThread != null)
            {
                _canceler.Cancel();
            }
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
