using System;
using System.IO;
using System.IO.Ports;
using System.Diagnostics;
using System.Collections.Generic;

namespace FJR.Sms {
    public class PhoneClient : IDisposable {
        private Stream _connectedStream;
        private bool _disposed;

        public PhoneClient(Stream connectedStream) {
            _connectedStream = connectedStream;

            InitPhone();
        }

        public PhoneClient(string serialPortName) {
            try {
                SerialPort serialPort = new SerialPort(serialPortName, 19600, Parity.Even, 8, StopBits.One);
                serialPort.Handshake = Handshake.RequestToSendXOnXOff;
                serialPort.Encoding = System.Text.Encoding.ASCII;
                serialPort.ReadTimeout = serialPort.WriteTimeout = 10000;
                serialPort.Open();
                if (!serialPort.IsOpen) {
                    throw new Exception();
                }
                _connectedStream = serialPort.BaseStream;

                InitPhone();
            } catch (Exception) {
                throw new ConnectionFailedException("Failed to start communications with phone");
            }
        }

        public List<SmsDeliverMessage> List(ListType type) {
            List<SmsDeliverMessage> result = new List<SmsDeliverMessage>();

            // set format to pdu
            Debug.WriteLine("Asking for PDU format");
            WriteCommandExpectResponse("AT+CMGF=0", "\rOK\r");

            // list storages
            Debug.WriteLine("Asking for storage enumeration");
            string response = WriteCommandExpectResponse("AT+CPMS?", "\rOK\r");
            string[] data = response.Split('\r');
            if (data[1].Substring(0, 7).ToUpper() != "+CPMS: ")
                throw new UnexpectedResponseException("Phone message storages could not be enumerated");

            // walk storages
            string[] storages = data[1].Substring(7).Split(',');
            for (int x = 0; x < storages.Length; x += 3) {
                Debug.WriteLine("Memory " + storages[x] + ": " + storages[x + 1] + " used, " + storages[x + 2] + " total");

                // select storage
                WriteCommandExpectResponse("AT+CPMS=" + storages[x], "\rOK\r");

                // list messages in storage
                response = WriteCommandExpectResponse("AT+CMGL=" + (int)type, "\rOK\r");
                data = response.Split('\r');
                for (int y = 2; y < data.Length - 2; y = y + 2) {
                    try {
                        result.Add(new SmsDeliverMessage(data[y], new MessageLocation(storages[x], int.Parse(data[y - 1].Substring(6, data[y - 1].IndexOf(",", 6) - 6)))));
                    } catch (Exception ex) {
                        Debug.WriteLine("Failed to read message: " + ex.Message + ", " + ex.StackTrace);
                    }
                }
            }

            MergeMultipartMessages(result);
            return result;
        }

        public bool Send(SmsSubmitMessage message) {
            // set format to pdu
            Debug.WriteLine("Asking for PDU format");
            WriteCommandExpectResponse("AT+CMGF=0", "\rOK\r");

            // select message service
            Debug.WriteLine("Selecting message service");
            WriteCommandExpectResponse("AT+CSMS=0", "\rOK\r");

            // send message
            Debug.WriteLine("Sending SMS");
            WriteCommandExpectResponse("AT+CMGS=" + ((message.Pdu.Length - 2) / 2), "\r> ");
            WriteCommandExpectResponse(message.Pdu + "\x1A", "\rOK\r");

            return true;
        }

        public void Delete(SmsDeliverMessage message) {
            for (int x = 0; x < message.MessageLocation.Count; x++) {
                // select storage
                WriteCommandExpectResponse("AT+CPMS=" + message.MessageLocation[x].StorageName, "\rOK\r");
            
                // delete message
                WriteCommandExpectResponse("AT+CMGD=" + message.MessageLocation[x].MessageIndex, "\rOK\r");
            }
        }

        private void InitPhone() {
            // say hi, and disable echoing if neccessary
            string response = WriteCommandExpectResponse("ATZ", "ATZ\r\rOK\r\0\rOK\r");
            if (response == "ATZ\r\rOK\r") {
                // disable echoing
                Debug.WriteLine("Asking to disable echoing");
                WriteCommandExpectResponse("ATE=0", "ATE=0\r\rOK\r\0\rOK\r");
                Debug.WriteLine("Echoing disabled");
            } else if (response == "\rOK\r") {
                Debug.WriteLine("Echoing seems to be off");
            } else {
                throw new UnexpectedResponseException("Unexpected response after sending AT");
            }

            // turn on DCD
            Debug.WriteLine("Turning on DCD");
            WriteCommandExpectResponse("AT&C0", "\rOK\r");

            // ask for name
            response = WriteCommandExpectResponse("ATI", "\rOK\r");
            string[] arrID = response.Split('\r');
            Debug.WriteLine("Nice to meet you \"" + arrID[1] + "\"");
        }

        private void MergeMultipartMessages(List<SmsDeliverMessage> messages) {
            for (int x = 0; x < messages.Count; x++) {
                if ((messages[x].HasMoreParts) && (messages[x].PartIndex == 1)) {
                    for (int y = 2; y <= messages[x].PartCount; y++) {
                        for (int z = 0; z < messages.Count; z++) {
                            if ((messages[z].HasMoreParts) && (messages[z].PartIndex == y) && (messages[z].PartGroupId == messages[x].PartGroupId) &&
                                (messages[z].SenderAddress.PhoneNumber == messages[x].SenderAddress.PhoneNumber) && Math.Abs(((TimeSpan)(messages[z].DateReceived - messages[x].DateReceived)).TotalHours) < 24) {

                                messages[x].Text += messages[z].Text;
                                messages[x].MessageLocation.AddRange(messages[z].MessageLocation);

                                messages.RemoveAt(z);
                                if (z < x) x--;

                                if (y == messages[x].PartCount) {
                                    messages[x].HasMoreParts = false;
                                }
                                break;
                            }
                        }
                    }
                }
            }
        }

        private string WriteCommandExpectResponse(string command, string response) {
            return WriteCommandExpectResponse(command, response.Split('\0'));
        }

        private string WriteCommandExpectResponse(string command, string[] response) {
            // write command
            Debug.WriteLine("C: " + command);
            byte[] writeBuffer = System.Text.Encoding.ASCII.GetBytes(command + "\r");
            _connectedStream.Write(writeBuffer, 0, writeBuffer.Length);

            // read response
            string result = "";
            byte[] readBuffer = new byte[512];
            int readLength;

            // read rows
            while (true) {
                readLength = _connectedStream.Read(readBuffer, 0, 512);
                if (readLength == 0) {
                    throw new UnexpectedResponseException("No response after sending " + command);
                }

                result = String.Concat(result, System.Text.Encoding.ASCII.GetString(readBuffer, 0, readLength).Replace("\n", ""));
                for (int x = 0; x < response.Length; x++) {
                    if (result.EndsWith(response[x])) {
                        Debug.WriteLine("S: " + result.Replace("\r", @"\r"));
                        return result;
                    }
                }
            }

            throw new UnexpectedResponseException("Invalid response after sending " + command + ", expected " + response + " but got " + result);
        }

        #region IDisposable Members
        // if .Dispose() is called, the destructor will be supressed
        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // if the destructor run, .Dispose() has never been called
        ~PhoneClient() {
            Dispose(false);
        }

        private void Dispose(bool disposing) {
            if (!_disposed) {
                if (disposing) {
                    _connectedStream.Dispose();
                }
                _disposed = true;
            }
        }
        #endregion
    }
}
