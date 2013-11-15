// QinetiQ SharePoint NNTP Server
// http://spnews.codeplex.com
// ---------------------------------------------------------------------------
// Last updated: Sep 2009
// ---------------------------------------------------------------------------
// 
// Microsoft Public License (Ms-PL)
//
// This license governs use of the accompanying software. If you use the software, you
// accept this license. If you do not accept the license, do not use the software.
//
// 1. Definitions
// The terms "reproduce," "reproduction," "derivative works," and "distribution" have the
// same meaning here as under U.S. copyright law.
// A "contribution" is the original software, or any additions or changes to the software.
// A "contributor" is any person that distributes its contribution under this license.
// "Licensed patents" are a contributor's patent claims that read directly on its contribution.
//
// 2. Grant of Rights
// (A) Copyright Grant- Subject to the terms of this license, including the license conditions and limitations in section 3, each contributor grants you a non-exclusive, worldwide, royalty-free copyright license to reproduce its contribution, prepare derivative works of its contribution, and distribute its contribution or any derivative works that you create.
// (B) Patent Grant- Subject to the terms of this license, including the license conditions and limitations in section 3, each contributor grants you a non-exclusive, worldwide, royalty-free license under its licensed patents to make, have made, use, sell, offer for sale, import, and/or otherwise dispose of its contribution in the software or derivative works of the contribution in the software.
//
// 3. Conditions and Limitations
// (A) No Trademark License- This license does not grant you rights to use any contributors' name, logo, or trademarks.
// (B) If you bring a patent claim against any contributor over patents that you claim are infringed by the software, your patent license from such contributor to the software ends automatically.
// (C) If you distribute any portion of the software, you must retain all copyright, patent, trademark, and attribution notices that are present in the software.
// (D) If you distribute any portion of the software in source code form, you may do so only under this license by including a complete copy of this license with your distribution. If you distribute any portion of the software in compiled or object code form, you may only do so under a license that complies with this license.
// (E) The software is licensed "as-is." You bear the risk of using it. The contributors give no express warranties, guarantees or conditions. You may have additional consumer rights under your local laws which this license cannot change. To the extent permitted under your local laws, the contributors exclude the implied warranties of merchantability, fitness for a particular purpose and non-infringement.
//

namespace CommunityBridge2.NNTPServer
{
    public enum PerfCounterName
    {
        TotalNntpCommandArticle,
        TotalNntpCommandBody,
        TotalNntpCommandHead,
        TotalNntpCommandStat,
        TotalNntpCommandNewNews,
        TotalNntpCommandGroup,
        TotalNntpCommandHelp,
        TotalNntpCommandIHave,
        TotalNntpCommandLast,
        TotalNntpCommandList,
        TotalNntpCommandNext,
        TotalNntpCommandNewgroups,
        TotalNntpCommandPost,
        TotalNntpCommandPostData,
        TotalNntpCommandQuit,
        TotalNntpCommandSlave,
        TotalNntpCommandNotRecognised,
        TotalNntpCommandSyntaxError,
        TotalNntpCommandAdhoc,
        TotalNntpCommandXHdr,
        TotalNntpCommandEndOfData,
        TotalNntpCommandAuthInfo,
        TotalNntpCommandXOver,
        TotalNntpCommandMode,
        UserPermissionsCacheItems,
        UserPermissionsCacheHits,
        UserPermissionsCacheMisses,
        ArticleBodyCacheItems,
        ArticleBodyCacheHits,
        ArticleBodyCacheMisses,
        ArticleBodyCacheTotalBytes,
        AdGroupCacheItems,
        AdGroupCacheHits,
        AdGroupCacheMisses,
        TotalNewsgroups,
        BytesSent,
        BytesReceived,
        CurrentConnections,
        TotalConnections,
        ResponseHelpTextFollows,
        ResponseServerReadyPostingAllowed,
        ResponseServerReadyPostingNotAllowed,
        ResponseSlaveStatusNoted,
        ResponseGoodbye,
        ResponseListOfGroupsFollow,
        ResponseListOfNewGroupsFollow,
        ResponseArticleTransferredOk,
        ResponseArticlePostedOk,
        ResponseHeaderFollows,
        ResponseOverviewInformationFollows,
        ResponseListOfNewsArticlesFollow,
        ResponseGroupSelected,
        ResponseArticleRetrievedHeadBodyFollow,
        ResponseArticleRetrievedHeadFollows,
        ResponseArticleRetrievedBodyFollows,
        ResponseArticleRetrievedRequestTextSeperately,
        ResponsePackagesFollowNtlm,
        ResponseAuthenticationAccepted,
        ResponseSendArticleIHave,
        ResponseSendArticlePost,
        ResponseMoreAuthenticationInformationRequired,
        ResponseProtocolSupportedProceed,
        ResponseArticleRetrievedNoGroupSelected,
        ResponseArticleRetrievedNoArticleSelected,
        ResponseArticleRetrievedNoNextArticleInGroup,
        ResponseArticleRetrievedNoPreviousArticleInGroup,
        ResponseArticleRetrievedNoArticleInGroup,
        ResponseArticleRetrievedNoArticle,
        ResponseArticleNotWanted,
        ResponseTransferFailed,
        ResponseArticleRejected,
        ResponseNoSuchGroup,
        ResponsePostingNotAllowed,
        ResponsePostingFailed,
        ResponsePostingFailedExcessiveLength,
        ResponsePostingFailedSubjectLineBlank,
        ResponsePostingFailedTextPartMissingInHtml,
        ResponsePostingFailedAccessDenied,
        ResponsePostingFailedGroupNotFound,
        ResponseAuthenticationRequired,
        ResponseAuthenticationRejected,
        ResponseCommandNotRecognised,
        ResponseCommandSyntaxError,
        ResponseAccessDenied,
        ResponseProgramFault,
        AverageProcessingTime,
        AverageProcessingTimeBase,
        CommandsProcessedPerSecond,
        MessageCount
    }

