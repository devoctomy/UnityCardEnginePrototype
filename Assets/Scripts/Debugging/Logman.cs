﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Assets.Scripts.Debugging
{

    public class Logman
    {

        #region private objects

        private static Logman cLMnCurrent;
        private String cStrLogRoot = String.Empty;
        private String cStrDefaultLogName = String.Empty;
        private Dictionary<String, BaseLogger> cDicLogs;

        #endregion

        #region public properties

        public static Logman Current
        {
            get
            {
                return (cLMnCurrent);
            }
        }

        public String LogRoot
        {
            get
            {
                return (cStrLogRoot);
            }
        }

        public String DefaultLogName
        {
            get
            {
                return (cStrDefaultLogName);
            }
        }

        public Dictionary<String, BaseLogger> Loggers
        {
            get
            {
                return (cDicLogs);
            }
        }

        #endregion

        #region constructor / destructor

        private Logman(String iLogRoot,
            String iDefaultLogName)
        {
            UnityEngine.Debug.Log(String.Format("Initialising Logman with root of '{0}'.", iLogRoot));
            cStrLogRoot = iLogRoot;
            if (!cStrLogRoot.EndsWith("/")) cStrLogRoot += "/";
            Directory.CreateDirectory(cStrLogRoot);
            cStrDefaultLogName = iDefaultLogName;
            cDicLogs = new Dictionary<String, BaseLogger>();
        }

        #endregion

        #region public methods

        public static void Initialise(String iLogRoot,
            String iDefaultLogName)
        {
            cLMnCurrent = new Logman(iLogRoot,
                iDefaultLogName);
        }

        public static void CreateLog<LoggerType>(String iName,
            Dictionary<String, String> iProperties) where LoggerType : BaseLogger
        {
            if (Current != null)
            {
                Type pTypLogger = typeof(LoggerType);

                PropertyInfo pPIoProperties = null;
                PropertyInfo[] pPIoAllProperties = pTypLogger.GetProperties(BindingFlags.Static | BindingFlags.Public);
                foreach (PropertyInfo curProperty in pPIoAllProperties)
                {
                    if (curProperty.Name == "Properties" &&
                        curProperty.PropertyType == typeof(String[]))
                    {
                        pPIoProperties = curProperty;
                    }
                }
                if (pPIoProperties == null)
                {
                    throw new Exception(String.Format("Logger of type '{0}' does not have a public static property named 'Properties' of type String[].", pTypLogger.Name));
                }

                MethodInfo pMIoCreate = pTypLogger.GetMethod("Create",
                    BindingFlags.Static | BindingFlags.Public);
                if (pMIoCreate != null)
                {
                    String[] pStrProperties = (String[])pPIoProperties.GetValue(null, null);
                    foreach (String curProperty in pStrProperties)
                    {
                        if (!iProperties.ContainsKey(curProperty))
                        {
                            throw new Exception(String.Format("Logger of type '{0}' requires a property with the key '{0}'.", pTypLogger.Name, curProperty));
                        }
                    }

                    Object[] pObjParams = { Current, iName, iProperties };
                    BaseLogger pBLrLogger = (BaseLogger)pMIoCreate.Invoke(null, pObjParams);
                    pBLrLogger.Log(BaseLogger.MessageType.Information, 
                        "Started logging '{0}' using logger type of '{1}' at location '{2}'.", 
                        iName, 
                        typeof(LoggerType).Name,
                        ((FileLogger)pBLrLogger).FullPath);         //!!!TODO: this shouldn't assume logger type
                    Current.cDicLogs.Add(iName, pBLrLogger);
                }
                else
                {
                    throw new Exception(String.Format("Logger of type '{0}' does not have a public static method named 'Create'.", pTypLogger.Name));
                }
            }
        }

        public static void Log(BaseLogger.MessageType iMessageType,
            String iMessageFormat,
            params Object[] iParams)
        {
            if (Current != null)
            {
                Log(Current.DefaultLogName,
                    iMessageType,
                    iMessageFormat,
                    iParams);
            }
        }

        public static void Log(String iName,
            BaseLogger.MessageType iMessageType,
            String iMessageFormat,
            params Object[] iParams)
        {
            if (Current != null)
            {
                if (Current.Loggers.ContainsKey(iName))
                {
                    Current.Loggers[iName].Log(iMessageType,
                        iMessageFormat,
                        iParams);
                }
                else
                {
                    //Do nothing at the moment
                }
            }
        }

        public static void LogException(Exception iException)
        {
            if (Current != null)
            {
                LogException(Current.DefaultLogName, iException);
            }
        }

        public static void LogException(String iName,
            Exception iException)
        {
            if(Current != null)
            {
                if (Current.Loggers.ContainsKey(iName))
                {
                    Current.Loggers[iName].LogException(iException);
                }
                else
                {
                    //Do nothing at the moment
                }
            }
        }

        public static DisposableLogger Time(String iOperation,
            BaseLogger.MessageType iMessageType)
        {
            return (DisposableLogger.Create(Current.DefaultLogName, 
                iMessageType, 
                iOperation));
        }

        public static DisposableLogger Time(String iName,
            BaseLogger.MessageType iMessageType,
            String iOperation)
        {
            return (DisposableLogger.Create(iName, 
                iMessageType,
                iOperation));
        }

        #endregion

    }

}
