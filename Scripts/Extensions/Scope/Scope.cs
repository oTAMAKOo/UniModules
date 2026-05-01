
using System;

namespace Extensions
{
    public abstract class Scope : IDisposable
    {
        private bool disposed = false;

        protected Scope() {}

        ~Scope()
        {
            Dispose();
        }

        public void Dispose()
        {
            if (disposed){ return; }

            disposed = true;

            CloseScope();

            GC.SuppressFinalize(this);
        }

        protected abstract void CloseScope();
    }
}