    public static class PerfCounters
    {
        //private const string LEGACY_CATEGORY = "QinetiQ NNTP Server";
        //private const string COMMANDS_CATEGORY = "QinetiQ NNTP Server - Commands";
        //private const string RESPONSES_CATEGORY = "QinetiQ NNTP Server - Responses";
        //private const string NETWORKING_CATEGORY = "QinetiQ NNTP Server - Networking";
        //private const string CACHES_CATEGORY = "QinetiQ NNTP Server - Caches";
        //private const string GENERAL_CATEGORY = "QinetiQ NNTP Server - General";
        //private static Dictionary<PerfCounterName, string> _counterCategoryMap = new Dictionary<PerfCounterName, string>();

        //private static void DeleteCategory(string categoryName, bool purge)
        //{
            //if (PerformanceCounterCategory.Exists(categoryName))
            //{
            //    if (!purge)
            //    {
            //        return;
            //    }
            //    else
            //    {
            //        PerformanceCounterCategory.Delete(categoryName);
            //    }
            //}
        //}

        public static void Initialise()
        {
            //Create(true, false);
        }

        public static void Create(bool initOnly, bool purge)
        {
            //// note - counter creation requires admin rights hence should
            ////        called from the installer for the service

            //try
            //{
            //    if (!initOnly)
            //    {
            //        DeleteCategory(LEGACY_CATEGORY, purge);
            //    }

            //    // nntp command totals

            //    if (!initOnly)
            //    {
            //        DeleteCategory(COMMANDS_CATEGORY, purge);
            //    }

            //    CounterCreationDataCollection colCommands = new CounterCreationDataCollection();

            //    AddCounter(colCommands, PerfCounterName.TotalNntpCommandAdhoc, PerformanceCounterType.NumberOfItems64, COMMANDS_CATEGORY, initOnly);
            //    AddCounter(colCommands, PerfCounterName.TotalNntpCommandArticle, PerformanceCounterType.NumberOfItems64, COMMANDS_CATEGORY, initOnly);
            //    AddCounter(colCommands, PerfCounterName.TotalNntpCommandAuthInfo, PerformanceCounterType.NumberOfItems64, COMMANDS_CATEGORY, initOnly);
            //    AddCounter(colCommands, PerfCounterName.TotalNntpCommandBody, PerformanceCounterType.NumberOfItems64, COMMANDS_CATEGORY, initOnly);
            //    AddCounter(colCommands, PerfCounterName.TotalNntpCommandEndOfData, PerformanceCounterType.NumberOfItems64, COMMANDS_CATEGORY, initOnly);
            //    AddCounter(colCommands, PerfCounterName.TotalNntpCommandGroup, PerformanceCounterType.NumberOfItems64, COMMANDS_CATEGORY, initOnly);
            //    AddCounter(colCommands, PerfCounterName.TotalNntpCommandHead, PerformanceCounterType.NumberOfItems64, COMMANDS_CATEGORY, initOnly);
            //    AddCounter(colCommands, PerfCounterName.TotalNntpCommandHelp, PerformanceCounterType.NumberOfItems64, COMMANDS_CATEGORY, initOnly);
            //    AddCounter(colCommands, PerfCounterName.TotalNntpCommandIHave, PerformanceCounterType.NumberOfItems64, COMMANDS_CATEGORY, initOnly);
            //    AddCounter(colCommands, PerfCounterName.TotalNntpCommandLast, PerformanceCounterType.NumberOfItems64, COMMANDS_CATEGORY, initOnly);
            //    AddCounter(colCommands, PerfCounterName.TotalNntpCommandList, PerformanceCounterType.NumberOfItems64, COMMANDS_CATEGORY, initOnly);
            //    AddCounter(colCommands, PerfCounterName.TotalNntpCommandMode, PerformanceCounterType.NumberOfItems64, COMMANDS_CATEGORY, initOnly);
            //    AddCounter(colCommands, PerfCounterName.TotalNntpCommandNewgroups, PerformanceCounterType.NumberOfItems64, COMMANDS_CATEGORY, initOnly);
            //    AddCounter(colCommands, PerfCounterName.TotalNntpCommandNewNews, PerformanceCounterType.NumberOfItems64, COMMANDS_CATEGORY, initOnly);
            //    AddCounter(colCommands, PerfCounterName.TotalNntpCommandNext, PerformanceCounterType.NumberOfItems64, COMMANDS_CATEGORY, initOnly);
            //    AddCounter(colCommands, PerfCounterName.TotalNntpCommandNotRecognised, PerformanceCounterType.NumberOfItems64, COMMANDS_CATEGORY, initOnly);
            //    AddCounter(colCommands, PerfCounterName.TotalNntpCommandPost, PerformanceCounterType.NumberOfItems64, COMMANDS_CATEGORY, initOnly);
            //    AddCounter(colCommands, PerfCounterName.TotalNntpCommandPostData, PerformanceCounterType.NumberOfItems64, COMMANDS_CATEGORY, initOnly);
            //    AddCounter(colCommands, PerfCounterName.TotalNntpCommandQuit, PerformanceCounterType.NumberOfItems64, COMMANDS_CATEGORY, initOnly);
            //    AddCounter(colCommands, PerfCounterName.TotalNntpCommandSlave, PerformanceCounterType.NumberOfItems64, COMMANDS_CATEGORY, initOnly);
            //    AddCounter(colCommands, PerfCounterName.TotalNntpCommandStat, PerformanceCounterType.NumberOfItems64, COMMANDS_CATEGORY, initOnly);
            //    AddCounter(colCommands, PerfCounterName.TotalNntpCommandSyntaxError, PerformanceCounterType.NumberOfItems64, COMMANDS_CATEGORY, initOnly);
            //    AddCounter(colCommands, PerfCounterName.TotalNntpCommandXHdr, PerformanceCounterType.NumberOfItems64, COMMANDS_CATEGORY, initOnly);
            //    AddCounter(colCommands, PerfCounterName.TotalNntpCommandXOver, PerformanceCounterType.NumberOfItems64, COMMANDS_CATEGORY, initOnly);
            //    AddCounter(colCommands, PerfCounterName.AverageProcessingTime, PerformanceCounterType.AverageTimer32, COMMANDS_CATEGORY, initOnly);
            //    AddCounter(colCommands, PerfCounterName.AverageProcessingTimeBase, PerformanceCounterType.AverageBase, COMMANDS_CATEGORY, initOnly);
            //    AddCounter(colCommands, PerfCounterName.CommandsProcessedPerSecond, PerformanceCounterType.RateOfCountsPerSecond32, COMMANDS_CATEGORY, initOnly);

            //    if (!initOnly)
            //    {
            //        if (!PerformanceCounterCategory.Exists(COMMANDS_CATEGORY))
            //        {
            //            PerformanceCounterCategory.Create(COMMANDS_CATEGORY, "Performance counters for the QinetiQ NNTP news server", PerformanceCounterCategoryType.SingleInstance, colCommands);
            //        }
            //    }

            //    // cache metrics

            //    if (!initOnly)
            //    {
            //        DeleteCategory(RESPONSES_CATEGORY, purge);
            //    }

            //    CounterCreationDataCollection colCaches = new CounterCreationDataCollection();

            //    AddCounter(colCaches, PerfCounterName.UserPermissionsCacheHits, PerformanceCounterType.NumberOfItems64, CACHES_CATEGORY, initOnly);
            //    AddCounter(colCaches, PerfCounterName.UserPermissionsCacheMisses, PerformanceCounterType.NumberOfItems64, CACHES_CATEGORY, initOnly);
            //    AddCounter(colCaches, PerfCounterName.UserPermissionsCacheItems, PerformanceCounterType.NumberOfItems64, CACHES_CATEGORY, initOnly);
            //    AddCounter(colCaches, PerfCounterName.AdGroupCacheHits, PerformanceCounterType.NumberOfItems64, CACHES_CATEGORY, initOnly);
            //    AddCounter(colCaches, PerfCounterName.AdGroupCacheItems, PerformanceCounterType.NumberOfItems64, CACHES_CATEGORY, initOnly);
            //    AddCounter(colCaches, PerfCounterName.AdGroupCacheMisses, PerformanceCounterType.NumberOfItems64, CACHES_CATEGORY, initOnly);
            //    AddCounter(colCaches, PerfCounterName.ArticleBodyCacheHits, PerformanceCounterType.NumberOfItems64, CACHES_CATEGORY, initOnly);
            //    AddCounter(colCaches, PerfCounterName.ArticleBodyCacheItems, PerformanceCounterType.NumberOfItems64, CACHES_CATEGORY, initOnly);
            //    AddCounter(colCaches, PerfCounterName.ArticleBodyCacheMisses, PerformanceCounterType.NumberOfItems64, CACHES_CATEGORY, initOnly);
            //    AddCounter(colCaches, PerfCounterName.ArticleBodyCacheTotalBytes, PerformanceCounterType.NumberOfItems64, CACHES_CATEGORY, initOnly);

            //    if (!initOnly)
            //    {
            //        if (!PerformanceCounterCategory.Exists(RESPONSES_CATEGORY))
            //        {
            //            PerformanceCounterCategory.Create(CACHES_CATEGORY, "Performance counters for the QinetiQ NNTP news server", PerformanceCounterCategoryType.SingleInstance, colCaches);
            //        }
            //    }

            //    // responses

            //    if (!initOnly)
            //    {
            //        DeleteCategory(NETWORKING_CATEGORY, purge);
            //    }

            //    CounterCreationDataCollection colResponses = new CounterCreationDataCollection();

            //    AddCounter(colResponses, PerfCounterName.ResponseAccessDenied, PerformanceCounterType.NumberOfItems64, RESPONSES_CATEGORY, initOnly);
            //    AddCounter(colResponses, PerfCounterName.ResponseArticleNotWanted, PerformanceCounterType.NumberOfItems64, RESPONSES_CATEGORY, initOnly);
            //    AddCounter(colResponses, PerfCounterName.ResponseArticlePostedOk, PerformanceCounterType.NumberOfItems64, RESPONSES_CATEGORY, initOnly);
            //    AddCounter(colResponses, PerfCounterName.ResponseArticleRejected, PerformanceCounterType.NumberOfItems64, RESPONSES_CATEGORY, initOnly);
            //    AddCounter(colResponses, PerfCounterName.ResponseArticleRetrievedBodyFollows, PerformanceCounterType.NumberOfItems64, RESPONSES_CATEGORY, initOnly);
            //    AddCounter(colResponses, PerfCounterName.ResponseArticleRetrievedHeadBodyFollow, PerformanceCounterType.NumberOfItems64, RESPONSES_CATEGORY, initOnly);
            //    AddCounter(colResponses, PerfCounterName.ResponseArticleRetrievedHeadFollows, PerformanceCounterType.NumberOfItems64, RESPONSES_CATEGORY, initOnly);
            //    AddCounter(colResponses, PerfCounterName.ResponseArticleRetrievedNoArticle, PerformanceCounterType.NumberOfItems64, RESPONSES_CATEGORY, initOnly);
            //    AddCounter(colResponses, PerfCounterName.ResponseArticleRetrievedNoArticleInGroup, PerformanceCounterType.NumberOfItems64, RESPONSES_CATEGORY, initOnly);
            //    AddCounter(colResponses, PerfCounterName.ResponseArticleRetrievedNoArticleSelected, PerformanceCounterType.NumberOfItems64, RESPONSES_CATEGORY, initOnly);
            //    AddCounter(colResponses, PerfCounterName.ResponseArticleRetrievedNoGroupSelected, PerformanceCounterType.NumberOfItems64, RESPONSES_CATEGORY, initOnly);
            //    AddCounter(colResponses, PerfCounterName.ResponseArticleRetrievedNoNextArticleInGroup, PerformanceCounterType.NumberOfItems64, RESPONSES_CATEGORY, initOnly);
            //    AddCounter(colResponses, PerfCounterName.ResponseArticleRetrievedNoPreviousArticleInGroup, PerformanceCounterType.NumberOfItems64, RESPONSES_CATEGORY, initOnly);
            //    AddCounter(colResponses, PerfCounterName.ResponseArticleRetrievedRequestTextSeperately, PerformanceCounterType.NumberOfItems64, RESPONSES_CATEGORY, initOnly);
            //    AddCounter(colResponses, PerfCounterName.ResponseArticleTransferredOk, PerformanceCounterType.NumberOfItems64, RESPONSES_CATEGORY, initOnly);
            //    AddCounter(colResponses, PerfCounterName.ResponseAuthenticationAccepted, PerformanceCounterType.NumberOfItems64, RESPONSES_CATEGORY, initOnly);
            //    AddCounter(colResponses, PerfCounterName.ResponseAuthenticationRejected, PerformanceCounterType.NumberOfItems64, RESPONSES_CATEGORY, initOnly);
            //    AddCounter(colResponses, PerfCounterName.ResponseAuthenticationRequired, PerformanceCounterType.NumberOfItems64, RESPONSES_CATEGORY, initOnly);
            //    AddCounter(colResponses, PerfCounterName.ResponseCommandNotRecognised, PerformanceCounterType.NumberOfItems64, RESPONSES_CATEGORY, initOnly);
            //    AddCounter(colResponses, PerfCounterName.ResponseCommandSyntaxError, PerformanceCounterType.NumberOfItems64, RESPONSES_CATEGORY, initOnly);
            //    AddCounter(colResponses, PerfCounterName.ResponseGoodbye, PerformanceCounterType.NumberOfItems64, RESPONSES_CATEGORY, initOnly);
            //    AddCounter(colResponses, PerfCounterName.ResponseGroupSelected, PerformanceCounterType.NumberOfItems64, RESPONSES_CATEGORY, initOnly);
            //    AddCounter(colResponses, PerfCounterName.ResponseHeaderFollows, PerformanceCounterType.NumberOfItems64, RESPONSES_CATEGORY, initOnly);
            //    AddCounter(colResponses, PerfCounterName.ResponseHelpTextFollows, PerformanceCounterType.NumberOfItems64, RESPONSES_CATEGORY, initOnly);
            //    AddCounter(colResponses, PerfCounterName.ResponseListOfGroupsFollow, PerformanceCounterType.NumberOfItems64, RESPONSES_CATEGORY, initOnly);
            //    AddCounter(colResponses, PerfCounterName.ResponseListOfNewGroupsFollow, PerformanceCounterType.NumberOfItems64, RESPONSES_CATEGORY, initOnly);
            //    AddCounter(colResponses, PerfCounterName.ResponseListOfNewsArticlesFollow, PerformanceCounterType.NumberOfItems64, RESPONSES_CATEGORY, initOnly);
            //    AddCounter(colResponses, PerfCounterName.ResponseMoreAuthenticationInformationRequired, PerformanceCounterType.NumberOfItems64, RESPONSES_CATEGORY, initOnly);
            //    AddCounter(colResponses, PerfCounterName.ResponseNoSuchGroup, PerformanceCounterType.NumberOfItems64, RESPONSES_CATEGORY, initOnly);
            //    AddCounter(colResponses, PerfCounterName.ResponseOverviewInformationFollows, PerformanceCounterType.NumberOfItems64, RESPONSES_CATEGORY, initOnly);
            //    AddCounter(colResponses, PerfCounterName.ResponsePackagesFollowNTLM, PerformanceCounterType.NumberOfItems64, RESPONSES_CATEGORY, initOnly);
            //    AddCounter(colResponses, PerfCounterName.ResponsePostingFailed, PerformanceCounterType.NumberOfItems64, RESPONSES_CATEGORY, initOnly);
            //    AddCounter(colResponses, PerfCounterName.ResponsePostingFailedAccessDenied, PerformanceCounterType.NumberOfItems64, RESPONSES_CATEGORY, initOnly);
            //    AddCounter(colResponses, PerfCounterName.ResponsePostingFailedExcessiveLength, PerformanceCounterType.NumberOfItems64, RESPONSES_CATEGORY, initOnly);
            //    AddCounter(colResponses, PerfCounterName.ResponsePostingFailedGroupNotFound, PerformanceCounterType.NumberOfItems64, RESPONSES_CATEGORY, initOnly);
            //    AddCounter(colResponses, PerfCounterName.ResponsePostingFailedSubjectLineBlank, PerformanceCounterType.NumberOfItems64, RESPONSES_CATEGORY, initOnly);
            //    AddCounter(colResponses, PerfCounterName.ResponsePostingFailedTextPartMissingInHtml, PerformanceCounterType.NumberOfItems64, RESPONSES_CATEGORY, initOnly);
            //    AddCounter(colResponses, PerfCounterName.ResponsePostingNotAllowed, PerformanceCounterType.NumberOfItems64, RESPONSES_CATEGORY, initOnly);
            //    AddCounter(colResponses, PerfCounterName.ResponseProgramFault, PerformanceCounterType.NumberOfItems64, RESPONSES_CATEGORY, initOnly);
            //    AddCounter(colResponses, PerfCounterName.ResponseProtocolSupportedProceed, PerformanceCounterType.NumberOfItems64, RESPONSES_CATEGORY, initOnly);
            //    AddCounter(colResponses, PerfCounterName.ResponseSendArticleIHave, PerformanceCounterType.NumberOfItems64, RESPONSES_CATEGORY, initOnly);
            //    AddCounter(colResponses, PerfCounterName.ResponseSendArticlePost, PerformanceCounterType.NumberOfItems64, RESPONSES_CATEGORY, initOnly);
            //    AddCounter(colResponses, PerfCounterName.ResponseServerReadyPostingAllowed, PerformanceCounterType.NumberOfItems64, RESPONSES_CATEGORY, initOnly);
            //    AddCounter(colResponses, PerfCounterName.ResponseServerReadyPostingNotAllowed, PerformanceCounterType.NumberOfItems64, RESPONSES_CATEGORY, initOnly);
            //    AddCounter(colResponses, PerfCounterName.ResponseSlaveStatusNoted, PerformanceCounterType.NumberOfItems64, RESPONSES_CATEGORY, initOnly);
            //    AddCounter(colResponses, PerfCounterName.ResponseTransferFailed, PerformanceCounterType.NumberOfItems64, RESPONSES_CATEGORY, initOnly);

            //    if (!initOnly)
            //    {
            //        if (!PerformanceCounterCategory.Exists(NETWORKING_CATEGORY))
            //        {
            //            PerformanceCounterCategory.Create(RESPONSES_CATEGORY, "Performance counters for the QinetiQ NNTP news server", PerformanceCounterCategoryType.SingleInstance, colResponses);
            //        }
            //    }

            //    // networking

            //    if (!initOnly)
            //    {
            //        DeleteCategory(CACHES_CATEGORY, purge);
            //    }

            //    CounterCreationDataCollection colNetworking = new CounterCreationDataCollection();

            //    AddCounter(colNetworking, PerfCounterName.BytesReceived, PerformanceCounterType.NumberOfItems64, NETWORKING_CATEGORY, initOnly);
            //    AddCounter(colNetworking, PerfCounterName.BytesSent, PerformanceCounterType.NumberOfItems64, NETWORKING_CATEGORY, initOnly);
            //    AddCounter(colNetworking, PerfCounterName.CurrentConnections, PerformanceCounterType.NumberOfItems64, NETWORKING_CATEGORY, initOnly);
            //    AddCounter(colNetworking, PerfCounterName.TotalConnections, PerformanceCounterType.NumberOfItems64, NETWORKING_CATEGORY, initOnly);

            //    if (!initOnly)
            //    {
            //        if (!PerformanceCounterCategory.Exists(CACHES_CATEGORY))
            //        {
            //            PerformanceCounterCategory.Create(NETWORKING_CATEGORY, "Performance counters for the QinetiQ NNTP news server", PerformanceCounterCategoryType.SingleInstance, colNetworking);
            //        }
            //    }

            //    // general

            //    if (!initOnly)
            //    {
            //        DeleteCategory(GENERAL_CATEGORY, purge);
            //    }

            //    CounterCreationDataCollection colGeneral = new CounterCreationDataCollection();

            //    AddCounter(colGeneral, PerfCounterName.TotalNewsgroups, PerformanceCounterType.NumberOfItems64, GENERAL_CATEGORY, initOnly);
            //    AddCounter(colGeneral, PerfCounterName.MessageCount, PerformanceCounterType.NumberOfItems64, GENERAL_CATEGORY, initOnly);

            //    if (!initOnly)
            //    {
            //        if (!PerformanceCounterCategory.Exists(GENERAL_CATEGORY))
            //        {
            //            PerformanceCounterCategory.Create(GENERAL_CATEGORY, "Performance counters for the QinetiQ NNTP news server", PerformanceCounterCategoryType.SingleInstance, colGeneral);
            //        }
            //    }
            //}
            //catch (Exception ex)
            //{
            //    Logging.Error("Failed to create performance counters", ex);
            //}
        }

