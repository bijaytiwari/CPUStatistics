using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using System.Xml;
using System.IO;

namespace CPUStatistics
{
    public class myMap
    {
        private Hashtable m_hsMap = new Hashtable();
        private const string m_fiveSeconds = "5";
        private const string m_tenMinutes = "10";
        public bool setMap(string[] arguments)
        {
            m_hsMap.Add(txtString.m_txtserverIP, "127.0.0.1");
            m_hsMap.Add(txtString.m_txtserverPort, "5000");
            m_hsMap.Add(txtString.m_txtserviceSelfIter, m_fiveSeconds);
            m_hsMap.Add(txtString.m_txtsendServerIter, m_tenMinutes);

            if (true == LoadConfig())
                return ValidateEntries();
            else
                return false;
        }

        public string getValue(string key)
        {
            string value = "";
            IDictionaryEnumerator iter = m_hsMap.GetEnumerator(); 
            while(iter.MoveNext())
            {
                if (iter.Key.ToString().CompareTo(key) == 0)
                {
                    value = (string)iter.Value;
                    break;
                }
            }
            return value;
        }

        private bool LoadConfig()
        {
            bool bRet = false;
            XmlDocument doc = new XmlDocument();
            Console.WriteLine("Searching for " + Directory.GetCurrentDirectory() + "\\config.xml\n");
            if (true == File.Exists(Directory.GetCurrentDirectory() + "\\config.xml"))
            {
                try
                {
                    doc.Load("config.xml");
                    XmlElement root = doc.DocumentElement;
                    XmlNodeList nodeList = root.GetElementsByTagName("param");
                    IDictionaryEnumerator iter = m_hsMap.GetEnumerator();
                    foreach (XmlNode node in nodeList)
                    {
                        //m_hsMap.Add(node.Attributes["arg"].Value.ToString(), node.Attributes["defaultVal"].Value.ToString());
                        while (iter.MoveNext())
                        {
                            if (iter.Key.ToString().Contains(node.Attributes["arg"].Value.ToString()))
                            {
                                m_hsMap.Remove(node.Attributes["arg"].Value.ToString());
                                m_hsMap.Add(node.Attributes["arg"].Value.ToString(), node.Attributes["defaultVal"].Value.ToString());
                                iter = m_hsMap.GetEnumerator();
                                break;
                            }
                        }
                        iter.Reset();
                    }
                    XmlNodeList processList = root.GetElementsByTagName("process");
                    int countProcess = 0;
                    foreach (XmlNode nodeProc in processList)
                   {
                       m_hsMap.Add(nodeProc.Attributes["arg1"].Value.ToString() + countProcess.ToString(), nodeProc.Attributes["defaultVal1"].Value.ToString());
                       m_hsMap.Add(nodeProc.Attributes["arg2"].Value.ToString() + countProcess.ToString(), nodeProc.Attributes["defaultVal2"].Value.ToString());
                       m_hsMap.Add(nodeProc.Attributes["arg3"].Value.ToString() + countProcess.ToString(), nodeProc.Attributes["defaultVal3"].Value.ToString());
                       bRet = true;
                       countProcess++;
                   }
                   m_hsMap.Add(txtString.m_txtprocessCount, countProcess.ToString());//count the number of process configured for monitoring

                }
                catch (Exception ex)
                {
                    bRet = false;
                    Console.WriteLine("LoadConfig : " + ex.Message);
                }
             
            }
            else
            {
                Console.WriteLine("The configuration file (config.xml) does not exist. Exiting....");
            }
            return bRet;
        }
        private bool ValidateEntries()
        {
            bool bRet = true;
            IDictionaryEnumerator iter = m_hsMap.GetEnumerator();

            try
            {
                for (int i = 0; i < 2; i++)//one for each of the iteration values
                {
                    while (iter.MoveNext())
                    {
                        if (iter.Key.ToString().Contains(txtString.m_txtserviceSelfIter))
                        {
                            if (Convert.ToInt64(iter.Value) <= 0)
                            {
                                m_hsMap.Remove(txtString.m_txtserviceSelfIter);
                                m_hsMap.Add(txtString.m_txtserviceSelfIter, m_fiveSeconds);
                                iter = m_hsMap.GetEnumerator();
                                break;
                            }
                        }
                        if (iter.Key.ToString().Contains(txtString.m_txtsendServerIter))
                        {
                            if (Convert.ToInt64(iter.Value) <= 0)
                            {
                                m_hsMap.Remove(txtString.m_txtsendServerIter);
                                m_hsMap.Add(txtString.m_txtsendServerIter, m_tenMinutes);
                                iter = m_hsMap.GetEnumerator();
                                break;
                            }
                        }
                    }
                    iter.Reset();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message+" Please provide correct inputs in config.xml or command line arguments. Exiting...");
                bRet = false;
            }
            return bRet;
        }
    }//end of class

    class txtString
    {
        public static string m_txtserverIP = "-serverIP";
        public static string m_txtserverPort = "-serverPort";
        public static string m_txtserviceSelfIter = "-serviceSelfIter";
        public static string m_txtsendServerIter = "-sendServerIter";
        public static string m_txtmonProcess = "-monProcess";
        public static string m_txtmonModule = "-monModule";
        public static string m_txtprocessCount = "processCount";//count the number of process configured for monitoring
        public static string m_txtdisplayName = "-displayName";
    }
}
