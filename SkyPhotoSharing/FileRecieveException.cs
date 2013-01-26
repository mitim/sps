using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkyPhotoSharing
{
    class FileRecieveException : ApplicationException
    {
        public FileRecieveException(Enlister en)
           : base(String.Format(Properties.Resources.ERROR_FILE_CANT_RECIEVE, en.Name))
        {
        }
    }
}
