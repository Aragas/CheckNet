using System;
using System.Collections;
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

        bool CaseOne;
        int CaseTwo;
        string CaseThree;

        public void FinishAction()
        {
            if (!String.IsNullOrEmpty(Action))
            {
                API.Execute(_skinHandle, Action);
            }
        }

        private void TypeVoid(MeasureType type, RulyCanceler c)
        {
            while (true)
            {
                c.ThrowIfCancellationRequested();

                #region Check

                if (type == MeasureType.CaseOne)
                {
                    CaseOne = NetworkInterface.GetIsNetworkAvailable();
                }

                if (type == MeasureType.CaseTwo)
                {
                    // If Network is broken - we don't need to check internet.
                    if (NetworkInterface.GetIsNetworkAvailable())
                    {
                        try
                        {
                            CaseTwo = 1234;
                        }
                        catch
                        {
                            CaseTwo = 12345;
                        }
                    }
                }

                if (type == MeasureType.CaseThree)
                {
                    CaseThree = "www.msftncsi.com";
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

            #region FinishAction
            _skinHandle = rm.GetSkin();
            Action = rm.ReadString("FinishAction", "");
            #endregion

            #region Update
            UpdateRate = rm.ReadInt("UpdateRate", 20);
            if (UpdateRate <= 0)
            {
                UpdateRate = 20;
            }
            #endregion
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
                                TypeVoid(MeasureType.CaseOne, _canceler);
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
                                TypeVoid(MeasureType.CaseTwo, _canceler);
                            }
                            catch (OperationCanceledException) {}
                        }).Start();
                    }

                    ReturnValueDouble = CaseTwo;
                    break;

                #endregion
            }

            #region Update
            UpdateCounter = UpdateCounter + 1;
            if (UpdateCounter >= UpdateRate)
            {
                UpdateCounter = 0;
                if (UpdatedString) UpdatedString = false;
            }
            #endregion

            return ReturnValueDouble;
        }

        internal string GetString()
        {
            switch (Type)
            {
                case MeasureType.CaseThree:
                    if (!UpdatedString)
                    {
                        RulyCanceler _canceler = new RulyCanceler();
                        new Thread(() =>
                        {
                            try
                            {
                                TypeVoid(MeasureType.CaseThree, _canceler);
                            }
                            catch (OperationCanceledException) {}
                        }).Start();
                        UpdatedString = true;
                    }

                    ReturnValueString = CaseThree;
                    break;
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

        #region Update
        bool UpdatedString;
        int UpdateCounter;
        int UpdateRate;
        #endregion

        #region ReturnValues
        string ReturnValueString;
        double ReturnValueDouble;
        #endregion

        #region FinishAction
        IntPtr _skinHandle;
        string Action;
        #endregion
    }

    internal class RulyCanceler
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
