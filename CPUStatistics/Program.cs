using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Net.Sockets;
using System.Net;
using System.Collections;
namespace CPUStatistics
{
    class Program
    {
        static public IPHostEntry m_Iphe = null;
        static public IPEndPoint m_Ipep = null;
        static public Socket m_socClient = null;
        public static ProcessStats m_procStats = null;
        public static myMap m_map = new myMap();
        public static string m_strStatsLogFile = "augmentCPUStatsistics.txt";

        static public bool m_bIsSocketConnected = true;
        static public ThreadStart m_thStartTransportData = null;
        static public Thread m_thTransportData = null;

        static public Mutex m_mutSyncFile = new Mutex();

        static void sendDataToServer()
        {
            //socket connection
            m_Iphe = Dns.GetHostEntry(m_map.getValue(txtString.m_txtserverIP));
            m_Ipep = new IPEndPoint(m_Iphe.AddressList[0], (int)Convert.ToInt64(m_map.getValue(txtString.m_txtserverPort)));
            m_socClient = new Socket(m_Ipep.Address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                m_socClient.Connect(m_map.getValue(txtString.m_txtserverIP), (int)Convert.ToInt64(m_map.getValue(txtString.m_txtserverPort)));
            }
            catch (SocketException exc)
            {
                m_bIsSocketConnected = false;
                Console.WriteLine("Socket connection failed : " + exc.Message);
            }

            int sendServerIter = (int)Convert.ToInt64(m_map.getValue(txtString.m_txtsendServerIter));
            sendServerIter = sendServerIter * 60;//as the value will be in minutes
            int counter = 0;
            while (true == m_bIsSocketConnected && true == m_socClient.Connected)
            {
                if (counter == sendServerIter)
                {
                    counter = 0;//reset the counter
                    try
                    {
                        m_mutSyncFile.WaitOne();
                        string strMessage="";
                        if(true == System.IO.File.Exists(m_strStatsLogFile) )
                            strMessage = System.IO.File.ReadAllText(m_strStatsLogFile);
                        m_mutSyncFile.ReleaseMutex();
                        if (strMessage.Length > 0)//make a socket connection when there is any data available
                        {
                            byte[] byData = System.Text.Encoding.ASCII.GetBytes(strMessage.ToString());
                            if (byData.Length == m_socClient.Send(byData))
                            {
                                //soon after the data has been sent to the server the data should be erased
                                m_mutSyncFile.WaitOne();
                                System.IO.File.WriteAllText(m_strStatsLogFile, "");
                                m_mutSyncFile.ReleaseMutex();
                            }
                        }
                    }
                    catch (SocketException exSoc)
                    {
                        m_bIsSocketConnected = false;
                        Console.WriteLine("Send data failed : "+exSoc.Message);
                        //at any cost delete the content of the file, else for a non existent socket the file size keeps on increasing
                        m_mutSyncFile.WaitOne();
                        System.IO.File.WriteAllText(m_strStatsLogFile, "");
                        m_mutSyncFile.ReleaseMutex();
                    }
                }
                else
                {
                    Thread.Sleep(1000);
                    counter++;
                }
            }
        }

        static void Main(string[] args)
        {
            if (false == m_map.setMap(args))
            {
                Thread.Sleep(2000);
                return;
            }

            System.IO.File.Delete(m_strStatsLogFile);//delete the previously created contents

            m_thStartTransportData = new ThreadStart(sendDataToServer);
            m_thTransportData = new Thread(m_thStartTransportData);
            m_thTransportData.Start();

            string delimeter = ",";
            int serviceSelfIter = (int)Convert.ToInt64(m_map.getValue(txtString.m_txtserviceSelfIter));

            m_procStats = new ProcessStats();
            m_procStats.initializeCounterObjects(m_map);
            string strMessage = null;
            bool bIsDataAvailable = false;

            while (m_bIsSocketConnected)
            {
                m_procStats.updateProcInfoList();
                IEnumerator enumrator = m_procStats.getProcInfo().GetEnumerator();
                ProcessInfo procInfo = null;
                 while (enumrator.MoveNext())
                 {
                     procInfo = (ProcessInfo)enumrator.Current;
                     //Console.WriteLine("Name ="+ procInfo.Name+" Cpu %= " + procInfo.CpuPercentageUsage);
                     if (procInfo.CpuPercentageUsage >= 0)
                     {
                         strMessage += Convert.ToString(System.DateTime.Now.ToString("MMM-dd-hh:mm:ss")) + delimeter + procInfo.Name + delimeter + procInfo.ID.ToString() + delimeter + procInfo.CpuPercentageUsage.ToString("F") + delimeter + procInfo.MemPercentageUsage.ToString("F") + delimeter + procInfo.ThreadCount.ToString() + delimeter + procInfo.ParentProcess + "\n";
                         bIsDataAvailable = true;
                     }
                 }
                 if (true == bIsDataAvailable)
                 {
                     bIsDataAvailable = false;
                     strMessage += Convert.ToString(System.DateTime.Now.ToString("MMM-dd-hh:mm:ss")) + delimeter + "Total" + delimeter + "0" + delimeter + procInfo.CpuAllProcessesPercUsage.ToString("F") + delimeter + procInfo.MemAllProcessesPercUsage.ToString("F") + delimeter + procInfo.TotalThreadCount.ToString() + delimeter + "total\n";
                     Console.WriteLine(strMessage);
                     m_mutSyncFile.WaitOne();
                     System.IO.File.AppendAllText(m_strStatsLogFile, strMessage);
                     m_mutSyncFile.ReleaseMutex();
                 }
                 strMessage = "";
                 Thread.Sleep(serviceSelfIter*1000);
                 m_procStats.updateProcessList();//update if there are more processes launched
            }
            if (false == m_bIsSocketConnected)
                Thread.Sleep(2000);//just to see the error message
            m_thTransportData.Abort();

        }
    }
}
