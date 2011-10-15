using System;
using System.Collections.Generic;
using System.Text;

namespace FJR.Sms {
    public abstract class SmsMessage { }

    public class SmsSubmitMessage : SmsMessage {
        /// <summary>The raw message</summary>
        public string Pdu;

		/// <summary>Creates an SMS-SUBMIT message</summary>
		public SmsSubmitMessage(Address address, string message) {
			StringBuilder result = new StringBuilder();

			// length of smsc information (use the one in the phone)
			result.Append("00");

			// TP-MTI: first octet (set SMS-SUBMIT & TP-VPS=Relative)
			result.Append("01");

			// TP-MR: message reference (phone shall decide)
			result.Append("00");

			// TP-DA: length of phoneno
			result.Append(address.PhoneNumber.Length.ToString("X02"));

			// TP-DA: type of address
			result.Append((128 + (int)address.TypeOfAddress + (int)address.NumberingPlan).ToString("X02"));

			// TP-DA: phoneno
			result.Append(Convert.ToDecimalSemi(address.PhoneNumber));

			// TP-PID
			result.Append("00");

			// TP-DCS (8 bit data)
			result.Append("04");

			// TP-VP: TP-Validity-Period
			/* 
			 * Absolute format, doesn't work well:
			int intGMTOffset = (int)TimeZone.CurrentTimeZone.GetUtcOffset(dteValidTo).TotalMinutes/15;
			string strGMTOffset = (Math.Abs(intGMTOffset) < 10 ? "0" + Math.Abs(intGMTOffset) : "" + Math.Abs(intGMTOffset));
			strGMTOffset = strGMTOffset.Substring(1,1) + strGMTOffset.Substring(0,1);
			if (intGMTOffset < 0) {
				byte b = (byte)strGMTOffset[0];
				b |= 0xFF;
				strGMTOffset = (char)b + strGMTOffset.Substring(1,1);
			}
			s += ToDecimalSemi(dteValidTo.ToString("yyMMddHHmmss")) + strGMTOffset;
			*/

			// TP-UDL
			result.Append(message.Length.ToString("X02"));
			
			// TP-UD
            result.Append(Convert.ToOctets(message).ToUpper());

			Pdu = result.ToString();
		}
    }

	public class SmsDeliverMessage : SmsMessage {
        /// <summary>The raw message</summary>
        public string Pdu;

        /// <summary>The location of the message - that is - both the storage name & the message index in that storage</summary>
        public List<MessageLocation> MessageLocation = new List<MessageLocation>();
        
        /// <summary>Address (phoneno) of the short message service center that processed the message</summary>
        public Address SMSCAddress = new Address();

		/// <summary>Address (phoneno) of the sender</summary>
        public Address SenderAddress = new Address();

        /// <summary>Date & Time when the message was received by the short message service center (GMT)</summary>
        public DateTime DateReceived;

		/// <summary>The message</summary>
        public string Text;

		/// <summary>True if the message has more parts</summary>
        public bool HasMoreParts;
        
        /// <summary>A unique ID that together with the SenderAddress uniquely identifies the part group</summary>
		public byte PartGroupId;

        /// <summary>The index of the part in the PartsID group</summary>
        public byte PartIndex;
        
        /// <summary>Total number of parts</summary>
		public byte PartCount;

