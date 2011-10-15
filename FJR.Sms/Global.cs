using System;

namespace FJR.Sms {
    public class UnexpectedResponseException : Exception {
        internal UnexpectedResponseException(string message) : base(message) { }
    }

    public class DecodeException : Exception {
        internal DecodeException(string message) : base(message) { }
    }

    public class ConnectionFailedException : Exception {
        internal ConnectionFailedException(string message) : base(message) { }
    }

    public enum TypeOfAddress {
        Unknown = 0,
        International = 16,
        National = 32,
        NetworkSpecific = 48,
        Subscriber = 64,
        Alphanumeric = 80,
        Abbreviated = 96,
        Reserved = 112
    }

    public enum NumberingPlan {
        Unknown = 0,
        ISDNOrPhone = 1,
        Data = 3,
        Telex = 4,
        National = 8,
        Private = 9,
        ERMES = 10,
        Reserved = 15
    }
    
    public enum ListType {
        ReceivedUnread = 0,
        ReceivedRead = 1,
        StoredUnsent = 2,
        StoredSent = 3,
        All = 4
    }

    public class Address {
        public string PhoneNumber;
        public TypeOfAddress TypeOfAddress;
        public NumberingPlan NumberingPlan;

        internal Address() : this(string.Empty, TypeOfAddress.Unknown, NumberingPlan.Unknown) { }

        public Address(string phoneNumber, TypeOfAddress typeOfAddress, NumberingPlan numberingPlan) {
            this.PhoneNumber = phoneNumber;
            this.TypeOfAddress = typeOfAddress;
            this.NumberingPlan = numberingPlan;
        }
    }

    public class MessageLocation {
        public string StorageName;
        public int MessageIndex;

        internal MessageLocation(string storageName, int messageIndex) {
            this.StorageName = storageName;
            this.MessageIndex = messageIndex;
        }
    }
}
