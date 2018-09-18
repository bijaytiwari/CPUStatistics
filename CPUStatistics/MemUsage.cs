using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;
using ComTypes = System.Runtime.InteropServices.ComTypes;

namespace CPUStatistics
{
    [StructLayout(LayoutKind.Sequential,CharSet =CharSet.Auto)]
    public class MEMORYSTATUSEX
    {
        public uint dwLength;
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;

        public MEMORYSTATUSEX()
        {
            this.dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
            dwMemoryLoad = 0;
            ullTotalPhys = 0;
            ullAvailPhys = 0;
            ullTotalPageFile = 0;
            ullAvailPageFile = 0;
            ullTotalVirtual = 0;
            ullAvailVirtual = 0;
            ullAvailExtendedVirtual = 0;
        }
    }

    class MemUsage
    {
        public static long m_memAvailable = 0;
        public static long m_memCommitted = 0;
        public static long m_memTotal = 0;
        //all private members
        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern bool GlobalMemoryStatusEx([In, Out]MEMORYSTATUSEX lpBuffer);


        public static void nextCounter()
        {
            try
            {
                MEMORYSTATUSEX memStatus = new MEMORYSTATUSEX();

                if (GlobalMemoryStatusEx(memStatus))
                {
                    m_memTotal = (long)memStatus.ullTotalPhys;
                    m_memAvailable = (long)memStatus.ullAvailPhys;
                    m_memCommitted = m_memTotal - m_memAvailable;
                    //Console.WriteLine("m_memCommitted =" + m_memCommitted/1024 + "m_memAvailable =" + m_memAvailable/1024 + "m_memTotal=" + m_memTotal/1024);
                }

                
            }
            catch (Exception ex)
            {
                Console.WriteLine("nextCounter: " + ex.Message);
            }

        }
    }
}
