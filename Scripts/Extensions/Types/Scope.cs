
using System;

namespace Extensions
{
    public abstract class Scope : IDisposable
    {
        protected Scope() {}

        ~Scope()
        {
            Dispose();
        }

        public void Dispose()
        {
            CloseScope();

            GC.SuppressFinalize(this);
        }

        protected abstract void CloseScope();
    }
}