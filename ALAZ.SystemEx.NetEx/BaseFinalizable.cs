using System;

namespace EchoSocketCore
{
    /// <summary>
    /// Base class for finalizable objects.
    /// </summary>
    public abstract class BaseFinalizable : BaseDisposable
    {
        #region Destructor

        ~BaseFinalizable()
        {
            Free(false);
        }

        #endregion Destructor

        #region Methods

        #region Dispose

        /// <summary>
        /// Disposes object resources.
        /// </summary>
        public new void Dispose()
        {
            lock (this)
            {
                if (!Disposed)
                {
                    try
                    {
                        Free(true);
                    }
                    finally
                    {
                        Disposed = true;
                        GC.SuppressFinalize(this);
                    }
                }
            }
        }

        #endregion Dispose

        #endregion Methods
    }
}