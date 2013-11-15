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

using System;
using System.Collections.Generic;

namespace CommunityBridge2.NNTPServer
{
    public class Newsgroup
    {
        private string _groupName;
        private readonly bool _postingAllowed;
        private readonly DateTime _dateAdded;

        public Newsgroup(string groupName, int firstArticle, int lastArticle, bool postingAllowed, int numberOfArticles, DateTime dateAdded)
        {
            _groupName = groupName;
            _firstArticle = firstArticle;
            _lastArticle = lastArticle;
            _postingAllowed = postingAllowed;
            _numberOfArticles = numberOfArticles;
            _dateAdded = dateAdded;
        }

        public string GroupName
        {
            get
            {
                return _groupName;
            }
          protected set { _groupName = value; }
        }

        private int _firstArticle;
        public int FirstArticle
        {
            get
            {
                lock(this)
                {
                    return _firstArticle;
                }
            }
            set
            {
                lock(this)
                {
                    _firstArticle = value;
                }
            }
        }

        private int _lastArticle;
        public int LastArticle
        {
            get
            {
                lock (this)
                {
                    return _lastArticle;
                }
            }
            set
            {
                lock (this)
                {
                    _lastArticle = value;
                }
            }
        }

        public bool PostingAllowed
        {
            get
            {
                return _postingAllowed;
            }
        }

        readonly Dictionary<int, Article>  _articles = new Dictionary<int, Article>();
        public Dictionary<int, Article> Articles
        {
            get { return _articles; }
        }

        private int _numberOfArticles;
        public int NumberOfArticles
        {
            get
            {
                lock (this)
                {
                    return _numberOfArticles;
                }
            }
            set
            {
                lock(this)
                {
                    _numberOfArticles = value;
                }
            }
        }

        public DateTime DateAdded
        {
            get
            {
                return _dateAdded;
            }
        }

        public string Description { get; set; }
        public string DisplayName { get; set; }
    }

}
