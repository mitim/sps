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

        public const string APPNAME = "Sky_Photo_Sharing";

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            SkypeConnection.Instance.WaitSkypeRunning();
            System.Windows.Forms.Application.EnableVisualStyles();
            this.DispatcherUnhandledException +=
                new DispatcherUnhandledExceptionEventHandler(HandleUnhandledUIException);

            AppDomain.CurrentDomain.UnhandledException +=
               new UnhandledExceptionEventHandler(HandleUnhandledThreadException);
        }

        private void HandleUnhandledUIException(object sender,DispatcherUnhandledExceptionEventArgs e)
        {
            e.Handled = true;
            Exception ex = e.Exception as Exception;
            log.Error(ex.Message, ex);
        }

        private void HandleUnhandledThreadException(object sende, UnhandledExceptionEventArgs e)
        {
            Exception ex = e.ExceptionObject as Exception;
            log.Error(ex.Message, ex);
        }

        private void ShowToUserMessage(Exception e)
        {
            if ((e is FileRecieveException) || (e is FileSendException))
            {
                var b = MessageBox.Show(e.Message,  e.GetType().ToString());
            }
        }
    }
}
