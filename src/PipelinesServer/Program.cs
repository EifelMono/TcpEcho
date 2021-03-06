﻿using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using System.Reflection;
using System.Threading.Tasks;

namespace TcpEcho
{
    class Program
    {
        private static bool _echo;
        private static readonly int _minimumBufferSize = 1024 * 2048;

        static async Task Main(string[] args)
        {
            _echo = args.FirstOrDefault() == "echo";

            Console.WriteLine(typeof(Program).Assembly.GetCustomAttribute<TargetFrameworkAttribute>()?.FrameworkName ?? "framework not found");
            Console.WriteLine($"MinimumBufferSize={_minimumBufferSize}");
            var listenSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            listenSocket.Bind(new IPEndPoint(IPAddress.Loopback, 8087));

            Console.WriteLine("Listening on port 8087");

            listenSocket.Listen(120);

            while (true)
            {
                var socket = await listenSocket.AcceptAsync();
                _ = ProcessLinesAsync(socket);
            }
        }

        private static async Task ProcessLinesAsync(Socket socket)
        {
            Console.WriteLine($"[{socket.RemoteEndPoint}]: connected");

            var pipe = new Pipe();
            Task writing = FillPipeAsync(socket, pipe.Writer);
            Task reading = ReadPipeAsync(socket, pipe.Reader);

            await Task.WhenAll(reading, writing);

            Console.WriteLine($"[{socket.RemoteEndPoint}]: disconnected");
        }

        private static async Task FillPipeAsync(Socket socket, PipeWriter writer)
        {


            while (true)
            {
                try
                {
                    // Request a minimum of 512 bytes from the PipeWriter
                    Memory<byte> memory = writer.GetMemory(_minimumBufferSize);

                    int bytesRead = await socket.ReceiveAsync(memory, SocketFlags.None);
                    Console.WriteLine($"received {bytesRead}");

                    if (bytesRead == 0)
                    {
                        break;
                    }

                    // Tell the PipeWriter how much was read
                    writer.Advance(bytesRead);
                }
                catch
                {
                    break;
                }

                TraceLine($"before await writer.FlushAsync()");
                // Make the data available to the PipeReader
                FlushResult result = await writer.FlushAsync();
                TraceLine($"after await writer.FlushAsync()");

                if (result.IsCompleted)
                {
                    break;
                }
            }

            // Signal to the reader that we're done writing
            writer.Complete();
        }

        private static async Task ReadPipeAsync(Socket socket, PipeReader reader)
        {
            while (true)
            {
                TraceLine($"before await reader.ReadAsync()");
                ReadResult result = await reader.ReadAsync();
                TraceLine($"after await reader.ReadAsync()");
                ReadOnlySequence<byte> buffer = result.Buffer;
                SequencePosition? position = null;

                do
                {
                    // Find the EOL
                    position = buffer.PositionOf((byte)'\n');

                    if (position != null)
                    {
                        var line = buffer.Slice(0, position.Value);
                        ProcessLine(socket, line);

                        // This is equivalent to position + 1
                        var next = buffer.GetPosition(1, position.Value);

                        // Skip what we've already processed including \n
                        buffer = buffer.Slice(next);
                    }
                }
                while (position != null);

                // We sliced the buffer until no more data could be processed
                // Tell the PipeReader how much we consumed and how much we left to process
                reader.AdvanceTo(buffer.Start, buffer.End);

                if (result.IsCompleted)
                {
                    break;
                }
            }

            reader.Complete();
        }

        private static void ProcessLine(Socket socket, in ReadOnlySequence<byte> buffer)
        {
            Console.Write($"[{socket.RemoteEndPoint}]: ");
            var length = 0;
            foreach (var segment in buffer)
            {
#if NETCOREAPP2_1
                length += Encoding.UTF8.GetString(segment.Span).Length;
#else
                length += Encoding.UTF8.GetString(segment).Length;
#endif
            }
            Console.WriteLine($"Length={length}");
        }

        static void TraceLine(string message)
        {
            // Console.WriteLine(message);
        }
    }

#if NET461
    internal static class Extensions
    {
        public static Task<int> ReceiveAsync(this Socket socket, Memory<byte> memory, SocketFlags socketFlags)
        {
            var arraySegment = GetArray(memory);
            return SocketTaskExtensions.ReceiveAsync(socket, arraySegment, socketFlags);
        }

        public static string GetString(this Encoding encoding, ReadOnlyMemory<byte> memory)
        {
            var arraySegment = GetArray(memory);
            return encoding.GetString(arraySegment.Array, arraySegment.Offset, arraySegment.Count);
        }

        private static ArraySegment<byte> GetArray(Memory<byte> memory)
        {
            return GetArray((ReadOnlyMemory<byte>)memory);
        }

        private static ArraySegment<byte> GetArray(ReadOnlyMemory<byte> memory)
        {
            if (!MemoryMarshal.TryGetArray(memory, out var result))
            {
                throw new InvalidOperationException("Buffer backed by array was expected");
            }

            return result;
        }
    }
#endif
}
