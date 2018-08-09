using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace UnitySocketClient
{
    public abstract class Socket : IDisposable
    {
        public event EventHandler Connected;
        public event EventHandler Closed;
        public event EventHandler Error;
        public event EventHandler DataReceived;
        public event EventHandler MessageReceived;

        void OnEvent(EventHandler h, EventArgs e)
        {
            var handler = h;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        protected void OnConnected(EventArgs e) { OnEvent(Connected, e); }
        protected void OnClosed(EventArgs e) { OnEvent(Closed, e); }
        protected void OnError(EventArgs e) { OnEvent(Error, e); }
        protected void OnDataReceived(EventArgs e) { OnEvent(DataReceived, e); }
        protected void OnMessageReceived(EventArgs e) { OnEvent(MessageReceived, e); }

        public abstract void Connect();
        public abstract void Close();
        public abstract void SendData(byte[] data);
        public abstract void SendMessage(string message);

        protected virtual void ReleaseSocket() { }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~Socket() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}

