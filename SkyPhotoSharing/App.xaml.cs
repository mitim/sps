using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Threading;

namespace SkyPhotoSharing
{
    /// <summary>
    /// App.xaml の相互作用ロジック
    /// </summary>
    public partial class App : Application
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public const string APPNAME = "SkyPhotoSharing";

        private System.Threading.Mutex mutex
            = new System.Threading.Mutex(false, APPNAME);


        private void Application_Startup(object sender, StartupEventArgs e)
        {
            AvoidMultiActivate();
            WaitSkype();
            System.Windows.Forms.Application.EnableVisualStyles();
            this.DispatcherUnhandledException +=
                new DispatcherUnhandledExceptionEventHandler(HandleUnhandledUIException);
            AppDomain.CurrentDomain.UnhandledException +=
                new UnhandledExceptionEventHandler(HandleUnhandledThreadException);
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            if (mutex == null) return;
            mutex.ReleaseMutex();
            mutex.Close();
        }

        private void HandleUnhandledUIException(object sender,DispatcherUnhandledExceptionEventArgs e)
        {
            e.Handled = true;
            Exception ex = e.Exception as Exception;
            log.Error(ex.Message, ex);
            ShowToUserMessage(ex);
        }

        private void HandleUnhandledThreadException(object sende, UnhandledExceptionEventArgs e)
        {
            Exception ex = e.ExceptionObject as Exception;
            log.Error(ex.Message, ex);
            ShowToUserMessage(ex);
        }

        private void ShowToUserMessage(Exception e)
        {
            if (e is FileTransactionException)
            {
                var b = MessageBox.Show(e.Message,  e.GetType().ToString());
            }
        }

        private void AvoidMultiActivate()
        {
            if (mutex.WaitOne(0, false) == true) return;
            log.Debug("Multiple boot detected.");
            mutex.Close();
            mutex = null;
            Shutdown();
        }

        private void WaitSkype()
        {
            var r = SkypeConnection.Instance.WaitSkypeRunning();
            if (r == true) return; 
            log.Error("Can't connect to Skype.");
            MessageBox.Show(SkyPhotoSharing.Properties.Resources.ERROR_SKYPE_DISABLE);
            Shutdown();
        }
    }
}
