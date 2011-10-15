using System.Windows.Forms;
using FJR.Sms;
using System;
using System.Collections.Generic;

namespace FJR.SmsManager {
    public partial class Main : Form {
        public Main() {
            InitializeComponent();

            // add serial ports
            for (int x = 1; x < 9; x++) {
                serialPortList.Items.Add("COM" + x);
            }

            // add more serial ports
            serialPortList.Items.AddRange(System.IO.Ports.SerialPort.GetPortNames());

            // simple send example
            /*
            try {
                ProgressShow("Opening Phone...");
                using (PhoneClient phoneClient = new PhoneClient("COM3")) {
                    ProgressShow("Sending message...");
                    try {
                        phoneClient.Send(new SmsSubmitMessage(new Address("46730000000", TypeOfAddress.International, NumberingPlan.ISDNOrPhone), "ABCDEFGHIJKLMNOPQRSTUVWXYZÅÄÖ1abcdefghijklmnopqrstuvwxyzåäö2ABCDEFGHIJKLMNOPQRSTUVWXYZÅÄÖ3abcdefghijklmnopqrstuvwxyzåäö4ABCDEFGHIJKLMNOPQRSTUVWXYZÅÄÖ5abcdefghijklmnopqrstuvwxyzåäö6ABCDEFGHIJKLMNOPQRSTUVWXYZÅÄÖ7abcdefghijklmnopqrstuvwxyzåäö8ABCDEFGHIJKLMNOPQRSTUVWXYZÅÄÖ9abcdefghijklmnopqrstuvwxyzåäö0"));
                        ProgressShow("Message Sent!");
                    } catch (Exception ex) {
                        ProgressShow("Failed to list messages: " + ex.ToString());
                    }
                }
            } catch (Exception ex) {
                ProgressShow("Failed to open phone: " + ex.Message);
            }*/

        }

        private void serialPortList_SelectedIndexChanged(object sender, System.EventArgs e) {
            messageList.Items.Clear();

            try {
                ProgressShow("Opening Phone...");
                using (PhoneClient phoneClient = new PhoneClient(serialPortList.Text)) {
                    ProgressShow("Listing messages...");
                    try {
                        List<SmsDeliverMessage> messages = phoneClient.List(ListType.All);
                        foreach (SmsDeliverMessage message in messages) {
                            ListViewItem item = new ListViewItem();
                            item.Text = message.DateReceived.ToString();
                            item.SubItems.Add(message.SenderAddress.PhoneNumber);
                            item.SubItems.Add(message.Text);
                            item.Tag = message;

                            messageList.Items.Add(item);
                        }
                    } catch (Exception ex) {
                        ProgressShow("Failed to list messages: " + ex.ToString());
                    }
                }
            } catch (Exception ex) {
                ProgressShow("Failed to open phone: " + ex.Message);
            }
        }

        private void newMessageSend_Click(object sender, EventArgs e) {
            try {
                ProgressShow("Opening Phone...");
                using (PhoneClient phoneClient = new PhoneClient(serialPortList.Text)) {
                    ProgressShow("Sending message...");
                    try {
                        phoneClient.Send(new SmsSubmitMessage(new Address(newMessageTo.Text, TypeOfAddress.International, NumberingPlan.ISDNOrPhone), newMessageText.Text));
                        ProgressShow("Message Sent!");
                    } catch (Exception ex) {
                        ProgressShow("Failed to list messages: " + ex.ToString());
                    }
                }
            } catch (Exception ex) {
                ProgressShow("Failed to open phone: " + ex.Message);
            }
        }

        private void messageList_SelectedIndexChanged(object sender, EventArgs e) {
            if (messageList.SelectedItems.Count > 0) {
                SmsDeliverMessage message = messageList.SelectedItems[0].Tag as SmsDeliverMessage;
                newMessageTo.Text = message.SenderAddress.PhoneNumber;
                newMessageText.Text = message.Text;
            }
        }

        private void existingMessageDelete_Click(object sender, EventArgs e) {
            if (messageList.SelectedItems.Count > 0) {
                SmsDeliverMessage message = messageList.SelectedItems[0].Tag as SmsDeliverMessage;
                try {
                    ProgressShow("Opening Phone...");
                    using (PhoneClient phoneClient = new PhoneClient(serialPortList.Text)) {
                        phoneClient.Delete(message);
                        messageList.Items.Remove(messageList.SelectedItems[0]);
                    }
                } catch (Exception ex) {
                    ProgressShow("Failed to open phone: " + ex.Message);
                }

                // reindex messages
                serialPortList_SelectedIndexChanged(null, null);
            }
        }

        private void ProgressShow(string message) {
            progressText.Text = message;
            progress.Visible = true;
            progressText.Refresh();
            progressTimer.Enabled = false;
            progressTimer.Enabled = true;
        }

        private void progressTimer_Tick(object sender, EventArgs e) {
            progress.Visible = false;
            progressTimer.Enabled = false;
        }
    }
}
