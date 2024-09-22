using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace UniTCP {
    public static class UniTCPUtilities {
        public static byte[] BuildMessage(string data) {
            var terminator = "\r\n";
            return Encoding.GetEncoding("UTF-8").GetBytes($"{data}{terminator}");
        }

        public static Task<T> WithCancellation<T>(this Task<T> task, CancellationToken cancellationToken) {
            return task.IsCompleted // fast-path optimization
                ? task
                : task.ContinueWith(
                    completedTask => completedTask.GetAwaiter().GetResult(),
                    cancellationToken,
                    TaskContinuationOptions.ExecuteSynchronously,
                    TaskScheduler.Default);
        }

        public static async Task<TcpClient> AcceptTcpClientAsync(this TcpListener listener, CancellationToken token) {
            using (token.Register(listener.Stop)) {
                try {
                    var client = await listener.AcceptTcpClientAsync().ConfigureAwait(false);
                    return client;
                } catch (ObjectDisposedException ex) {
                    // Token was canceled - swallow the exception and return null
                    token.ThrowIfCancellationRequested();
                    throw ex;
                }
            }
        }
    }
}
