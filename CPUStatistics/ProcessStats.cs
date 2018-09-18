using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using System.Diagnostics;
using System.Reflection;

namespace CPUStatistics
{
    class ProcessStats
    {
        private const string  m_txtModuleName = ".dll";//keep this string for future use 
        private myMap m_refmap;
        private ArrayList m_checkedProcInfoList = new ArrayList();
        private Hashtable m_hsParentInfo = new Hashtable();
        private Hashtable m_hsCpuUsageObjects = new Hashtable();
        private Hashtable m_hsParentProcObjects = new Hashtable();

        private PerformanceCounter m_totalCpuUsage = new PerformanceCounter("Process", "% Processor Time", "_Total");//total = CPU cycle per process +idle time
        private PerformanceCounter m_idleCpuUsage = new PerformanceCounter("Process", "% Processor Time", "Idle"); //idle time

        public ArrayList getProcInfo()
        {
            return m_checkedProcInfoList;
        }

        private bool isProcessExists(int processID, string processName)
        {
            bool bIsProcessIDExists = false;
            Process[] procList = Process.GetProcessesByName(processName);
            foreach (Process proc in procList)
            { 
                if(processID == proc.Id)
                {
                    bIsProcessIDExists = true;
                    break;
                }
            }
            return bIsProcessIDExists;
        }
        public void updateProcessList()
        {
            for (int i = 0; i < Convert.ToInt64(m_refmap.getValue(txtString.m_txtprocessCount)); i++)
            {
                string strProcName = m_refmap.getValue(txtString.m_txtmonProcess + i.ToString());
                //get all the instances of the process
                Process[] procList = Process.GetProcessesByName(strProcName);
                foreach (Process proc in procList)
                {
                    if (m_hsCpuUsageObjects.ContainsKey(proc.Id))//if the process id is available then check if the process exists
                    {
                        if (!isProcessExists(proc.Id, strProcName))
                        {
                            m_hsCpuUsageObjects.Remove(proc.Id);
                            updatePerfCounterList(strProcName);
                        }
                    }
                    else
                    {
                        m_hsCpuUsageObjects.Add(proc.Id, new CpuUsage(proc.Id));
                        updatePerfCounterList(strProcName);
                    }

                }
            }
        }
        private CpuUsage getCpuUsageObject(int processID)
        {
            CpuUsage retCpuUsage = null;
            IDictionaryEnumerator iter = m_hsCpuUsageObjects.GetEnumerator();
            while (iter.MoveNext())
            {
                if ((int)iter.Key == processID)
                {
                    retCpuUsage = (CpuUsage)iter.Value;
                    break;
                }
            }
            return retCpuUsage;
        }
        private void updatePerfCounterList(string strProcName)
        {
            //perf counter objects first removed and then added to keep consistency with the process name field 
            //in performance counter eg "notepad, notepad#1, notepad#2 etc"
            try
            {
                Process[] procList = Process.GetProcessesByName(strProcName);
                if (procList.Length > 0)//first delete from hash table
                {
                    foreach (Process proc in procList)
                    {
                        m_hsParentProcObjects.Remove(proc.Id);
                        m_hsParentInfo.Remove(proc.Id);
                    }

                }
                for (int index = 0; index < procList.Length; index++)//create counter for each instance of the process
                {
                    string processIndexName = index == 0 ? strProcName : strProcName + "#" + index;
                    PerformanceCounter perfCtrobj = new PerformanceCounter("Process", "Creating Process Id", processIndexName);
                    //for freshly added processes update the parent process info
                    Process parentInfo = Process.GetProcessById((int)perfCtrobj.RawValue);
                    m_hsParentInfo.Add(procList[index].Id, parentInfo.ProcessName);
                    m_hsParentProcObjects.Add(procList[index].Id, perfCtrobj);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("updatePerfCounterList : " + ex.Message);
            }

        }


        public void initializeCounterObjects(myMap map)
        {
            m_refmap = map;
            updateProcessList();
        }
        private bool isValidProcess(Process proc, string strModuleName)
        {
            if (m_txtModuleName.Equals(strModuleName))
                return true;//consider that the there is no specific module name mentioned

            bool bModuleAvailable = false;
            foreach(ProcessModule procModule in proc.Modules)
            {
                if (true == procModule.ModuleName.Equals(strModuleName))
                {
                    bModuleAvailable = true;
                    break;
                }
            }
            return bModuleAvailable;
        }
        public void updateProcInfoList()
        {
            m_checkedProcInfoList.Clear();
            MemUsage.nextCounter();
            int countTotalThread = getTotalThreadCount();
            decimal cpuAllProcessesPerc = getTotalCpuPercentage();
            decimal memAllProcessesPerc = (decimal)(MemUsage.m_memCommitted * 100) / MemUsage.m_memTotal;

            try
            {
                for (int i = 0; i < Convert.ToInt64(m_refmap.getValue(txtString.m_txtprocessCount)); i++)
                {
                    string strProcName = m_refmap.getValue(txtString.m_txtmonProcess + i.ToString());
                    string strModuleName = m_refmap.getValue(txtString.m_txtmonModule + i.ToString());
                    string strProcDisplayName = m_refmap.getValue(txtString.m_txtdisplayName + i.ToString());
                    //get all the instances of the process
                    Process[] procList = Process.GetProcessesByName(strProcName);
                    for (int instance = 0; instance < procList.Length; instance++)
                    {
                        if (false == isValidProcess(procList[instance], strModuleName))
                            continue;
                        ProcessInfo procInfo = new ProcessInfo();
                        //process name
                        if(strProcDisplayName.Equals(""))
                            procInfo.Name = procList[instance].ProcessName;
                        else
                            procInfo.Name = strProcDisplayName;

                        //process ID
                        procInfo.ID = procList[instance].Id;
                        //calculate cpu percentage
                        procInfo.CpuPercentageUsage = getCpuUsageObject(procList[instance].Id).GetUsage();
                        //get total memory used
                        procInfo.MemoryAvailable = MemUsage.m_memAvailable;
                        procInfo.MemoryCommitted = MemUsage.m_memCommitted;
                        //memory percentage usage
                        procInfo.MemUsage = procList[instance].WorkingSet64;//let it be assigned here 
                        procInfo.MemPercentageUsage = (decimal)(procList[instance].WorkingSet64 * 100) / MemUsage.m_memTotal;
                        //number of threads used by process
                        procInfo.ThreadCount = procList[instance].Threads.Count;
                        //parent process
                        procInfo.ParentProcess = getParentName(procList[instance].Id);


                        //the data for total system as a whole
                        //cpu usage by all processes
                        procInfo.CpuAllProcessesPercUsage = (decimal)cpuAllProcessesPerc;
                        //memory usage by all processes
                        procInfo.MemAllProcessesPercUsage = (decimal)memAllProcessesPerc;
                        //total number of threads
                        procInfo.TotalThreadCount = countTotalThread;

                        m_checkedProcInfoList.Add(procInfo);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("updateProcInfoList : "+ ex.Message);
            }

        }
        //the cpu consumption by all the processes
        private decimal getTotalCpuPercentage()
        {
            decimal cpuAllProcess = 0;
            try
            {
                float total = m_totalCpuUsage.NextValue();
                float idle = m_idleCpuUsage.NextValue();
                float actual = total -idle;
                if(total > 0)
                    cpuAllProcess = (decimal)(actual * 100 / total);
            }
            catch (Exception ex)
            {
                Console.WriteLine("getTotalCpuPercentage : " + ex.Message);
            }
            return cpuAllProcess;
        }
        private int getTotalThreadCount()
        {
            int countThread = 0;
            Process[] procList =  Process.GetProcesses();
            foreach (Process proc in procList)
            {
                countThread += proc.Threads.Count;
            }

            return countThread;
        }

        private string getParentName(int processID)
        {
            string strParnetName = null;
            IDictionaryEnumerator iter = m_hsParentInfo.GetEnumerator();
            while (iter.MoveNext())
            {
                if ((int)iter.Key == processID)
                {
                    strParnetName = (string)iter.Value;
                    break;
                }
                
            }
            return strParnetName;
        }

    }

    public class ProcessInfo
    {
        public string Name;
        public long   CpuUsage;
        public int    ID;
        public long MemUsage;
        public long MemoryAvailable;
        public long MemoryCommitted;
        public string ParentProcess;
        public int ThreadCount;
        public float CpuPercentageUsage;
        public decimal MemPercentageUsage;
        public decimal CpuAllProcessesPercUsage;
        public decimal MemAllProcessesPercUsage;
        public decimal TotalThreadCount;

        public ProcessInfo()
        {
            Name = "";
            CpuUsage = 0;
            ID = 0;
            MemUsage = 0;
            MemoryAvailable = 0;
            MemoryCommitted = 0;
            ParentProcess = "";
            ThreadCount = 0;
            CpuPercentageUsage = 0;
        }
    }
}
