using System;
using System.Text;
using System.Runtime.InteropServices;

namespace fomultirun
{
    [Flags]
    public enum ProcessCreationFlags : uint
    {
        ZERO_FLAG = 0x00000000,
        CREATE_BREAKAWAY_FROM_JOB = 0x01000000,
        CREATE_DEFAULT_ERROR_MODE = 0x04000000,
        CREATE_NEW_CONSOLE = 0x00000010,
        CREATE_NEW_PROCESS_GROUP = 0x00000200,
        CREATE_NO_WINDOW = 0x08000000,
        CREATE_PROTECTED_PROCESS = 0x00040000,
        CREATE_PRESERVE_CODE_AUTHZ_LEVEL = 0x02000000,
        CREATE_SEPARATE_WOW_VDM = 0x00001000,
        CREATE_SHARED_WOW_VDM = 0x00001000,
        CREATE_SUSPENDED = 0x00000004,
        CREATE_UNICODE_ENVIRONMENT = 0x00000400,
        DEBUG_ONLY_THIS_PROCESS = 0x00000002,
        DEBUG_PROCESS = 0x00000001,
        DETACHED_PROCESS = 0x00000008,
        EXTENDED_STARTUPINFO_PRESENT = 0x00080000,
        INHERIT_PARENT_AFFINITY = 0x00010000
    }

    public struct STARTUPINFO
    {
        public uint cb;
        public string lpReserved;
        public string lpDesktop;
        public string lpTitle;
        public uint dwX;
        public uint dwY;
        public uint dwXSize;
        public uint dwYSize;
        public uint dwXCountChars;
        public uint dwYCountChars;
        public uint dwFillAttribute;
        public uint dwFlags;
        public short wShowWindow;
        public short cbReserved2;
        public IntPtr lpReserved2;
        public IntPtr hStdInput;
        public IntPtr hStdOutput;
        public IntPtr hStdError;
    }

    public struct PROCESS_INFORMATION
    {
        public IntPtr hProcess;
        public IntPtr hThread;
        public uint dwProcessId;
        public uint dwThreadId;
    }

    class Program
    {
        const int PAGE_EXECUTE_READWRITE = 0x40;

        #region invokes
        [DllImport("kernel32.dll")]
        public static extern bool CreateProcess(string lpApplicationName,
           string lpCommandLine, IntPtr lpProcessAttributes,
           IntPtr lpThreadAttributes,
           bool bInheritHandles, ProcessCreationFlags dwCreationFlags,
           IntPtr lpEnvironment, string lpCurrentDirectory,
           ref STARTUPINFO lpStartupInfo,
           out PROCESS_INFORMATION lpProcessInformation);

        [DllImport("kernel32.dll")]
        public static extern uint ResumeThread(IntPtr hThread);

        [DllImport("kernel32.dll")]
        public static extern uint SuspendThread(IntPtr hThread);

        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(int dwDesiredAccess,
               bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool WriteProcessMemory(IntPtr hProcess, int lpBaseAddress,
          byte[] lpBuffer, int dwSize, out int lpNumberOfBytesWritten);

        [DllImport("kernel32.dll")]
        static extern bool VirtualProtectEx(IntPtr hProcess, int lpAddress,
           int dwSize, uint flNewProtect, out uint lpflOldProtect);

        [DllImport("kernel32.dll")]
        public static extern bool ReadProcessMemory(IntPtr hProcess,
            int lpBaseAddress, byte[] lpBuffer, int dwSize, out int lpNumberOfBytesRead);
        #endregion

        static string generateRandomString(int len)
        {
            var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var stringChars = new char[len];
            var random = new Random();

            for (int i = 0; i < stringChars.Length; i++)
            {
                stringChars[i] = chars[random.Next(chars.Length)];
            }

            var finalString = new String(stringChars);
            return finalString;
        }

        static void run()
        {
            string[] args = Environment.GetCommandLineArgs();
            if(args.Length < 2)
            {
                Console.WriteLine("Usage:");
                Console.WriteLine(@"fomultirun path\to\fonline.exe");
                Console.WriteLine("");
                Console.WriteLine("For example:");
                Console.WriteLine(@"fomultirun C:\Games\FOnline Reloaded\FOnline.exe");
                Environment.Exit(1);
            }
            string processpath = args[1];
            if(!System.IO.File.Exists(processpath))
            {
                Console.WriteLine("Invalid path: " + processpath);
                Environment.Exit(2);
            }

            //create the proces in SUSPENDED state
            STARTUPINFO si = new STARTUPINFO();
            PROCESS_INFORMATION pi = new PROCESS_INFORMATION();
            bool success = CreateProcess(processpath, null,
                IntPtr.Zero, IntPtr.Zero, false,
                ProcessCreationFlags.CREATE_SUSPENDED,
                IntPtr.Zero, null, ref si, out pi);

            //get full access to it
            IntPtr processHandle = OpenProcess(0x1F0FFF, false, (int)pi.dwProcessId);
            Console.WriteLine("Handle: " + (int)pi.dwProcessId);
            
            { // replaces foclassic_instance
                byte[] buffer = Encoding.ASCII.GetBytes(generateRandomString(18));
                uint OLD = 0;
                int bytesWritten = 0;
                VirtualProtectEx(processHandle, 0x015BFA88, buffer.Length, PAGE_EXECUTE_READWRITE, out OLD);
                WriteProcessMemory(processHandle, 0x015BFA88, buffer, buffer.Length, out bytesWritten);
                Console.WriteLine("Bytes written: " + bytesWritten.ToString());
            }

            { // replaces _fcsync_
                byte[] buffer = Encoding.ASCII.GetBytes(generateRandomString(8));
                uint OLD = 0;
                int bytesWritten = 0;
                VirtualProtectEx(processHandle, 0x015BC920, buffer.Length, PAGE_EXECUTE_READWRITE, out OLD);
                WriteProcessMemory(processHandle, 0x015BC920, buffer, buffer.Length, out bytesWritten);
                Console.WriteLine("Bytes written: " + bytesWritten.ToString());
            }
            
            //start the thread
            IntPtr ThreadHandle = pi.hThread;
            ResumeThread(ThreadHandle);

            //finish!            
        }

        static void Main(string[] args)
        {
            run();
        }
    }
}
