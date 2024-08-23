using Microsoft.Win32.SafeHandles;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.ServiceProcess;
using System.Text;

namespace ScreenCapture
{
    public partial class Service1 : ServiceBase
    {
        private string logFilePath = @"C:\Screenshots\ServiceLogFile.txt";

        public Service1()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            WriteLog("Service started.");
            WatchLogins();
        }

        protected override void OnStop()
        {
            WriteLog("Service stopped.");
        }

        private void WatchLogins()
        {
            WriteLog("Watching for login events...");
            EventLog securityLog = new EventLog("Security");
            securityLog.EntryWritten += OnEntryWritten;
            securityLog.EnableRaisingEvents = true;
        }

        private void OnEntryWritten(object sender, EntryWrittenEventArgs e)
        {
            WriteLog($"Entry written. ID: {e.Entry.InstanceId}");
            if (e.Entry.InstanceId == 4624) // Event ID for successful logon
            {
                string username = ExtractUsernameFromEvent(e.Entry.Message);
                WriteLog($"User logged in: {username}");

                // Start programs under the logged-in user’s context
                StartProgramsForUser(username);
            }
        }

        private void StartProgramsForUser(string username)
        {
            int sessionId = GetActiveSessionId();
            if (sessionId != -1)
            {
                if (!IsProgramAlreadyRunning("Test.exe", sessionId))
                {
                    RunProgramForLoggedInUser(@"C:\Screenshots\app\Test.exe", sessionId);
                }
                else
                {
                    WriteLog($"Program 'Test.exe' is already running for session {sessionId}, skipping execution.");
                }
            }
            else
            {
                WriteLog("No active session found.");
            }
        }

        private int GetActiveSessionId()
        {
            return WTSGetActiveConsoleSessionId();
        }

        private bool IsProgramAlreadyRunning(string programName, int sessionId)
        {
            try
            {
                var processes = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(programName))
                                       .Where(p => p.SessionId == sessionId);

                return processes.Any();
            }
            catch (Exception ex)
            {
                WriteLog($"Error checking if program is running: {ex.Message}");
                return false;
            }
        }

        private void RunProgramForLoggedInUser(string programPath, int sessionId)
        {
            IntPtr userToken = IntPtr.Zero;
            try
            {
                if (WTSQueryUserToken(sessionId, out userToken))
                {
                    IntPtr duplicatedToken = IntPtr.Zero;

                    // Duplicate the token so we can use it to start a process
                    if (DuplicateTokenEx(userToken, 0x10000000, IntPtr.Zero, 2, 1, out duplicatedToken))
                    {
                        STARTUPINFO startupInfo = new STARTUPINFO();
                        PROCESS_INFORMATION processInfo = new PROCESS_INFORMATION();

                        startupInfo.cb = Marshal.SizeOf(startupInfo);

                        // Use CreateProcessAsUser to start the process under the user's context
                        bool result = CreateProcessAsUser(
                            duplicatedToken,
                            null,
                            programPath,
                            IntPtr.Zero,
                            IntPtr.Zero,
                            false,
                            0,
                            IntPtr.Zero,
                            null,
                            ref startupInfo,
                            out processInfo
                        );

                        if (result)
                        {
                            WriteLog($"Started program: {programPath} for session {sessionId}");
                        }
                        else
                        {
                            int errorCode = Marshal.GetLastWin32Error();
                            WriteLog($"Failed to start program: {programPath}. Error code: {errorCode}");
                        }

                        // Close process and thread handles
                        CloseHandle(processInfo.hProcess);
                        CloseHandle(processInfo.hThread);
                    }
                    else
                    {
                        WriteLog("Failed to duplicate user token.");
                    }

                    CloseHandle(duplicatedToken);
                }
                else
                {
                    WriteLog("Failed to retrieve user token.");
                }
            }
            catch (Exception ex)
            {
                WriteLog($"Error starting program: {ex.Message}");
            }
            finally
            {
                if (userToken != IntPtr.Zero)
                {
                    CloseHandle(userToken);
                }
            }
        }

        private string ExtractUsernameFromEvent(string eventMessage)
        {
            var lines = eventMessage.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                if (line.Contains("Account Name:"))
                {
                    return line.Split(new[] { ':' }, 2)[1].Trim();
                }
            }
            return string.Empty;
        }

        private void WriteLog(string message)
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(logFilePath, true))
                {
                    writer.WriteLine($"{DateTime.Now}: {message}");
                }
            }
            catch (Exception ex)
            {
                // Handle any exceptions related to file writing
            }
        }

        // Importing necessary functions for impersonation and process creation
        [DllImport("wtsapi32.dll", SetLastError = true)]
        private static extern bool WTSQueryUserToken(int SessionId, out IntPtr phToken);

        [DllImport("kernel32.dll")]
        private extern static bool CloseHandle(IntPtr handle);

        [DllImport("kernel32.dll")]
        private static extern int WTSGetActiveConsoleSessionId();

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private extern static bool DuplicateTokenEx(
            IntPtr hExistingToken,
            uint dwDesiredAccess,
            IntPtr lpTokenAttributes,
            int ImpersonationLevel,
            int TokenType,
            out IntPtr phNewToken);

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private extern static bool CreateProcessAsUser(
            IntPtr hToken,
            string lpApplicationName,
            string lpCommandLine,
            IntPtr lpProcessAttributes,
            IntPtr lpThreadAttributes,
            bool bInheritHandles,
            uint dwCreationFlags,
            IntPtr lpEnvironment,
            string lpCurrentDirectory,
            ref STARTUPINFO lpStartupInfo,
            out PROCESS_INFORMATION lpProcessInformation);

        [StructLayout(LayoutKind.Sequential)]
        private struct STARTUPINFO
        {
            public int cb;
            public string lpReserved;
            public string lpDesktop;
            public string lpTitle;
            public int dwX;
            public int dwY;
            public int dwXSize;
            public int dwYSize;
            public int dwXCountChars;
            public int dwYCountChars;
            public int dwFillAttribute;
            public int dwFlags;
            public short wShowWindow;
            public short cbReserved2;
            public IntPtr lpReserved2;
            public IntPtr hStdInput;
            public IntPtr hStdOutput;
            public IntPtr hStdError;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct PROCESS_INFORMATION
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public int dwProcessId;
            public int dwThreadId;
        }
    }
}
