using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Remoting;
using System.Text;
using Hg.Net.Models;

namespace Hg.Net
{
    public class HgCommandServerClient
    {
        public List<string> Capabilities { get; private set; }
        public string HgEncoding { get; private set; }
        private readonly string _hgPath = "hg";
        private const byte HeaderLength = 5;
        private Process _cmdServer;

        public HgCommandServerClient()
        {

        }

        public HgCommandServerClient(string hgPath)
            : this()
        {
            _hgPath = hgPath;
        }

        public bool Connect(string pathToRepo)
        {
            var args = string.Format(@"serve --cmdserver pipe --cwd ""{0}"" --repository ""{0}""", pathToRepo);
            var serverInfo = new ProcessStartInfo(_hgPath, args)
            {
                RedirectStandardInput = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
                UseShellExecute = false
            };
            serverInfo.EnvironmentVariables.Add("HGENCODING", "UTF-8");

            try
            {
                _cmdServer = Process.Start(serverInfo);
                Hello();
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        public static void Init(string path, string hgPath)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException("Path cannot be empty");
            }

            if (string.IsNullOrEmpty(hgPath))
            {
                throw new ArgumentException("Mercurial path cannot be empty");
            }

            var args = string.Format("init {0}", path);

            var processInfo = new ProcessStartInfo(hgPath, args)
            {
                RedirectStandardInput = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
                UseShellExecute = false
            };
            processInfo.EnvironmentVariables.Add("HGENCODING", "UTF-8");

            var process = Process.Start(processInfo);

            if (process == null) throw new Exception("Failed to start process");

            process.WaitForExit();

            if (process.ExitCode != 0 || !string.IsNullOrEmpty(process.StandardError.ReadToEnd()))
            {
                throw new Exception("Init reposiory failed on path " + path);
            }
        }

        private void Hello()
        {
            var response = ReadResponse();

            if (response.Channel != Channel.O || !response.Messsage.Contains("capabilities") || !response.Messsage.Contains("encoding"))
            {
                throw new ServerException("Handshake failed");
            }

            var parsedMessage = response.Messsage.Split('\n')
                .Select(s => s.Split(new[] { ": " }, StringSplitOptions.RemoveEmptyEntries)).ToDictionary(t => t[0], t => t[1]);

            Capabilities = parsedMessage["capabilities"].Split(' ').ToList();
            HgEncoding = parsedMessage["encoding"];
        }

        private static int ReadBytes(Stream stream, byte[] buffer, int offset, int length)
        {
            var remaining = length;
            var read = 1;

            while (remaining > 0 && read > 0)
            {
                read = stream.Read(buffer, offset, remaining);
                offset += read;
                remaining -= read;
            }

            return length - remaining;
        }

        private int RunCommand(IEnumerable<string> command, IDictionary<Channel, Stream> outputs)
        {
            var commandBuffer = Encoding.UTF8.GetBytes("runcommand\n");
            var enumerable = command as string[] ?? command.ToArray();

            var argBuffer = enumerable.Aggregate(new List<byte>(), (b, a) =>
            {
                b.AddRange(Encoding.UTF8.GetBytes(a));
                b.Add(0);
                return b;
            }, b =>
            {
                b.RemoveAt(b.Count - 1);
                return b.ToArray();
            }).ToArray();

            var lenBuffer = BitConverter.GetBytes(argBuffer.Length);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(lenBuffer);
            }

            lock (_cmdServer)
            {
                _cmdServer.StandardInput.BaseStream.Write(commandBuffer, 0, commandBuffer.Length); //TODO: wtf? why it returns "abort: unknown command ﻿runcommand" on mac or linux
                _cmdServer.StandardInput.BaseStream.Write(lenBuffer, 0, lenBuffer.Length);
                _cmdServer.StandardInput.BaseStream.Write(argBuffer, 0, argBuffer.Length);
                _cmdServer.StandardInput.BaseStream.Flush();

                try
                {
                    while (true)
                    {
                        var response = ReadResponse();
                        if (response.Channel == Channel.R)
                            return GetMessageLength(response.Buffer, 0);

                        if (outputs != null && outputs.ContainsKey(response.Channel))
                        {
                            outputs[response.Channel].Write(response.Buffer, 0, response.Buffer.Length);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(string.Join(" ", enumerable.ToArray()));
                    Console.WriteLine(ex);
                    _cmdServer.StandardOutput.BaseStream.Flush();
                    _cmdServer.StandardError.BaseStream.Flush();
                    throw;
                }
            }
        }

        private ServerResponse ReadResponse()
        {
            var header = new byte[HeaderLength];
            int bytesRead;

            try
            {
                bytesRead = ReadBytes(_cmdServer.StandardOutput.BaseStream, header, 0, HeaderLength);
            }
            catch (Exception ex)
            {
                throw new ServerException("Error when try to read command from server", ex);
            }

            if (bytesRead != HeaderLength)
            {
                throw new ServerException(string.Format("Invalid response header length of {0} bytes", bytesRead));
            }

            var channel = (Channel)header[0];
            var messageLength = GetMessageLength(header, 1);

            if (channel == Channel.I || channel == Channel.L)
                return new ServerResponse(channel, messageLength.ToString());

            var messageBuffer = new byte[messageLength];

            try
            {
                bytesRead = ReadBytes(_cmdServer.StandardOutput.BaseStream, messageBuffer, 0, messageLength);
            }
            catch (Exception ex)
            {
                throw new ServerException("Error when try to read command from server", ex);
            }

            if (bytesRead != messageLength)
            {
                throw new ServerException(string.Format("Error when try to read command from server: Expected {0} bytes, read {1}", messageLength, bytesRead));
            }

            var message = new ServerResponse((Channel)header[0], messageBuffer);
            return message;
        }

        public CommandResponse ExecuteCommand(IEnumerable<string> command)
        {
            var output = new MemoryStream();
            var error = new MemoryStream();
            var outputs = new Dictionary<Channel, Stream>() {
                { Channel.O, output },
                { Channel.E, error },
            };

            var result = RunCommand(command, outputs);
            return new CommandResponse(result, Encoding.UTF8.GetString(output.GetBuffer(), 0, (int)output.Length),
                Encoding.UTF8.GetString(error.GetBuffer(), 0, (int)error.Length));
        }

        private static int GetMessageLength(byte[] buffer, int offset)
        {
            return IPAddress.NetworkToHostOrder(BitConverter.ToInt32(buffer, offset));
        }
    }
}

