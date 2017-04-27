using System;
using System.Collections.Generic;
using System.Text;

namespace JenkinsTrayTracker.Utils.BackgroundProcessing
{
    class ErrorHolder
    {
        private Exception error;

        public Exception Error
        {
            get { return error; }
            set { error = value; }
        }
    }
}