		/// <summary>Decodes an SMS-DELIVER message</summary>
        internal SmsDeliverMessage(string pdu, MessageLocation messageLocation) {
			this.Pdu = pdu;
            this.MessageLocation.Add(messageLocation);
			int pduOffset = 0;

            #region Short Message Service Center
            // length of the smsc information
			byte smscLength = byte.Parse(pdu.Substring(pduOffset,2), System.Globalization.NumberStyles.HexNumber);
			pduOffset += 2;

			if (smscLength > 0) {
				// type of address
                Convert.FromTypeOfAddress(byte.Parse(pdu.Substring(pduOffset, 2), System.Globalization.NumberStyles.HexNumber), out SMSCAddress.TypeOfAddress, out SMSCAddress.NumberingPlan);
				pduOffset += 2;

				// message center address
                SMSCAddress.PhoneNumber += Convert.FromDecimalSemi(pdu.Substring(pduOffset, smscLength * 2 - 2));
				pduOffset += smscLength*2-2;
            }
            #endregion

            #region First octet (message type & header info bit)
            // first octet (TP-MTI must be set to SMS-DELIVER)
			byte firstOctet = byte.Parse(pdu.Substring(pduOffset,2), System.Globalization.NumberStyles.HexNumber);
            if (Convert.TestBit(firstOctet, 1) || Convert.TestBit(firstOctet, 0)) {
				throw new DecodeException("Only SMS-DELIVER messages can be decoded!");
			}
			// TP-UDH
			bool hasMessageHeader = false;
            if (Convert.TestBit(firstOctet, 6)) {		
				hasMessageHeader = true;
			}
			pduOffset += 2;
            #endregion

            #region Sender Address
            // length of the sender address
			byte senderLength = byte.Parse(pdu.Substring(pduOffset,2), System.Globalization.NumberStyles.HexNumber);
			pduOffset += 2;

			// type of address
            Convert.FromTypeOfAddress(byte.Parse(pdu.Substring(pduOffset, 2), System.Globalization.NumberStyles.HexNumber), out SenderAddress.TypeOfAddress, out SenderAddress.NumberingPlan);
			pduOffset += 2;

			// sender address
            if (SenderAddress.TypeOfAddress == TypeOfAddress.Alphanumeric) {
                SenderAddress.PhoneNumber += Convert.FromSeptets(pdu.Substring(pduOffset, senderLength + (senderLength % 2 == 1 ? 1 : 0)));
            } else {
                SenderAddress.PhoneNumber += Convert.FromDecimalSemi(pdu.Substring(pduOffset, senderLength + (senderLength % 2 == 1 ? 1 : 0)));
            }
			pduOffset += senderLength+(senderLength % 2 == 1 ? 1 : 0);
            #endregion

            #region Metadata (compression, alphabet, class, encoding)
            // TP-PID: protocol id
			byte protocolId = byte.Parse(pdu.Substring(pduOffset, 2), System.Globalization.NumberStyles.HexNumber);
			pduOffset += 2;

			// TP-DCS: data coding scheme
			bool messageCompressed = false;
			int messageAlphabet = 0;
			int messageClass = -1;
			byte messageEncoding = byte.Parse(pdu.Substring(pduOffset, 2),System.Globalization.NumberStyles.HexNumber); 
			pduOffset += 2;
            if (!Convert.TestBit(messageEncoding, 7) && !Convert.TestBit(messageEncoding, 6)) {					// general data
                messageCompressed = (Convert.TestBit(messageEncoding, 5));
                if (Convert.TestBit(messageEncoding, 4)) {
                    if (!Convert.TestBit(messageEncoding, 1) && !Convert.TestBit(messageEncoding, 0)) {
						messageClass = 0;		// class 0
                    } else if (!Convert.TestBit(messageEncoding, 1) && Convert.TestBit(messageEncoding, 0)) {
						messageClass = 1;		// class 1 (me specific)
                    } else if (Convert.TestBit(messageEncoding, 1) && !Convert.TestBit(messageEncoding, 0)) {
						messageClass = 2;		// class 2 (sim specific)
                    } else if (Convert.TestBit(messageEncoding, 1) && Convert.TestBit(messageEncoding, 0)) {
						messageClass = 3;		// class 3 (te specific)
					}
				}
                if (!Convert.TestBit(messageEncoding, 3) && !Convert.TestBit(messageEncoding, 2)) {
					messageAlphabet = 0;		// default alphabet
                } else if (!Convert.TestBit(messageEncoding, 3) && Convert.TestBit(messageEncoding, 2)) {
					messageAlphabet = 1;		// 8 bit data
                } else if (Convert.TestBit(messageEncoding, 3) && !Convert.TestBit(messageEncoding, 2)) {
					messageAlphabet = 2;		// ucs2 (16 bit)
                } else if (Convert.TestBit(messageEncoding, 3) && Convert.TestBit(messageEncoding, 2)) {
					messageAlphabet = 0;		// reserved (default alphabet)
				}
            } else if (Convert.TestBit(messageEncoding, 7) && Convert.TestBit(messageEncoding, 6)) {
                if (Convert.TestBit(messageEncoding, 5) && Convert.TestBit(messageEncoding, 4)) {					// data coding/message class
                    if (!Convert.TestBit(messageEncoding, 2)) {
						messageAlphabet = 0;	// default alphabet
					} else {
						messageAlphabet = 1;	// 8 bit data
					}

                    if (!Convert.TestBit(messageEncoding, 1) && !Convert.TestBit(messageEncoding, 0)) {
						messageClass = 0;
                    } else if (!Convert.TestBit(messageEncoding, 1) && Convert.TestBit(messageEncoding, 0)) {
						messageClass = 1;
                    } else if (Convert.TestBit(messageEncoding, 1) && !Convert.TestBit(messageEncoding, 0)) {
						messageClass = 2;
                    } else if (Convert.TestBit(messageEncoding, 1) && Convert.TestBit(messageEncoding, 0)) {
						messageClass = 3;
					}
				} else {																// message waiting indication group
                    if (Convert.TestBit(messageEncoding, 5) && !Convert.TestBit(messageEncoding, 4)) {
						messageAlphabet = 2;
					} 
				}
            }
            #endregion

            #region Timestamp (adjusted to computers local time)
            // TP-SCTS: service center time stamp
            string messageTimestamp = Convert.FromDecimalSemi(pdu.Substring(pduOffset, 14));
			pduOffset += 14;			

			int year = byte.Parse(messageTimestamp.Substring(0, 2));
			if ((year >= 79) && (year <= 99)) {
				year += 1900;
			} else {
				year += 2000;
			}
			DateReceived = new DateTime(year, byte.Parse(messageTimestamp.Substring(2,2)), byte.Parse(messageTimestamp.Substring(4,2)), byte.Parse(messageTimestamp.Substring(6,2)), byte.Parse(messageTimestamp.Substring(8,2)), byte.Parse(messageTimestamp.Substring(10,2)));

			// calculate offset adjustments we need to do to get localtime
			int computerGmtOffset = (int)TimeZone.CurrentTimeZone.GetUtcOffset(DateTime.Now).TotalMinutes;	// local GMT offset

			string messageGmtOffset = messageTimestamp.Substring(12,2);
			int messageGmtOffsetNum = 0;
			byte b = byte.Parse(""+messageGmtOffset[1]); // already swapped
			if ((b & 0xFF) == 0xFF) {
				// negative
				b &= 0xFE;
				messageGmtOffset = "" + messageGmtOffset[0] + (char)b;
				messageGmtOffsetNum = -byte.Parse(messageGmtOffset);
			} else {
				// positive
				messageGmtOffsetNum = byte.Parse(messageGmtOffset);
			}
			messageGmtOffsetNum *= 15;																		// message GMT offset

			this.DateReceived = this.DateReceived.AddMinutes(computerGmtOffset-messageGmtOffsetNum);
            #endregion

            #region Message length, encoded message, and header info
            // TP-UDL: user data length
			int messageLength = byte.Parse(pdu.Substring(pduOffset,2), System.Globalization.NumberStyles.HexNumber);
			pduOffset += 2;

			string messageTPUD;
			if (messageAlphabet == 0) {
				messageTPUD = pdu.Substring(pduOffset, (int)Math.Ceiling(messageLength*(7.0/8.0))*2);
			} else {
				messageTPUD = pdu.Substring(pduOffset, messageLength*2);
			}

			// TP-UD: user data
			pduOffset = 0;
			int headerLength = 0;
			if (hasMessageHeader) {
				// TP-UDHL (header length)
				headerLength = byte.Parse(messageTPUD.Substring(pduOffset, 2), System.Globalization.NumberStyles.HexNumber);
				pduOffset += 2;

				// for each information element
				int headerLengthRemaining = headerLength*2;
				int headerOffset = pduOffset;
				while (headerLengthRemaining > 0) {
					byte ieElement = byte.Parse(messageTPUD.Substring(headerOffset, 2), System.Globalization.NumberStyles.HexNumber);
					headerOffset += 2;
					headerLengthRemaining -= 2;

					int ieLength = byte.Parse(messageTPUD.Substring(headerOffset, 2), System.Globalization.NumberStyles.HexNumber);
					headerOffset += 2;
					headerLengthRemaining -= 2;

					if ((ieElement == 0) && (ieLength == 3)) {
						// concatenated short message
						HasMoreParts = true;

						PartGroupId = byte.Parse(messageTPUD.Substring(headerOffset, 2), System.Globalization.NumberStyles.HexNumber);
						headerOffset += 2;
						headerLengthRemaining -= 2;

						PartCount = byte.Parse(messageTPUD.Substring(headerOffset, 2), System.Globalization.NumberStyles.HexNumber);
						headerOffset += 2;
						headerLengthRemaining -= 2;

						PartIndex = byte.Parse(messageTPUD.Substring(headerOffset, 2), System.Globalization.NumberStyles.HexNumber);
						headerOffset += 2;
						headerLengthRemaining -= 2;
					} else {
						headerOffset += ieLength*2;
						headerLengthRemaining -= ieLength*2;
					}
				}
				pduOffset = headerLength*2+2;
            }
            #endregion

            #region Message decoding
            if (messageAlphabet == 0) {
                Text = Convert.FromSeptets(messageTPUD);

                // cut sms
                if (Text.Length > messageLength) {
                    Text = Text.Substring(0, messageLength);
                }				

                // remove header
                if (hasMessageHeader) {
					headerLength++;
					Text = Text.Substring((int)Math.Ceiling(headerLength/7.0)*7);
				}
			} else {
                Text = Convert.FromOctets(messageTPUD.Substring(pduOffset, messageTPUD.Length - pduOffset));
            }
            #endregion
        }

		public override string ToString() {
			return string.Concat("SmsMessage:",
                "\nShort Message Service Center: ", this.SMSCAddress.PhoneNumber, 
                "\nDate: ", this.DateReceived,
                "\nSender: ", this.SenderAddress,
                "\nText: ", this.Text);
		}
	}
}
