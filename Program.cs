using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;

namespace TruncateFile
{
    static class Program
    {
        const string InstallExplorerExtensionSwitch = "-i";
        const string UninstallExplorerExtensionSwitch = "-u";
        const string WorkSwitch = "-w";
        const string RegPath = @"*\shell\Truncate\command";
        const string RegDeletePath = @"*\shell\Truncate";

        [STAThread]
        static void Main()
        {
            var cmdline = Environment.GetCommandLineArgs();
            if (cmdline.Length == 1)
            {
                MessageBox.Show("Please provide arguments", "Error");
                return;
            }
            var executablePath = cmdline[0];
            var commandSwitch = cmdline[1];

            switch (commandSwitch){
                case InstallExplorerExtensionSwitch:{
                    InstallExplorerExtension(executablePath);
                    return;
                } break;
                case UninstallExplorerExtensionSwitch:{
                    UninstallExplorerExtension();
                } break;
                case WorkSwitch: {
                    foreach(var fileName in cmdline.Skip(2)){
                        start: FileStream fs = null;
                        try {
                            fs = new FileStream(fileName, FileMode.Truncate, FileAccess.Write, FileShare.None);
                        } catch(Exception e)
                        {
                            var result = MessageBox.Show(e.ToString(), String.Format("Failed to delete '{0}'", Path.GetFileName(fileName)), MessageBoxButtons.AbortRetryIgnore);
                            if (result == DialogResult.Abort) return;
                            if (result == DialogResult.Retry) goto start;
                            if (result == DialogResult.Ignore) continue;
                            
                        } finally {
                                if (fs != null) fs.Dispose();     
                        }
                    }
                } break;
                default:{
                    MessageBox.Show(String.Format("Unknown switch '{0}'. Use '-i' to install Explorer extension, '-u' to uninsntall the extension and '-w' to list files to truncate to.", commandSwitch), "Error");
                    return;
                }break;
            }
        }

        private static void RunElevated(string executable, string arguments)
        {
            //props to Victor Hurdugaci : http://victorhurdugaci.com/using-uac-with-c-part-1/
            var processInfo = new ProcessStartInfo()
                {
                    FileName = executable,
                    Arguments = arguments,
                    Verb = "runas"
                };
            try
            {
                Process.Start(processInfo);
            }
            catch
            {
                //don't care
            }
            
        }

        private static void UninstallExplorerExtension()
        {
            //http://stackoverflow.com/questions/2959419/registry-in-net-deletesubkeytree-says-the-subkey-does-not-exists-but-hey-it
            //so
            var pricipal = new WindowsPrincipal(WindowsIdentity.GetCurrent());
            if (pricipal.IsInRole(WindowsBuiltInRole.Administrator))
            {
                Registry.ClassesRoot.DeleteSubKeyTree(RegDeletePath);
            }
            else
            {
                RunElevated(Application.ExecutablePath, "-u");
            }
        }

        private static void InstallExplorerExtension(string executablePath)
        {
            try
            {
                using (var key = Registry.ClassesRoot.CreateSubKey(RegPath,RegistryKeyPermissionCheck.ReadWriteSubTree))
                {
                    key.SetValue(null, String.Format("\"{0}\" -w \"%1\"", executablePath));
                }
            }
            catch (UnauthorizedAccessException)
            {
                RunElevated(Application.ExecutablePath, "-i");
            }
            
        }
    }
}
