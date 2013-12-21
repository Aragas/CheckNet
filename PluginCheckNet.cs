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
        enum MeasureType
        {
            CaseOne,
            CaseTwo,
            CaseThree
        }
        MeasureType Type;

        int UpdateCounter;
        int UpdateRate;

        bool CaseOne;
        int CaseTwo;
        string CaseThree;
        string ReturnValueString;
        double ReturnValueDouble;


        IntPtr _skinHandle;
        string _finishAction;

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

                if (type == MeasureType.CaseOne.ToString())
                {
                    CaseOne = NetworkInterface.GetIsNetworkAvailable();
                }

                if (type == MeasureType.CaseTwo.ToString())
                {
                    // If Network is broken - we don't need to check internet.
                    if (NetworkInterface.GetIsNetworkAvailable())
                    {
                        try
                        {
                            CaseTwo = NetworkInterface.LoopbackInterfaceIndex;
                        }
                        catch
                        {
                            CaseTwo = 0;
                        }
                    }
                }

                if (type == MeasureType.CaseThree.ToString())
                {
                    CaseThree = Dns.GetHostAddresses("www.msftncsi.com")[0].ToString();
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
            
            _skinHandle = rm.GetSkin();
            _finishAction = rm.ReadString("FinishAction", "");

            // Switch is better because we can have a lot of options. 
            // All logic is in Update(), so we just need to check that this option in acceptable.
            string type = rm.ReadString("Type", "");
            switch (type.ToUpperInvariant())
            {
                case "CASEONE":
                    Type = MeasureType.CaseOne;
                    break;
                    
                case "CASETWO":
                    Type = MeasureType.CaseTwo;
                    break;

                case "CASETHREE":
                    Type = MeasureType.CaseThree;
                    break;

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
                case MeasureType.CaseOne:
                    if (UpdateCounter == 0)
                    {
                        RulyCanceler _canceler = new RulyCanceler();
                        new Thread(() =>
                        {
                            try
                            {
                                TypeCheck(MeasureType.CaseOne.ToString(), _canceler);
                            }
                            catch (OperationCanceledException) {}
                        }).Start();
                    }

                    if (CaseOne)
                    {
                        ReturnValueDouble = 1.0;
                    }
                    else
                    {
                        ReturnValueDouble = -1.0;
                    }
                    break;

                    #endregion

                #region CaseTwo
                case MeasureType.CaseTwo:
                    if (UpdateCounter == 0)
                    {
                        RulyCanceler _canceler = new RulyCanceler();
                        new Thread(() =>
                        {
                            try
                            {
                                TypeCheck(MeasureType.CaseTwo.ToString(), _canceler);
                            }
                            catch (OperationCanceledException) {}
                        }).Start();
                    }

                    ReturnValueDouble = CaseTwo;
                    break;

                #endregion
            }

            UpdateCounter = UpdateCounter + 1;
            if (UpdateCounter >= UpdateRate)
            {
                UpdateCounter = 0;
            }

            return ReturnValueDouble;
        }

        internal string GetString()
        {
            switch (Type)
            {
                #region CaseThree
                case MeasureType.CaseThree:
                    if (UpdateCounter == 0)
                    {
                        RulyCanceler _canceler = new RulyCanceler();
                        new Thread(() =>
                        {
                            try
                            {
                                TypeCheck(MeasureType.CaseThree.ToString(), _canceler);
                            }
                            catch (OperationCanceledException) {}
                        }).Start();
                    }

                    ReturnValueString = CaseThree;
                    break;

                #endregion
            }

            return ReturnValueString;
        }

        internal void ExecuteBang(string args)
        {
            return;
        }

        internal static void Finalize()
        {
            return;
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
            Measure.Finalize();
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

        [DllExport]
        public unsafe static char* GetString(void* data)
        {
            uint id = (uint)data;
            fixed (char* s = Measures[id].GetString()) return s;
        }

        [DllExport]
        public unsafe static void ExecuteBang(void* data, char* args)
        {
            uint id = (uint)data;
            Measures[id].ExecuteBang(new string(args));
        }
    }

}
