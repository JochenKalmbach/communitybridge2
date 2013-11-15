using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using CommunityBridge2.NNTPServer;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CommunityBridge2.ArticleConverter;

namespace CommunityBridge2.PlainTextConverterTests
{
    /// <summary>
    /// Summary description for PlainTextConverterTests
    /// </summary>
    [TestClass]
    public class PlainTextConverterTests
    {

        public PlainTextConverterTests()
        {
            //
            // TODO: Add constructor logic here
            //
        }

        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        #region Additional test attributes
        //
        // You can use the following additional attributes as you write your tests:
        //
        // Use ClassInitialize to run code before running the first test in the class
        // [ClassInitialize()]
        // public static void MyClassInitialize(TestContext testContext) { }
        //
        // Use ClassCleanup to run code after all tests in a class have run
        // [ClassCleanup()]
        // public static void MyClassCleanup() { }
        //
        // Use TestInitialize to run code before running each test 
        // [TestInitialize()]
        // public void MyTestInitialize() { }
        //
        // Use TestCleanup to run code after each test has run
        // [TestCleanup()]
        // public void MyTestCleanup() { }
        //
        #endregion

        [DataSource("System.Data.OleDb", "Provider=Microsoft.Jet.OLEDB.4.0;Data Source=|DataDirectory|\\TestData.mdb", "PlainTextConverterText2Html", DataAccessMethod.Sequential), DeploymentItem("PlainTextConverterTests\\TestData.mdb"), TestMethod]
        public void TestText2HtmlWithData()
        {
            var c = new Converter {UsePlainTextConverter = UsePlainTextConverters.SendAndReceive};
            addUserDefinedTags(ref c);

            // test data
            c.PostsAreAlwaysFormatFlowed = TestContext.DataRow["FormatFlowed"].Equals(true);
            string input = (TestContext.DataRow["Input"] as string);
            string output = (TestContext.DataRow["Output"] as string);
            Boolean areEqual = TestContext.DataRow["Equal"].Equals(true);

            // test
            var a = new Article { Body = input, ContentType = null };
            c.NewArticleFromClient(a);
            if (areEqual)
            {
                Assert.AreEqual(output, a.Body);
            }
            else
            {
                Assert.AreNotEqual(output, a.Body);
            }
        }

        [DataSource("System.Data.OleDb", "Provider=Microsoft.Jet.OLEDB.4.0;Data Source=|DataDirectory|\\TestData.mdb", "PlainTextConverterText2Html", DataAccessMethod.Sequential), DeploymentItem("PlainTextConverterTests\\TestData.mdb"), TestMethod]
        public void TestUnixText2HtmlWithData()
        {
            // linke break = \n without \r

            var c = new Converter { UsePlainTextConverter = UsePlainTextConverters.SendAndReceive };
            addUserDefinedTags(ref c);

            // test data
            c.PostsAreAlwaysFormatFlowed = TestContext.DataRow["FormatFlowed"].Equals(true);
            string input = (TestContext.DataRow["Input"] as string).Replace("\n", "\r\n").Replace("\r", String.Empty);
            string output = (TestContext.DataRow["Output"] as string);
            Boolean areEqual = TestContext.DataRow["Equal"].Equals(true);

            // test
            var a = new Article { Body = input, ContentType = null };
            c.NewArticleFromClient(a);
            if (areEqual)
            {
                Assert.AreEqual(output, a.Body);
            }
            else
            {
                Assert.AreNotEqual(output, a.Body);
            }
        }

        private void addUserDefinedTags(ref Converter c)
        {
            // init user-defined tags
            c.UserDefinedTags = new UserDefinedTagCollection();

            var utag = new UserDefinedTag();
            utag.HtmlText = "<a href=\"http://www.example.com/faq.htm#{TEXT}\">FAQ {TEXT}</a>";
            utag.TagName = "FAQ";
            c.UserDefinedTags.Add(utag);

            utag = new UserDefinedTag();
            utag.HtmlText = "<a href=\"http://www.example.com/{TEXT1}/index.html\">{TEXT2}</a>";
            utag.TagName = "REF";
            c.UserDefinedTags.Add(utag);

            utag = new UserDefinedTag();
            utag.HtmlText = "<ul>{TEXT}</ul>";
            utag.TagName = "UL";
            c.UserDefinedTags.Add(utag);

            utag = new UserDefinedTag();
            utag.HtmlText = "<li>{TEXT}</li>";
            utag.TagName = "LI";
            c.UserDefinedTags.Add(utag);

            utag = new UserDefinedTag();
            utag.HtmlText = "<img src=\"{TEXT}\" />";
            utag.TagName = "IMG";
            c.UserDefinedTags.Add(utag);
        }

        [DataSource("System.Data.OleDb", "Provider=Microsoft.Jet.OLEDB.4.0;Data Source=|DataDirectory|\\TestData.mdb", "PlainTextConverterHtml2Text", DataAccessMethod.Sequential), DeploymentItem("PlainTextConverterTests\\TestData.mdb"), TestMethod]
        public void TestHtml2TextWithData()
        {
            var c = new Converter { UsePlainTextConverter = UsePlainTextConverters.SendAndReceive };
            string input = (TestContext.DataRow["Input"] as string);
            string output = (TestContext.DataRow["Output"] as string);
            Boolean areEqual = TestContext.DataRow["Equal"].Equals(true);

            c.AutoLineWrap = (int)(TestContext.DataRow["AutoLineWrap"]);

            var a = new Article { Body = input, ContentType = null };
            c.NewArticleFromWebService(a, Encoding.UTF8);
            if (areEqual)
            {
                Assert.AreEqual(output, a.Body);
            }
            else
            {
                Assert.AreNotEqual(output, a.Body);
            }
        }
    }
}
