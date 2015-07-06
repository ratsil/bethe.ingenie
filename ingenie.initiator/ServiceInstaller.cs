using System;
using System.Collections;
using System.Configuration.Install;
using System.ServiceProcess;
using System.ComponentModel;

namespace ingenie.initiator
{
    [RunInstaller(true)]
    public partial class ServiceInstaller : System.Configuration.Install.Installer
    {
        private System.ServiceProcess.ServiceInstaller _cInstaller;
        private System.ServiceProcess.ServiceProcessInstaller _cProcessInstaller;

        public ServiceInstaller()
        {
            InitializeComponent();
            // Instantiate installers for process and services.
            _cInstaller = new System.ServiceProcess.ServiceInstaller();
            _cProcessInstaller = new System.ServiceProcess.ServiceProcessInstaller();

            // The services run under the system account.
            _cProcessInstaller.Account = ServiceAccount.LocalSystem;

            _cInstaller.StartType = ServiceStartMode.Automatic;
			_cInstaller.DelayedAutoStart = true;

            // ServiceName must equal those on ServiceBase derived classes.            
			_cInstaller.ServiceName = "ingenie.initiator";


            // Add installers to collection. Order is not important.
            Installers.Add(_cInstaller);
            Installers.Add(_cProcessInstaller);
        }
    }
}