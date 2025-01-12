﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Diagnostics;

using Windows.Foundation;
using Windows.Security.Authentication.Web.Core;
using Windows.Security.Authentication.Web;
using Windows.Security.Cryptography;
using Windows.Security.Credentials;
using System.IO;
using Windows.System;

namespace BedrockLauncher.UpdateProcessor
{
    public class Win10AuthenticationManager
    {
        private const int WU_ERRORS_START = 0x7ffc0200;
        private const int WU_NO_ACCOUNT = 0x7ffc0200;
        private const int WU_ERRORS_END = 0x7ffc0200;

        public struct WUAccount
        {
            public string UserName { get; set; }
            public string AccountType { get; set; }
        }

        public static List<WUAccount> CurrentAccounts { get; set; } = new List<WUAccount>();

        static Win10AuthenticationManager()
        {
            var myPath = new Uri(AppDomain.CurrentDomain.BaseDirectory).LocalPath;
            var myFolder = Path.Combine(Path.GetDirectoryName(myPath), "runtimes", "WUTokenHelper");

            var is64 = Environment.Is64BitProcess;
            var subfolder = is64 ? "\\x64\\" : "\\Win32\\";

            LoadLibrary(myFolder + subfolder + "WUTokenHelper.dll");
        }

        [DllImport("kernel32.dll")]
        private static extern IntPtr LoadLibrary(string dllToLoad);

        [DllImport("WUTokenHelper.dll", CallingConvention = CallingConvention.StdCall)] private static extern int GetWUToken(int userIndex, [MarshalAs(UnmanagedType.LPWStr)] out string token);
        [DllImport("WUTokenHelper.dll", CallingConvention = CallingConvention.StdCall)] private static extern int GetTotalWUAccounts();
        [DllImport("WUTokenHelper.dll", CallingConvention = CallingConvention.Cdecl)] [return: MarshalAs(UnmanagedType.BStr)] private static extern string GetWUAccountUserName(int userIndex);
        [DllImport("WUTokenHelper.dll", CallingConvention = CallingConvention.Cdecl)] [return: MarshalAs(UnmanagedType.BStr)] private static extern string GetWUProviderName(int userIndex);


        public static void GetWUUsers()
        {
            CurrentAccounts.Clear();
            List<WUAccount> results = new List<WUAccount>();
            int count = GetTotalWUAccounts();

            WUAccount default_account = new WUAccount();
            default_account.UserName = "Default Account";
            default_account.AccountType = "(No Authentication)";
            results.Add(default_account);

            for (int i = 0; i < count; i++)
            {
                WUAccount account = new WUAccount();
                account.UserName = GetWUAccountUserName(i);
                account.AccountType = GetWUProviderName(i);
                results.Add(account);
            }

            CurrentAccounts = results;
        }

        public static string GetWUToken(int SelectedUserIndex)
        {
            int index = SelectedUserIndex - 1;
            if (index == -1) return string.Empty;

            string token;
            int status = GetWUToken(index, out token);
            if (status >= WU_ERRORS_START && status <= WU_ERRORS_END) throw new WUTokenException(status);
            else if (status != 0) Marshal.ThrowExceptionForHR(status);
            return token;
        }

        public class WUTokenException : Exception
        {
            public WUTokenException(int exception) : base(GetExceptionText(exception))
            {
                HResult = exception;
            }
            private static String GetExceptionText(int e)
            {
                switch (e)
                {
                    case WU_NO_ACCOUNT: return "No account";
                    default: return "Unknown " + e;
                }
            }
        }

    }

}
