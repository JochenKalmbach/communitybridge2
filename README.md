# communitybridge2

## Installation
Just download the latest version from http://communitybridge2.codeplex.com/releases and execute the msi file.
If you do not have .NET 4 runtime installed, you will first be redirected to the download page: http://www.microsoft.com/download/en/details.aspx?displaylang=en&id=17718

## Starting the bridge
You can start the bridge by selecting "Community Bridge 2" in your program files. It will then ask you for your LiveId username and password. You need to specify the same as in the answers-forums.
_Be aware that in the first time, only selected people wil lhave access to the forums via the bridge. This selection is done by Microsoft._

## Connect your newsreader
After the bridge has started and you have successfully authenticated, you can start your newsreader and create a new account. *Please be sure you select port 120 (instead of 119) in your newsreader!* Alternatively you can change the port in the main UI and restart the bridge.
* Detailed instruction for Windows Live Mail: [SetupWLM]

Hint: If you use also the social/msdn/technet bridge, be aware that you need to create TWO accounts in your newsreader. Beside your existing (social/msdn/technet) account, you need to create a new account with port 120. If your newsreader does not support two indentical servernames (like Thunderbird), then you need to specify 127.0.0.1 as servername.

## Understanding the newsgroup names
The answers forums are not very easy transferable into newsgroup names. The main problem is, that there are only a hand full of forums which has several metadata information. This structure is mapped with the bridge into newsgroups. For each available forum, there is one main-newsgroup. For each available metadata, there is one sub newsgroup.
[image:AnswersForumMetaData.jpg]
For example "Office" is one forum; this forum has metadata like "Excel, Access, Word, ..." or "Office 2007, 2010, ...". For each of this metadata a newsgroup is created. So you will see the following newsgroups:
* answers.en-us.office
* answers.en-us.office.excel
* answers.en-us.office.access
* answers.en-us.office.word
* answers.en-us.office.office_2007
* answers.en-us.office.office_2010

Be aware that a thread ,might be present in two newsgroups if several meta infos are assigned to this thread (like Excel and Office 2007, then this thread is visible in "answers.en-us.office" (contains all threads), "answers.en-us.office.excel" and "answers.en-us.office.office_2007".

## Plaintext converter
Today most newsreader also support HTML postings. But if your want you can enable the plaintext converter. This will convert the HTML into plaintext. For more info see: [url:http://communitybridge.codeplex.com/wikipage?title=Markup%20Guide&referringTitle=Documentation]

## Issues
If you have any issues with the bridge, please provide us some feedback. You can either use this page ([url:http://communitybridge2.codeplex.com/workitem/list/basic]) or all MVPs can use the new private MVP forum: [url:http://social.microsoft.com/Forums/en-us/mvpnntpanswersbridge/threads].

## Advanced options
[image:Bridge2.jpg]

Advanced actions for beginning:
- prefetch Newsgroup list (may take 2-3 minutes)
- prefetch the newsgroups you want to subscribe to
 (you can use the text filter at the bottom for filtering
- then you can start your newsreader, get the newsgroup list from the bridge and subscribe to newsgroups

Some background information:
Answers is "thread-based", not message-based. That means that all services the bridge does are based on what information it gets about the threads from the webservice (cpslite). cpslite is what runs on the Microsoft side 
and delivers the data from Answers to the bridge. cpslite is limited to the last 1000 Threads. This means that you cannot go back more than one thousand threads with the initial prefetch. The number of messages you get 
depends on the forum (of course) and the number of messages in each thread. The average number of messages is around 3 to 5 per thread. So, with 1000 threads you may get around 3000 - 5000 messages. If you get a 
lot less that means this forums doesn't have that many threads/messages. 
The default for prefetching threads is set to 300. This will get you an average of 500 - 1000 messages for a forum.
Once a newsgroup is prefetched the fetching of new threeads/messages is done in realtime when your newsreader connects and asks for new messages. 
Normally, this action should be finished within the timeout of your newsreader. But it may happen with large groups that this action takes longer. In this case your newsreader may timeout and stop trying. The bridge will NOT stop when the newsreader disconnects and will go on fetching that group until it is up-to-date. So, with the enxt conenct your 
newsreader should then get fresh data.

"Old" messages:
Because of the thread-based nature of Answers it may happen that you get old messages that you didn't want to get and that don't seem to "fit" in the "last 1000". This may happen for instance if an old thread got some activity (a new posting, soem moderator action, a "mark as answer" etc.).

Authentication and bridge startup
The bridge may seem to startup slowly (e.g. it may take up to a minute until the "Stop" button appears). If you experience that use the "Create LiveID auto login" menu item to create an authentication blob. This should speed-up things.

Reauthentication:
The bridge has to reauthenticate automatically every hour. If that fails for some reason it may not be able to connect to the webservice anymore. In that case exit the bridge and restart it. This problem should arise only very rarely if ever.

Newsgroup names:
The newsgroups names are derived from the tags Answers uses. Answers does not have "sub forums". There is only one main forum like Windows or Office or Internet Explorer. The sub forums for specific versions or Windows 
versions or other topics are just "tags". Then we also have a lot of locales, e.g. "languages". From this mix the bridge creates around 9000 groups. This is only a subset of what you could create by recombination of 
the various tags.
If you subscribe for instance to
- answers.de-DE.ie
you get the German Internet Explorer forum.
- answers.de-DE.ie.ie9
- answers.de-DE.ie.windows7
give you a subset of the main IE forum with the messages for the respective tags.
Please note that although these are subsets of the main forum and contain messages from it the bridge and your newsreader has to fetch *all* of it. Which means you get lots of duplicate messages in the bridge cache and in  
your subscribed groups in the newsreader. We can't avoid that. So, the suggestion is, that you subscribe only to the main group or only to a few sub groups or you will create a lot of duplicate network traffic and data.

Meta Data:
You can display the metadata about a thread/message in the subject or in the signature of a message or both or none. This can be set in advanced settings. Default is set to "in subject and signature".
