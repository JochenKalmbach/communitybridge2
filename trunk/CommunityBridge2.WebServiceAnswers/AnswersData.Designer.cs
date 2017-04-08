﻿//------------------------------------------------------------------------------
// <auto-generated>
//    This code was generated from a template.
//
//    Manual changes to this file may cause unexpected behavior in your application.
//    Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using System;
using System.ComponentModel;
using System.Data.EntityClient;
using System.Data.Objects;
using System.Data.Objects.DataClasses;
using System.Linq;
using System.Runtime.Serialization;
using System.Xml.Serialization;

[assembly: EdmSchemaAttribute()]
namespace CommunityBridge2.WebServiceAnswers
{
    #region Contexts
    
    /// <summary>
    /// No Metadata Documentation available.
    /// </summary>
    public partial class AnswersDataEntities : ObjectContext
    {
        #region Constructors
    
        /// <summary>
        /// Initializes a new AnswersDataEntities object using the connection string found in the 'AnswersDataEntities' section of the application configuration file.
        /// </summary>
        public AnswersDataEntities() : base("name=AnswersDataEntities", "AnswersDataEntities")
        {
            this.ContextOptions.LazyLoadingEnabled = true;
            OnContextCreated();
        }
    
        /// <summary>
        /// Initialize a new AnswersDataEntities object.
        /// </summary>
        public AnswersDataEntities(string connectionString) : base(connectionString, "AnswersDataEntities")
        {
            this.ContextOptions.LazyLoadingEnabled = true;
            OnContextCreated();
        }
    
        /// <summary>
        /// Initialize a new AnswersDataEntities object.
        /// </summary>
        public AnswersDataEntities(EntityConnection connection) : base(connection, "AnswersDataEntities")
        {
            this.ContextOptions.LazyLoadingEnabled = true;
            OnContextCreated();
        }
    
        #endregion
    
        #region Partial Methods
    
        partial void OnContextCreated();
    
        #endregion
    
        #region ObjectSet Properties
    
        /// <summary>
        /// No Metadata Documentation available.
        /// </summary>
        public ObjectSet<Mapping> Mappings
        {
            get
            {
                if ((_Mappings == null))
                {
                    _Mappings = base.CreateObjectSet<Mapping>("Mappings");
                }
                return _Mappings;
            }
        }
        private ObjectSet<Mapping> _Mappings;
    
        /// <summary>
        /// No Metadata Documentation available.
        /// </summary>
        public ObjectSet<Version> Versions
        {
            get
            {
                if ((_Versions == null))
                {
                    _Versions = base.CreateObjectSet<Version>("Versions");
                }
                return _Versions;
            }
        }
        private ObjectSet<Version> _Versions;

        #endregion

        #region AddTo Methods
    
        /// <summary>
        /// Deprecated Method for adding a new object to the Mappings EntitySet. Consider using the .Add method of the associated ObjectSet&lt;T&gt; property instead.
        /// </summary>
        public void AddToMappings(Mapping mapping)
        {
            base.AddObject("Mappings", mapping);
        }
    
        /// <summary>
        /// Deprecated Method for adding a new object to the Versions EntitySet. Consider using the .Add method of the associated ObjectSet&lt;T&gt; property instead.
        /// </summary>
        public void AddToVersions(Version version)
        {
            base.AddObject("Versions", version);
        }

        #endregion

    }

    #endregion

    #region Entities
    
    /// <summary>
    /// No Metadata Documentation available.
    /// </summary>
    [EdmEntityTypeAttribute(NamespaceName="AnswersDataModel", Name="Mapping")]
    [Serializable()]
    [DataContractAttribute(IsReference=true)]
    public partial class Mapping : EntityObject
    {
        #region Factory Method
    
        /// <summary>
        /// Create a new Mapping object.
        /// </summary>
        /// <param name="messageId">Initial value of the MessageId property.</param>
        /// <param name="messageNumber">Initial value of the MessageNumber property.</param>
        /// <param name="id">Initial value of the Id property.</param>
        public static Mapping CreateMapping(global::System.Guid messageId, global::System.Int64 messageNumber, global::System.Guid id)
        {
            Mapping mapping = new Mapping();
            mapping.MessageId = messageId;
            mapping.MessageNumber = messageNumber;
            mapping.Id = id;
            return mapping;
        }

        #endregion

        #region Simple Properties
    
