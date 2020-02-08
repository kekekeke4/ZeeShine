using System;
using System.Collections.Generic;
using System.Text;

namespace ZeeShine.ComponentModel
{
    public class DisposeableObject : IDisposable
    {
        protected bool disposed;

        ~DisposeableObject()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            AssertDisposed();
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            disposed = true;
        }

        protected virtual void AssertDisposed()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }
    }
}
