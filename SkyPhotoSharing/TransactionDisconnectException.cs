using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkyPhotoSharing
{
    class TransactionDisconnectException : FileTransactionException
    {
        public TransactionDisconnectException(Enlister en) :
            base(String.Format(Properties.Resources.ERROR_FILE_TRANSFER_DISCONNECT, en.Name))
        {
        }
    }
}