        public static void Reset()
        {
            //try
            //{
            //    foreach (PerfCounterName item in Enum.GetValues(typeof(PerfCounterName)))
            //    {
            //        SetCounterValue(item, 0);
            //    }
            //}
            //catch (Exception ex)
            //{
            //    Logging.Error("Failed to reset performance counters", ex);
            //}
        }

        //private static void AddCounter(CounterCreationDataCollection counterCreationDataCollection, PerfCounterName name, PerformanceCounterType type, string category, bool initOnly)
        //{
            //try
            //{
            //    if (!initOnly)
            //    {
            //        CounterCreationData counter = new CounterCreationData();

            //        counter.CounterName = name.ToString();
            //        counter.CounterHelp = name.ToString();
            //        counter.CounterType = type;

            //        if (!counterCreationDataCollection.Contains(counter))
            //        {
            //            counterCreationDataCollection.Add(counter);
            //        }
            //    }

            //    if (!_counterCategoryMap.ContainsKey(name))
            //    {
            //        _counterCategoryMap.Add(name, category);
            //    }
            //}
            //catch (Exception ex)
            //{
            //    Logging.Error("Failed to add counter [" + name.ToString() + "]", ex);
            //}
        //}

        public static void IncrementCounter(PerfCounterName name)
        {
            //try
            //{
            //    using (PerformanceCounter counter = new PerformanceCounter(_counterCategoryMap[name], name.ToString(), false))
            //    {
            //        counter.Increment();
            //    }
                
            //}
            //catch (Exception ex)
            //{
            //    Logging.Error("Failed to increment counter [" + name.ToString() + "]", ex);
            //}
        }

