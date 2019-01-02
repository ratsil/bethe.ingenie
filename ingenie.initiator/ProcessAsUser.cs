using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using helpers.extensions;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace ingenie
{
    public class ProcessAsUser
    {

        [StructLayout(LayoutKind.Sequential)]
        internal struct PROCESS_INFORMATION
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public uint dwProcessId;
            public uint dwThreadId;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct SECURITY_ATTRIBUTES
        {
            public uint nLength;
            public IntPtr lpSecurityDescriptor;
            public bool bInheritHandle;
        }

        [StructLayout(LayoutKind.Sequential)]
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

        internal enum SECURITY_IMPERSONATION_LEVEL
        {
            SecurityAnonymous,
            SecurityIdentification,
            SecurityImpersonation,
            SecurityDelegation
        }

        internal enum TOKEN_TYPE
        {
            TokenPrimary = 1,
            TokenImpersonation
        }

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool CreateProcessAsUser(
            IntPtr hToken,
            string lpApplicationName,
            string lpCommandLine,
            ref SECURITY_ATTRIBUTES lpProcessAttributes,
            ref SECURITY_ATTRIBUTES lpThreadAttributes,
            bool bInheritHandles,
            uint dwCreationFlags,
            IntPtr lpEnvironment,
            string lpCurrentDirectory,
            ref STARTUPINFO lpStartupInfo,
            out PROCESS_INFORMATION lpProcessInformation);


        [DllImport("advapi32.dll", EntryPoint = "DuplicateTokenEx", SetLastError = true)]
        private static extern bool DuplicateTokenEx(
            IntPtr hExistingToken,
            uint dwDesiredAccess,
            ref SECURITY_ATTRIBUTES lpThreadAttributes,
            Int32 ImpersonationLevel,
            Int32 dwTokenType,
            ref IntPtr phNewToken);


        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool OpenProcessToken(
            IntPtr ProcessHandle,
            UInt32 DesiredAccess,
            ref IntPtr TokenHandle);

        [DllImport("userenv.dll", SetLastError = true)]
        private static extern bool CreateEnvironmentBlock(
                ref IntPtr lpEnvironment,
                IntPtr hToken,
                bool bInherit);


        [DllImport("userenv.dll", SetLastError = true)]
        private static extern bool DestroyEnvironmentBlock(
                IntPtr lpEnvironment);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(
            IntPtr hObject);

        private const short SW_HIDE = 0;
        private const short SW_SHOW = 5;
        private const uint TOKEN_QUERY = 0x0008;
        private const uint TOKEN_DUPLICATE = 0x0002;
        private const uint TOKEN_ASSIGN_PRIMARY = 0x0001;
		private const uint TOKEN_IMPERSONATE = 0x0004;
        private const int GENERIC_ALL_ACCESS = 0x10000000;
        private const int STARTF_USESHOWWINDOW = 0x00000001;
        private const int STARTF_FORCEONFEEDBACK = 0x00000040;
        private const uint CREATE_UNICODE_ENVIRONMENT = 0x00000400;
		private const uint MAXIMUM_ALLOWED = 0x2000000;
		private const uint CREATE_NEW_CONSOLE = 0x00000010;

        [DllImport("user32.dll")]
        //[return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetForegroundWindow(IntPtr hWnd);
        [DllImport("user32.dll")]
        private static extern bool SetFocus(IntPtr hWnd);
        [DllImport("user32.dll")]
        internal static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private static void SetFocusOnWindow(IntPtr pWindowHandle)
        {
            ShowWindow(pWindowHandle, 3); // 3==maximized
            SetForegroundWindow(pWindowHandle);  //this.Handle
            SetFocus(pWindowHandle);
        }

        private static int LaunchProcessAsUser(string cmdLine, IntPtr token, IntPtr envBlock, bool bHideConsole)
        {
            int nRetVal = -1;


            PROCESS_INFORMATION pi = new PROCESS_INFORMATION();
            SECURITY_ATTRIBUTES saProcess = new SECURITY_ATTRIBUTES();
            SECURITY_ATTRIBUTES saThread = new SECURITY_ATTRIBUTES();
            saProcess.nLength = (uint)Marshal.SizeOf(saProcess);
            saThread.nLength = (uint)Marshal.SizeOf(saThread);

            STARTUPINFO si = new STARTUPINFO();
            si.cb = (uint)Marshal.SizeOf(si);


            //if this member is NULL, the new process inherits the desktop 
            //and window station of its parent process. If this member is 
            //an empty string, the process does not inherit the desktop and 
            //window station of its parent process; instead, the system 
            //determines if a new desktop and window station need to be created. 
            //If the impersonated user already has a desktop, the system uses the 
            //existing desktop. 

			si.lpDesktop = ""; //Modify as needed     // @"WinSta0\Default"
            si.dwFlags = STARTF_USESHOWWINDOW | STARTF_FORCEONFEEDBACK;
			si.wShowWindow = bHideConsole ? SW_HIDE : SW_SHOW; //SW_HIDE SW_SHOW
															   //Set other si properties as required. 

			if (!CreateProcessAsUser(token, null, cmdLine, ref saProcess, ref saThread, false, CREATE_UNICODE_ENVIRONMENT, envBlock, null, ref si, out pi))
			{
				int error = Marshal.GetLastWin32Error();
				string message = String.Format("CreateProcessAsUser Error: {0}", error);
				(new helpers.Logger()).WriteNotice(message);

			}
			else
				nRetVal = (int)pi.dwProcessId;
			return nRetVal;
        }


        private static IntPtr GetPrimaryToken(int processId)
        {
            IntPtr token = IntPtr.Zero;
            IntPtr primaryToken = IntPtr.Zero;
            bool retVal = false;
            Process p = null;

            try
            {
                p = Process.GetProcessById(processId);
            }

            catch (ArgumentException)
            {

                string details = String.Format("ProcessID {0} Not Available", processId);
                (new helpers.Logger()).WriteNotice(details);
                throw;
            }


            //Gets impersonation token 
            retVal = OpenProcessToken(p.Handle, TOKEN_DUPLICATE, ref token);
            if (retVal == true)
            {

                SECURITY_ATTRIBUTES sa = new SECURITY_ATTRIBUTES();
                sa.nLength = (uint)Marshal.SizeOf(sa);

				//Convert the impersonation token into Primary token    // TOKEN_ASSIGN_PRIMARY | TOKEN_DUPLICATE | TOKEN_QUERY
                retVal = DuplicateTokenEx(
                    token,
					MAXIMUM_ALLOWED,
                    ref sa,
                    (int)SECURITY_IMPERSONATION_LEVEL.SecurityIdentification,
                    (int)TOKEN_TYPE.TokenPrimary,
                    ref primaryToken);

                //Close the Token that was previously opened. 
                CloseHandle(token);
                if (retVal == false)
                {
                    string message = String.Format("DuplicateTokenEx Error: {0}", Marshal.GetLastWin32Error());
                    (new helpers.Logger()).WriteNotice(message);
                }

            }

            else
            {

                string message = String.Format("OpenProcessToken Error: {0}", Marshal.GetLastWin32Error());
                (new helpers.Logger()).WriteNotice(message);

            }

            //We'll Close this token after it is used. 
            return primaryToken;

        }

        private static IntPtr GetEnvironmentBlock(IntPtr token)
        {

            IntPtr envBlock = IntPtr.Zero;
            bool retVal = CreateEnvironmentBlock(ref envBlock, token, false);
            if (retVal == false)
            {

                //Environment Block, things like common paths to My Documents etc. 
                //Will not be created if "false" 
                //It should not adversley affect CreateProcessAsUser. 

                string message = String.Format("CreateEnvironmentBlock Error: {0}", Marshal.GetLastWin32Error());
                (new helpers.Logger()).WriteNotice(message);

            }
            return envBlock;
        }

		static public int Launch(string appCmdLine, int nProcessId, bool bHideConsole)
        {
            int nRetVal = -1;
			if (nProcessId > 1)
            {
				IntPtr pToken = GetPrimaryToken(nProcessId);

				if (pToken != IntPtr.Zero)
                {

					IntPtr envBlock = GetEnvironmentBlock(pToken);
					nRetVal = LaunchProcessAsUser(appCmdLine, pToken, envBlock, bHideConsole);
                    if (envBlock != IntPtr.Zero)
                        DestroyEnvironmentBlock(envBlock);

					CloseHandle(pToken);
                }

            }
			return nRetVal;
        }
        static public void SetFocusToProcess(Process cProcess)
        {
            //cProcess.Refresh();
            //cProcess.WaitForInputIdle(); //this is the key!!    // not working - no window in LocalSystem user account if launched as another user!!!
            //SetFocusOnWindow(cProcess.Handle);    // not working - no window in LocalSystem user account if launched as another user!!!
        }

        internal const int CTRL_C_EVENT = 0;
        [DllImport("kernel32.dll")]
        internal static extern bool GenerateConsoleCtrlEvent(uint dwCtrlEvent, uint dwProcessGroupId);
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool AttachConsole(uint dwProcessId);
        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        internal static extern bool FreeConsole();
        [DllImport("kernel32.dll")]
        static extern bool SetConsoleCtrlHandler(ConsoleCtrlDelegate HandlerRoutine, bool Add);
        // Delegate type to be used as the Handler Routine for SCCH
        delegate Boolean ConsoleCtrlDelegate(uint CtrlType);

        static public bool KillProcess2(Process p) // NOT WORKED FOR ME  ((
        {
            if (AttachConsole((uint)p.Id))
            {
                (new helpers.Logger()).WriteNotice("AttachConsole");
                SetConsoleCtrlHandler(null, true);
                try
                {
                    if (!GenerateConsoleCtrlEvent(CTRL_C_EVENT, 0))
                    {
                        (new helpers.Logger()).WriteNotice("false1");
                        return false;
                    }
                    p.WaitForExit();
                }
                finally
                {
                    FreeConsole();
                    SetConsoleCtrlHandler(null, false);
                }
                return true;
            }
            else
            {
                GenerateConsoleCtrlEvent(CTRL_C_EVENT, 0);
                (new helpers.Logger()).WriteNotice("GENERATE 2");
            }
            (new helpers.Logger()).WriteNotice("false2");
            return false;
        }
        static public void KillProcess3(Process p)
        {
            int nPID = p.Id;
            Process cmd = new Process();
            cmd.StartInfo.FileName = "cmd.exe";
            cmd.StartInfo.RedirectStandardInput = true;
            cmd.StartInfo.RedirectStandardOutput = true;
            cmd.StartInfo.CreateNoWindow = false;
            cmd.StartInfo.UseShellExecute = true;
            cmd.Start();

            string strCmdText;
            strCmdText = "taskkill /PID " + nPID;
            (new helpers.Logger()).WriteNotice(strCmdText);
            cmd.StandardInput.WriteLine(strCmdText);
            System.Threading.Thread.Sleep(10000);
            cmd.StandardInput.Flush();
            cmd.StandardInput.Close();
            cmd.WaitForExit();
            Console.WriteLine(cmd.StandardOutput.ReadToEnd());
        }
        //START CMD.EXE /c "taskkill /PID 26120"

        static public void KillProcess4(Process p) // работает, но только с форсом /F
        {
            int nPID = p.Id;
            string strCmdText;
            strCmdText = "taskkill /PID " + nPID + " /F";

            System.Security.SecureString theSecureString = new System.Security.SecureString();
            string sPass = "htgkbrf";
            for (int nI = 0; nI < sPass.Length; nI++)
                theSecureString.AppendChar(sPass[nI]);
            ProcessStartInfo cProcessStartInfo = new ProcessStartInfo()
            {
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                UseShellExecute = false,
                //UserName = "replica",   // это если нужно из-под юзера
                //Password = theSecureString,
                //Domain = "SAN",
                //LoadUserProfile = true, //
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                FileName = "cmd.exe",
                Arguments = "/c \"" + strCmdText + "\"",
            };
            string sMessage = "";
            string sLogger = "<br>------------------- BEGIN -------------------<br>";
            (new helpers.Logger()).WriteNotice("start [process=" + cProcessStartInfo.FileName + "][args=" + cProcessStartInfo.Arguments + "]");
            Process cProcess = Process.Start(cProcessStartInfo);
            cProcess.PriorityClass = ProcessPriorityClass.Normal;
            //string sErrorMessage = "";
            cProcess.OutputDataReceived += (sender, args) => sMessage += (args.Data.IsNullOrEmpty() ? "" : ConvertCp866Cp1251(args.Data) + "<br>");  // засирает поток и в итоге сильно замедляет работу
            cProcess.ErrorDataReceived += (sender, args) => sMessage += (args.Data.IsNullOrEmpty() ? "" : ConvertCp866Cp1251(args.Data) + "<br>");// cLogger.WriteNotice("error<br>", args.Data);
            cProcess.BeginErrorReadLine();
            //string sTMP = cProcess.StandardOutput.ReadToEnd();
            cProcess.BeginOutputReadLine();
            cProcess.WaitForExit();
            sLogger += sMessage + "<br>------------------- END -------------------";
            (new helpers.Logger()).WriteNotice(sLogger);
        }
        static public void KillProcess(Process p, string sOwner)
        {
            System.Diagnostics.Process[] aExplorers = System.Diagnostics.Process.GetProcessesByName("explorer");
            ingenie.initiator.Service.ProcessOwner cProcessOwner = null;
            string strCmdText = "taskkill /PID " + p.Id;
            string sArguments = "/c \"" + strCmdText + "\"";
            foreach (System.Diagnostics.Process cExplorer in aExplorers)
            {
                cProcessOwner = ingenie.initiator.Service.ProcessTarget.GetProcessOwner(cExplorer.Id);
                (new helpers.Logger()).WriteDebug2(cExplorer.Id + ":" + cProcessOwner.sUsername);
                if (sOwner == cProcessOwner.sUsername)
                {
                    (new helpers.Logger()).WriteNotice("запуск cmd.exe " + sArguments);
                    int nID = ProcessAsUser.Launch("\"cmd.exe\" " + sArguments, cExplorer.Id, true);
                    break;
                }
            }
        }
        static public string ConvertCp866Cp1251(string line)
        {
            //string line = "¸ą¤®åą ­Øā«ģ";
            if (line == null || line.Length == 0)
                return "";
            Encoding w1251 = Encoding.GetEncoding("windows-1251");
            Encoding cp866 = Encoding.GetEncoding("CP866");
            return cp866.GetString(w1251.GetBytes(line));
        }
    }

}
