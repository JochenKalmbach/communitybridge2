using Microsoft.Support.Community.DataLayer.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommunityBridge2.WebServiceAnswers
{
    public interface IForumData
    {
        void UpdateAuthTicket(string ticket);

        string[] GetSupportedLocales();

        ForumPagedResult GetForumList(string localeName);

        MetaData[] GetMetaDataListByForumId(Forum forum, string localeName);

        MetaData[] GetMetaDataListByForumId(Guid forumId, string localeName);

        Message GetMessage(Guid messageId, AdditionalMessageDataOptions additionalMessageDataOptions);

        MessagePagedResult GetMessageListByThreadId(Guid threadId, MessageSortOrder? sortOrder, SortDirection? sortDir, int startRow, int maxRows, AdditionalMessageDataOptions additionalMessageDataOptions);

        PagedForumThreadList GetThreadListByForumId(Guid forumId, string forumShortName,
            string localeName, Guid[] metadataFilters, ThreadFilter[] threadFilters, ThreadSortOrder? sortOrder, SortDirection? sortDirection, int startRow, int maxRows, AdditionalThreadDataOptions additionalThreadDataOptions);

        PagedForumThreadList GetThreadListByForumId(Guid forumId,
            string localeName, Guid[] metadataFilters, ThreadFilter[] threadFilters, ThreadSortOrder? sortOrder, SortDirection? sortDirection, int startRow, int maxRows, AdditionalThreadDataOptions additionalThreadDataOptions);

        ObsoleteThread[] GetObsoleteThreadList(System.DateTime startDate);

        Guid AddMessage(Guid threadId, Guid? parentId, string messageText);
    }
}
