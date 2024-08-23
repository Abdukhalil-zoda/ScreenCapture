using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.IO;
using System.Linq;
using System.Reflection;
using System.ServiceProcess;
using System.Threading.Tasks;

namespace ScreenCapture
{
    [RunInstaller(true)]
    public partial class ProjectInstaller : Installer
    {
        private ServiceProcessInstaller processInstaller;
        private ServiceInstaller serviceInstaller;

        public ProjectInstaller()
        {
            InitializeComponent();

            string sourceDir = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "app");
            string destinationDir = @"C:\Screenshots\app";

            if (Directory.Exists(sourceDir))
            {
                CopyDirectory(sourceDir, destinationDir);
            }
        }

        void CopyDirectory(string srcDir, string dstDir)
        {
            Directory.CreateDirectory(dstDir);

            foreach (string file in Directory.GetFiles(srcDir))
            {
                string destFileName = Path.Combine(dstDir, Path.GetFileName(file));
                File.Copy(file, destFileName, true);
            }

            foreach (string directory in Directory.GetDirectories(srcDir))
            {
                string dstDirName = Path.Combine(dstDir, Path.GetFileName(directory));
                CopyDirectory(directory, dstDirName);
            }
        }

    }
}
