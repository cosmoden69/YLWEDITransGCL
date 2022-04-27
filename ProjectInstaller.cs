using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.ServiceProcess;
using System.Linq;


namespace YLWEDITransGCL
{
    [RunInstaller(true)]
    public partial class ProjectInstaller : Installer
    {
        public ProjectInstaller()
        {
            InitializeComponent();
        }

        private void serviceInstaller1_AfterInstall(object sender, InstallEventArgs e)
        {
            //var service = new ServiceController(serviceInstaller1.ServiceName);
            //if (service.Status != ServiceControllerStatus.Running)
            //{
            //    service.Start();
            //}
        }
    }
}
