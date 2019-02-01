using System;
using System.IO;
using System.Threading.Tasks;

namespace TwitterFavImageSave.Common
{
    /// <summary>
    /// ストリームのWrapperクラス
    /// </summary>
    public class WrappingStream : Stream
    {
        Stream m_streamBase;

        public WrappingStream(Stream streamBase)
        {
            m_streamBase = streamBase ?? throw new ArgumentNullException("streamBase");
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, System.Threading.CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            return m_streamBase.ReadAsync(buffer, offset, count, cancellationToken);
        }

        public new Task<int> ReadAsync(byte[] buffer, int offset, int count)
        {
            ThrowIfDisposed();
            return m_streamBase.ReadAsync(buffer, offset, count);
        }

        public override bool CanRead
        {
            get { return m_streamBase == null ? false : m_streamBase.CanRead; }
        }

        public override bool CanSeek
        {
            get { return m_streamBase == null ? false : m_streamBase.CanSeek; }
        }

        public override bool CanWrite
        {
            get { return m_streamBase == null ? false : m_streamBase.CanWrite; }
        }

        public override long Length
        {
            get { ThrowIfDisposed(); return m_streamBase.Length; }
        }

        public override long Position
        {
            get { ThrowIfDisposed(); return m_streamBase.Position; }
            set { ThrowIfDisposed(); m_streamBase.Position = value; }
        }

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            ThrowIfDisposed();
            return m_streamBase.BeginRead(buffer, offset, count, callback, state);
        }

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            ThrowIfDisposed();
            return m_streamBase.BeginWrite(buffer, offset, count, callback, state);
        }

        public override int EndRead(IAsyncResult asyncResult)
        {
            ThrowIfDisposed();
            return m_streamBase.EndRead(asyncResult);
        }

        public override void EndWrite(IAsyncResult asyncResult)
        {
            ThrowIfDisposed();
            m_streamBase.EndWrite(asyncResult);
        }

        public override void Flush()
        {
            ThrowIfDisposed();
            m_streamBase.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            ThrowIfDisposed();
            return m_streamBase.Read(buffer, offset, count);
        }

        public override int ReadByte()
        {
            ThrowIfDisposed();
            return m_streamBase.ReadByte();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            ThrowIfDisposed();
            return m_streamBase.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            ThrowIfDisposed();
            m_streamBase.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            ThrowIfDisposed();
            m_streamBase.Write(buffer, offset, count);
        }

        public override void WriteByte(byte value)
        {
            ThrowIfDisposed();
            m_streamBase.WriteByte(value);
        }

        protected Stream WrappedStream
        {
            get { return m_streamBase; }
        }

        protected override void Dispose(bool disposing)
        {
            // doesn't close the base stream, but just prevents access to it through this WrappingStream

            if (disposing)
            {
                m_streamBase.Dispose();
                m_streamBase = null;
            }

            base.Dispose(disposing);
        }

        private void ThrowIfDisposed()
        {
            // throws an ObjectDisposedException if this object has been disposed
            if (m_streamBase == null)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }
    }
}