        /// <summary>
        /// No Metadata Documentation available.
        /// </summary>
        [EdmScalarPropertyAttribute(EntityKeyProperty=false, IsNullable=false)]
        [DataMemberAttribute()]
        public global::System.Guid MessageId
        {
            get
            {
                return _MessageId;
            }
            set
            {
                OnMessageIdChanging(value);
                ReportPropertyChanging("MessageId");
                _MessageId = StructuralObject.SetValidValue(value, "MessageId");
                ReportPropertyChanged("MessageId");
                OnMessageIdChanged();
            }
        }
        private global::System.Guid _MessageId;
        partial void OnMessageIdChanging(global::System.Guid value);
        partial void OnMessageIdChanged();
    
        /// <summary>
        /// No Metadata Documentation available.
        /// </summary>
        [EdmScalarPropertyAttribute(EntityKeyProperty=false, IsNullable=false)]
        [DataMemberAttribute()]
        public global::System.Int64 MessageNumber
        {
            get
            {
                return _MessageNumber;
            }
            set
            {
                OnMessageNumberChanging(value);
                ReportPropertyChanging("MessageNumber");
                _MessageNumber = StructuralObject.SetValidValue(value, "MessageNumber");
                ReportPropertyChanged("MessageNumber");
                OnMessageNumberChanged();
            }
        }
        private global::System.Int64 _MessageNumber;
        partial void OnMessageNumberChanging(global::System.Int64 value);
        partial void OnMessageNumberChanged();
    
        /// <summary>
        /// No Metadata Documentation available.
        /// </summary>
        [EdmScalarPropertyAttribute(EntityKeyProperty=false, IsNullable=true)]
        [DataMemberAttribute()]
        public global::System.String Author
        {
            get
            {
                return _Author;
            }
            set
            {
                OnAuthorChanging(value);
                ReportPropertyChanging("Author");
                _Author = StructuralObject.SetValidValue(value, true, "Author");
                ReportPropertyChanged("Author");
                OnAuthorChanged();
            }
        }
        private global::System.String _Author;
        partial void OnAuthorChanging(global::System.String value);
        partial void OnAuthorChanged();
    
        /// <summary>
        /// No Metadata Documentation available.
        /// </summary>
        [EdmScalarPropertyAttribute(EntityKeyProperty=false, IsNullable=true)]
        [DataMemberAttribute()]
        public global::System.String Title
        {
            get
            {
                return _Title;
            }
            set
            {
                OnTitleChanging(value);
                ReportPropertyChanging("Title");
                _Title = StructuralObject.SetValidValue(value, true, "Title");
                ReportPropertyChanged("Title");
                OnTitleChanged();
            }
        }
        private global::System.String _Title;
        partial void OnTitleChanging(global::System.String value);
        partial void OnTitleChanged();
    
        /// <summary>
        /// No Metadata Documentation available.
        /// </summary>
        [EdmScalarPropertyAttribute(EntityKeyProperty=false, IsNullable=true)]
        [DataMemberAttribute()]
        public global::System.String MetaData
        {
            get
            {
                return _MetaData;
            }
            set
            {
                OnMetaDataChanging(value);
                ReportPropertyChanging("MetaData");
                _MetaData = StructuralObject.SetValidValue(value, true, "MetaData");
                ReportPropertyChanged("MetaData");
                OnMetaDataChanged();
            }
        }
        private global::System.String _MetaData;
        partial void OnMetaDataChanging(global::System.String value);
        partial void OnMetaDataChanged();
    
        /// <summary>
        /// No Metadata Documentation available.
        /// </summary>
        [EdmScalarPropertyAttribute(EntityKeyProperty=true, IsNullable=false)]
        [DataMemberAttribute()]
        public global::System.Guid Id
        {
            get
            {
                return _Id;
            }
            set
            {
                if (_Id != value)
                {
                    OnIdChanging(value);
                    ReportPropertyChanging("Id");
                    _Id = StructuralObject.SetValidValue(value, "Id");
                    ReportPropertyChanged("Id");
                    OnIdChanged();
                }
            }
        }
        private global::System.Guid _Id;
        partial void OnIdChanging(global::System.Guid value);
        partial void OnIdChanged();
    
        /// <summary>
        /// No Metadata Documentation available.
        /// </summary>
        [EdmScalarPropertyAttribute(EntityKeyProperty=false, IsNullable=true)]
        [DataMemberAttribute()]
        public Nullable<global::System.DateTime> LastReplyDate
        {
            get
            {
                return _LastReplyDate;
            }
            set
            {
                OnLastReplyDateChanging(value);
                ReportPropertyChanging("LastReplyDate");
                _LastReplyDate = StructuralObject.SetValidValue(value, "LastReplyDate");
                ReportPropertyChanged("LastReplyDate");
                OnLastReplyDateChanged();
            }
        }
        private Nullable<global::System.DateTime> _LastReplyDate;
        partial void OnLastReplyDateChanging(Nullable<global::System.DateTime> value);
        partial void OnLastReplyDateChanged();
    
