using System.ServiceProcess;

namespace ScreenCapture
{
    partial class ProjectInstaller
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {

            processInstaller = new ServiceProcessInstaller();
            serviceInstaller = new ServiceInstaller();
            // Run service as System [task 1 done]
            processInstaller.Account = ServiceAccount.LocalSystem;

            serviceInstaller.ServiceName = "ScreenCapture";
            serviceInstaller.Description = "Service for screen capturing";
            serviceInstaller.DisplayName = "Screen Capture";

            serviceInstaller.StartType = ServiceStartMode.Automatic;

            Installers.Add(serviceInstaller);
            Installers.Add(processInstaller);

        }

        #endregion

        private System.ServiceProcess.ServiceProcessInstaller serviceProcessInstaller1;
        private System.ServiceProcess.ServiceInstaller serviceInstaller1;
    }
}