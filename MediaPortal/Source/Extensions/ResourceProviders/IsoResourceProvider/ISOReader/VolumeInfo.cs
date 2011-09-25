using System;

namespace ISOReader
{
    public class VolumeInfo
    {
        private VolumeType _type;
        private string _standardId;
        private byte _version;
        private string _systemId;
        private string _volumeId;
        private long _volumeSpaceSize;
        private int _volumeSequenceNumber;
        private short _logicalBlockSize;
        private string _publisherId;
        private string _preparerId;
        private string _applicationId;
        private string _copyrightFileId;
        private DateTime _creationDate;
        private DateTime _modificationDate;
        private DateTime _expirationDate;

        public VolumeType Type
        {
            get { return _type; }
        }

        public string StandardId
        {
            get { return _standardId; }
        }

        public byte Version
        {
            get { return _version; }
        }

        public string SystemId
        {
            get { return _systemId; }
        }

        public string VolumeId
        {
            get { return _volumeId; }
        }

        public long VolumeSpaceSize
        {
            get { return _volumeSpaceSize; }
            set { _volumeSpaceSize = value; }
        }

        public int VolumeSequenceNumber
        {
            get { return _volumeSequenceNumber; }
        }

        public short LogicalBlockSize
        {
            get { return _logicalBlockSize; }
        }

        public string PublisherId
        {
            get { return _publisherId; }
        }

        public string PreparerId
        {
            get { return _preparerId; }
        }

        public string ApplicationId
        {
            get { return _applicationId; }
        }

        public string CopyrightFileID
        {
            get { return _copyrightFileId; }
        }

        public DateTime CreationDate
        {
            get { return _creationDate; }
        }

        public DateTime ModificationDate
        {
            get { return _modificationDate; }
        }

        public DateTime ExpirationDate
        {
            get { return _expirationDate; }
        }

        
        internal VolumeInfo(VolumeType t, string stdID, byte ver, string sysID, string volID,
            long volSpaceSize, int volSeqNum, short logBlckSize, string pubID,
            string prepID, string appID, string cpyFileIF, DateTime create, DateTime modif, DateTime expir)
        {
            _type = t;
            _standardId = stdID;
            _version = ver;
            _systemId = sysID;
            _volumeId = volID;
            _volumeSpaceSize = volSpaceSize;
            _volumeSequenceNumber = volSeqNum;
            _logicalBlockSize = logBlckSize;
            _publisherId = pubID;
            _preparerId = prepID;
            _applicationId = appID;
            _copyrightFileId = cpyFileIF;
            _creationDate = create;
            _modificationDate = modif;
            _expirationDate = expir;
        }
    }
}