        /// <summary>
        /// No Metadata Documentation available.
        /// </summary>
        [EdmScalarPropertyAttribute(EntityKeyProperty=false, IsNullable=true)]
        [DataMemberAttribute()]
        public Nullable<global::System.Guid> ThreadId
        {
            get
            {
                return _ThreadId;
            }
            set
            {
                OnThreadIdChanging(value);
                ReportPropertyChanging("ThreadId");
                _ThreadId = StructuralObject.SetValidValue(value, "ThreadId");
                ReportPropertyChanged("ThreadId");
                OnThreadIdChanged();
            }
        }
        private Nullable<global::System.Guid> _ThreadId;
        partial void OnThreadIdChanging(Nullable<global::System.Guid> value);
        partial void OnThreadIdChanged();
    
        /// <summary>
        /// No Metadata Documentation available.
        /// </summary>
        [EdmScalarPropertyAttribute(EntityKeyProperty=false, IsNullable=true)]
        [DataMemberAttribute()]
        public Nullable<global::System.DateTime> ActivityDate
        {
            get
            {
                return _ActivityDate;
            }
            set
            {
                OnActivityDateChanging(value);
                ReportPropertyChanging("ActivityDate");
                _ActivityDate = StructuralObject.SetValidValue(value, "ActivityDate");
                ReportPropertyChanged("ActivityDate");
                OnActivityDateChanged();
            }
        }
        private Nullable<global::System.DateTime> _ActivityDate;
        partial void OnActivityDateChanging(Nullable<global::System.DateTime> value);
        partial void OnActivityDateChanged();
    
        /// <summary>
        /// No Metadata Documentation available.
        /// </summary>
        [EdmScalarPropertyAttribute(EntityKeyProperty=false, IsNullable=true)]
        [DataMemberAttribute()]
        public global::System.String ActivityAction
        {
            get
            {
                return _ActivityAction;
            }
            set
            {
                OnActivityActionChanging(value);
                ReportPropertyChanging("ActivityAction");
                _ActivityAction = StructuralObject.SetValidValue(value, true, "ActivityAction");
                ReportPropertyChanged("ActivityAction");
                OnActivityActionChanged();
            }
        }
        private global::System.String _ActivityAction;
        partial void OnActivityActionChanging(global::System.String value);
        partial void OnActivityActionChanged();

        #endregion

    }
    
    /// <summary>
    /// No Metadata Documentation available.
    /// </summary>
    [EdmEntityTypeAttribute(NamespaceName="AnswersDataModel", Name="Version")]
    [Serializable()]
    [DataContractAttribute(IsReference=true)]
    public partial class Version : EntityObject
    {
        #region Factory Method
    
        /// <summary>
        /// Create a new Version object.
        /// </summary>
        /// <param name="id">Initial value of the Id property.</param>
        /// <param name="version1">Initial value of the Version1 property.</param>
        public static Version CreateVersion(global::System.Guid id, global::System.Int64 version1)
        {
            Version version = new Version();
            version.Id = id;
            version.Version1 = version1;
            return version;
        }

        #endregion

        #region Simple Properties
    
        /// <summary>
        /// No Metadata Documentation available.
        /// </summary>
        [EdmScalarPropertyAttribute(EntityKeyProperty=true, IsNullable=false)]
        [DataMemberAttribute()]
        public global::System.Guid Id
        {
            get
            {
                return _Id;
            }
            set
            {
                if (_Id != value)
                {
                    OnIdChanging(value);
                    ReportPropertyChanging("Id");
                    _Id = StructuralObject.SetValidValue(value, "Id");
                    ReportPropertyChanged("Id");
                    OnIdChanged();
                }
            }
        }
        private global::System.Guid _Id;
        partial void OnIdChanging(global::System.Guid value);
        partial void OnIdChanged();
    
        /// <summary>
        /// No Metadata Documentation available.
        /// </summary>
        [EdmScalarPropertyAttribute(EntityKeyProperty=false, IsNullable=false)]
        [DataMemberAttribute()]
        public global::System.Int64 Version1
        {
            get
            {
                return _Version1;
            }
            set
            {
                OnVersion1Changing(value);
                ReportPropertyChanging("Version1");
                _Version1 = StructuralObject.SetValidValue(value, "Version1");
                ReportPropertyChanged("Version1");
                OnVersion1Changed();
            }
        }
        private global::System.Int64 _Version1;
        partial void OnVersion1Changing(global::System.Int64 value);
        partial void OnVersion1Changed();

        #endregion

    }

    #endregion

}
