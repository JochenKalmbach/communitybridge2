﻿using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Reflection;
using System.Threading;
using CommunityBridge2.LiveConnect.Public;

namespace CommunityBridge2.LiveConnect.Internal
{

  /// <summary>
  /// Defines a provider for progress updates.
  /// </summary>
  /// <typeparam name="T">The type of progress update value.This type parameter is contravariant. That is, you can use either the type you specified or any type that is less derived. For more information about covariance and contravariance, see Covariance and Contravariance in Generics.</typeparam>
  public interface IProgress<in T>
  {
    /// <summary>
    /// Reports a progress update.
    /// </summary>
    /// <param name="value">The value of the updated progress.</param>
    void Report(T value);
  }

  /// <summary>
    /// The Platform class implements platform dependent methods
    /// </summary>
    internal static class Platform
    {
        public static string GetLibraryHeaderValue()
        {
            string assemblyName = Assembly.GetExecutingAssembly().FullName;
            string[] versions = assemblyName.Split(',')[1].Split('=')[1].Split('.');

            return string.Format(
                CultureInfo.InvariantCulture,
                "Windows/.Net4_{0}.{1}Client",
                versions[0],
                versions[1]);
        }

        public static void SetDownloadRequestOption(HttpWebRequest request)
        {
            // No-op
        }

        public static void RegisterForCancel(object state, Action callback)
        {
            if (state is CancellationToken)
            {
                CancellationToken ct = (CancellationToken)state;
                ct.Register(callback);
            }
        }

        public static void StreamReadAsync(
            Stream stream,
            byte[] buffer,
            int offset,
            int count,
            SynchronizationContextWrapper syncContext,
            Action<int, Exception> callback)
        {
            Exception error = null;
            int length = 0;
            try
            {
                stream.BeginRead(buffer, offset, count,
                    delegate(IAsyncResult ar)
                    {
                        try
                        {
                            length = stream.EndRead(ar);
                        }
                        catch (Exception e)
                        {
                            error = e;
                        }

                        callback(length, error);
                    },
                    null);
            }
            catch (Exception e)
            {
                error = e;
                callback(length, error);
            }
        }

        public static void StreamWriteAsync(
            Stream stream,
            byte[] buffer,
            int offset,
            int count,
            SynchronizationContextWrapper syncContext,
            Action<int, Exception> callback)
        {
            Exception error = null;
            int length = 0;

            try
            {
                stream.BeginWrite(buffer, offset, count,
                    delegate(IAsyncResult ar)
                    {
                        try
                        {
                            stream.EndWrite(ar);
                            stream.Flush();
                            length = count;
                        }
                        catch (Exception e)
                        {
                            error = e;
                        }

                        callback(length, error);
                    },
                    null);
            }
            catch (Exception e)
            {
                error = e;
                callback(length, error);
            }
        } 

        public static void ReportProgress(object handler, LiveOperationProgress progress)
        {
            var progressHandler = handler as IProgress<LiveOperationProgress>;
            if (progressHandler != null)
            {
                progressHandler.Report(progress);
            }
        }
    }
}