        public static void IncrementCounterBy(PerfCounterName name, long val)
        {
            //try
            //{
            //    using (PerformanceCounter counter = new PerformanceCounter(_counterCategoryMap[name], name.ToString(), false))
            //    {
            //        counter.IncrementBy(val);
            //    }
            //}
            //catch (Exception ex)
            //{
            //    Logging.Error("Failed to increment counter [" + name.ToString() + "] by set amount", ex);
            //}
        }

        public static void DecrementCounterBy(PerfCounterName name, long val)
        {
            //try
            //{
            //    using (PerformanceCounter counter = new PerformanceCounter(_counterCategoryMap[name], name.ToString(), false))
            //    {
            //        long newVal = counter.RawValue - val;
            //        if (newVal >= 0)
            //        {
            //            counter.RawValue = newVal;
            //        }
            //    }
            //}
            //catch (Exception ex)
            //{
            //    Logging.Error("Failed to decrement counter [" + name.ToString() + "] by set amount", ex);
            //}
        }

        public static void DecrementCounter(PerfCounterName name)
        {
            //try
            //{
            //    using (PerformanceCounter counter = new PerformanceCounter(_counterCategoryMap[name], name.ToString(), false))
            //    {
            //        counter.Decrement();
            //    }
            //}
            //catch (Exception ex)
            //{
            //    Logging.Error("Failed to decrement counter [" + name.ToString() + "]", ex);
            //}
        }

        public static void SetCounterValue(PerfCounterName name, long val)
        {
            //try
            //{
            //    using (PerformanceCounter counter = new PerformanceCounter(_counterCategoryMap[name], name.ToString(), false))
            //    {
            //        counter.RawValue = val;
            //    }
            //}
            //catch (Exception ex)
            //{
            //    Logging.Error("Failed to set raw value of counter [" + name.ToString() + "]", ex);
            //}
        }
    }
}
