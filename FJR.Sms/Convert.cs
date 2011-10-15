using System.Text;

namespace FJR.Sms {
    internal static class Convert {
        private static char[] gsmCharset = { '@', '£', '$', '¥', 'è', 'é', 'ù', 'ì', 'ò', 'Ç', '\n', 'Ø', 'ø', '\r', 'Å', 'å', 'D', '_', 'F', 'G', 'L', 'W', 'P', 'Y', 'S', 'Q', 'X', '\0', 'Æ', 'æ', 'ß', 'É', ' ', '!', '"', '#', '¤', '%', '&', '\'', '(', ')', '*', '+', ',', '-', '.', '/', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', ':', ';', '<', '=', '>', '?', '¡', 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z', 'Ä', 'Ö', 'Ñ', 'Ü', '§', '¿', 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z', 'ä', 'ö', 'ñ', 'ü', 'à' };
        private static char[] gsmExtCharset = { ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', '\r', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', '^', ' ', ' ', ' ', ' ', ' ', ' ', '\0', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', '{', '}', ' ', ' ', ' ', ' ', ' ', '\\', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', '[', '~', ']', ' ', '|', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', '€', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ' };

        internal static void FromTypeOfAddress(byte octet, out TypeOfAddress typeOfAddress, out NumberingPlan numberingPlan) {
            typeOfAddress = TypeOfAddress.Unknown;
            numberingPlan = NumberingPlan.Unknown;
            if ((octet & 128) == 128) {
                if (!TestBit(octet, 6) && !TestBit(octet, 5) && !TestBit(octet, 4))
                    typeOfAddress = TypeOfAddress.Unknown;
                if (!TestBit(octet, 6) && !TestBit(octet, 5) && TestBit(octet, 4))
                    typeOfAddress = TypeOfAddress.International;
                if (!TestBit(octet, 6) && TestBit(octet, 5) && !TestBit(octet, 4))
                    typeOfAddress = TypeOfAddress.National;
                if (!TestBit(octet, 6) && TestBit(octet, 5) && TestBit(octet, 4))
                    typeOfAddress = TypeOfAddress.NetworkSpecific;
                if (TestBit(octet, 6) && !TestBit(octet, 5) && !TestBit(octet, 4))
                    typeOfAddress = TypeOfAddress.Subscriber;
                if (TestBit(octet, 6) && !TestBit(octet, 5) && TestBit(octet, 4))
                    typeOfAddress = TypeOfAddress.Alphanumeric;
                if (TestBit(octet, 6) && TestBit(octet, 5) && !TestBit(octet, 4))
                    typeOfAddress = TypeOfAddress.Abbreviated;
                if (TestBit(octet, 6) && TestBit(octet, 5) && TestBit(octet, 4))
                    typeOfAddress = TypeOfAddress.Abbreviated;

                if (!TestBit(octet, 3) && !TestBit(octet, 2) && !TestBit(octet, 1) && !TestBit(octet, 0))
                    numberingPlan = NumberingPlan.Unknown;
                if (!TestBit(octet, 3) && !TestBit(octet, 2) && !TestBit(octet, 1) && TestBit(octet, 0))
                    numberingPlan = NumberingPlan.ISDNOrPhone;
                if (!TestBit(octet, 3) && !TestBit(octet, 2) && TestBit(octet, 1) && TestBit(octet, 0))
                    numberingPlan = NumberingPlan.Data;
                if (!TestBit(octet, 3) && TestBit(octet, 2) && !TestBit(octet, 1) && !TestBit(octet, 0))
                    numberingPlan = NumberingPlan.Telex;
                if (TestBit(octet, 3) && !TestBit(octet, 2) && !TestBit(octet, 1) && !TestBit(octet, 0))
                    numberingPlan = NumberingPlan.National;
                if (TestBit(octet, 3) && !TestBit(octet, 2) && !TestBit(octet, 1) && TestBit(octet, 0))
                    numberingPlan = NumberingPlan.Private;
                if (TestBit(octet, 3) && !TestBit(octet, 2) && TestBit(octet, 1) && !TestBit(octet, 0))
                    numberingPlan = NumberingPlan.ERMES;
                if (TestBit(octet, 3) && TestBit(octet, 2) && TestBit(octet, 1) && TestBit(octet, 0))
                    numberingPlan = NumberingPlan.Reserved;
            }
        }

        internal static string FromDecimalSemi(string data) {
            string result = "";
            for (int x = 0; x < data.Length; x += 2) {
                if (data[x] == 'F') {
                    result += "" + data[x + 1];
                } else {
                    result += "" + data[x + 1] + data[x];
                }
            }
            return result;
        }

        internal static string ToDecimalSemi(string data) {
            string result = "";
            if (data.Length > 0) {
                if (data.Length % 2 == 1)
                    data += "F";

                for (int x = 0; x < data.Length; x += 2) {
                    result += data[x + 1] + "" + data[x];
                }
            }
            return result;
        }

        internal static string FromSeptets(string data) {
            StringBuilder result = new StringBuilder();
            byte temp;
            bool extendedChar = false;

            // get bytes from string
            byte[] dataBytes = new byte[data.Length / 2];
            for (int x = 0, y = 0; x < data.Length; x += 2, y++) {
                dataBytes[y] = byte.Parse(data.Substring(x, 2), System.Globalization.NumberStyles.HexNumber);
            }

            // convert septets to chars
            for (int x = 0; x < dataBytes.Length; x += 7) {
                if (x + 0 < dataBytes.Length) {
                    temp = ((byte)(dataBytes[x] & 127));
                    extendedChar = FromSeptetsAppendChar(result, temp, extendedChar);
                }
                if (x + 1 < dataBytes.Length) {
                    temp = ((byte)(((dataBytes[x + 1] & 63) << 1) + (dataBytes[x] >> 7)));
                    extendedChar = FromSeptetsAppendChar(result, temp, extendedChar);
                }
                if (x + 2 < dataBytes.Length) {
                    temp = ((byte)(((dataBytes[x + 2] & 31) << 2) + (dataBytes[x + 1] >> 6)));
                    extendedChar = FromSeptetsAppendChar(result, temp, extendedChar);
                }
                if (x + 3 < dataBytes.Length) {
                    temp = ((byte)(((dataBytes[x + 3] & 15) << 3) + (dataBytes[x + 2] >> 5)));
                    extendedChar = FromSeptetsAppendChar(result, temp, extendedChar);
                }
                if (x + 4 < dataBytes.Length) {
                    temp = ((byte)(((dataBytes[x + 4] & 7) << 4) + (dataBytes[x + 3] >> 4)));
                    extendedChar = FromSeptetsAppendChar(result, temp, extendedChar);
                }
                if (x + 5 < dataBytes.Length) {
                    temp = ((byte)(((dataBytes[x + 5] & 3) << 5) + (dataBytes[x + 4] >> 3)));
                    extendedChar = FromSeptetsAppendChar(result, temp, extendedChar);
                }
                if (x + 6 < dataBytes.Length) {
                    temp = ((byte)(((dataBytes[x + 6] & 1) << 6) + (dataBytes[x + 5] >> 2)));
                    extendedChar = FromSeptetsAppendChar(result, temp, extendedChar);

                    temp = (byte)(dataBytes[x + 6] >> 1);
                    extendedChar = FromSeptetsAppendChar(result, temp, extendedChar);
                }
            }
            return result.ToString();
        }

        private static bool FromSeptetsAppendChar(StringBuilder result, byte dataByte, bool extendedChar) {
            if (extendedChar) {
                extendedChar = false;
                result.Append(gsmExtCharset[dataByte]);
            } else {
                if (gsmCharset[dataByte] == '\0') {
                    extendedChar = true;
                } else {
                    result.Append(gsmCharset[dataByte]);
                }
            }
            return extendedChar;
        }

        internal static string FromOctets(string data) {
            StringBuilder result = new StringBuilder();
            for (int x = 0, y = 0; x < data.Length; x += 2, y++) {
                result.Append((char)byte.Parse(data.Substring(x, 2), System.Globalization.NumberStyles.HexNumber));
            }
            return result.ToString();
        }

        internal static string ToOctets(string data) {
            byte[] dataBytes = System.Text.Encoding.Default.GetBytes(data);
            StringBuilder result = new StringBuilder();
            for (int x = 0; x < dataBytes.Length; x++) {
                result.AppendFormat("{0:X02}", dataBytes[x]);
            }
            return result.ToString();
        }

        internal static bool TestBit(byte octet, byte bit) {
            bit = (byte)(1 << bit);
            return (octet & bit) == bit;
        }
    }
}
