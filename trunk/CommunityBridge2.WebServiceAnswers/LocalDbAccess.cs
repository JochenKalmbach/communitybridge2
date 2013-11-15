using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace CommunityBridge2.WebServiceAnswers
{
  public class LocalDbAccess
  {
    static LocalDbAccess()
    {
      _FileCreateAppId = Guid.NewGuid();
    }

    private const string CeConnectionString =
      @"metadata=res://*/AnswersData.csdl|res://*/AnswersData.ssdl|res://*/AnswersData.msl;provider=System.Data.SqlServerCe.3.5;provider connection string=""Data Source=###DATABASE###;Max Database Size=4091""";

    private const string CeNativeConnectionString =
      @"provider=System.Data.SqlServerCe.3.5;provider connection string=""Data Source=###DATABASE###;Max Database Size=4091""";

    //private string _ConnectionStr;
    private string _BasePath;

    public LocalDbAccess(string basePath)
    {
      //const int dbVersionNumber = 1;

      // First: Check if the sdf.file is available
      basePath = Environment.ExpandEnvironmentVariables(basePath);

      if (Directory.Exists(basePath) == false)
        Directory.CreateDirectory(basePath);

      _BasePath = basePath;


      //using (var dc = CreateConnection())
      //{
      //  bool wrongVersion = true;
      //  try
      //  {
      //    var v = dc.DBVersionInfoSet.FirstOrDefault(p => true);
      //    if (v != null)
      //    {
      //      wrongVersion = v.Version != dbVersionNumber;
      //    }

      //    // try to save the active groups...
      //    // TODO: 
      //  }
      //  catch
      //  {
      //  }
      //  if (wrongVersion)
      //  {
      //    // INSERT INTO [DBVersionInfoSet] ([Id], [Version]) VALUES ('{383B3BD9-BF14-4AFB-B33C-2AD1486CC144}', 1)
      //    // GO


      //    // Database already exists...
      //    // TODO: Versioning...
      //    if (
      //        MessageBox.Show("Database-Version does not match! Please reinstall the application!",
      //                        "Warning", MessageBoxButton.OK) ==
      //        MessageBoxResult.Yes)
      //    {
      //      //// ... rebuild...
      //      //dc.DeleteDatabase();
      //      //CreateDatabaseWithVersion(dc, dbVersionNumber);

      //      //// TODO: Rebuild previously active groups...
      //    }
      //  }
      //}
    }

    private static Guid _FileCreateAppId;

    private string GetSqlCeDbFileName(string baseGroupName, string subName, bool createDatabaseIfItDoesNotExist = true)
    {
      string fn = Path.Combine(_BasePath, baseGroupName);
      if (Directory.Exists(fn) == false)
        Directory.CreateDirectory(fn);

      if (string.IsNullOrEmpty(subName))
        fn = Path.Combine(fn, "_Main.Group.sdf");
      else
        fn = Path.Combine(fn, subName + ".sdf");

      if (File.Exists(fn) == false)
      {
        //string[] names = this.GetType().Assembly.GetManifestResourceNames();
        if (createDatabaseIfItDoesNotExist == false)
          return null;

        // Here I need to use a mutex to prevent duplicate creating of the file... this leads to sharing violations...
        string mutexName = _FileCreateAppId.ToString("D", System.Globalization.CultureInfo.InvariantCulture) + baseGroupName.ToLowerInvariant();
        using (var m = new System.Threading.Mutex(false, mutexName))
        {
          m.WaitOne();
          try
          {
            if (File.Exists(fn) == false) // prüfe hier nochmals, da ich nur hier den Mutex besitze!
            {
              using (var db =
                typeof(AnswersDataEntities).Assembly.GetManifestResourceStream(
                  "CommunityBridge2.WebServiceAnswers.AnswersData.sdf"))
              {
                byte[] data = new byte[db.Length];
                db.Read(data, 0, data.Length);
                string fn2 = fn + "_tmp";
                using (var f = File.Create(fn2))
                {
                  f.Write(data, 0, data.Length);
                }
                File.Move(fn2, fn);
              }
            }
          }
          finally
          {
            m.ReleaseMutex();
          }
        }
      }
      return fn;
    }
    public System.Data.SqlServerCe.SqlCeConnection CreateSqlCeConnection(string baseGroupName, string subName, bool createDatabaseIfItDoesNotExist = true)
    {
      string fn = GetSqlCeDbFileName(baseGroupName, subName, createDatabaseIfItDoesNotExist);
      if (fn == null)
        return null;

      var conStr = CeNativeConnectionString.Replace("###DATABASE###", fn);

      return new System.Data.SqlServerCe.SqlCeConnection(conStr);
    }
    public AnswersDataEntities CreateConnection(string baseGroupName, string subName, bool createDatabaseIfItDoesNotExist = true)
    {
      string fn = GetSqlCeDbFileName(baseGroupName, subName, createDatabaseIfItDoesNotExist);
      if (fn == null)
        return null;

      var conStr = CeConnectionString.Replace("###DATABASE###", fn);

      return new AnswersDataEntities(conStr);
    }
  }
}
namespace Microsoft.Support.Community.DataLayer.Entity
{
  public partial class Thread
  {
    public Message[] MessageTemp;
  }
}

namespace CommunityBridge2.WebServiceAnswers
{
  public partial class Mapping
  {
    public object Tag;

    public DateTime CreatedDate { get; set; }

    public DateTime? ActivityDateUtc
    {
      get
      {
        if (ActivityDate.HasValue == false)
          return null;
        if (ActivityDate.Value.Kind == DateTimeKind.Utc)
          return ActivityDate.Value;
        // Convert it to UTC
        var dt = ActivityDate.Value;
        return new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second, dt.Millisecond, DateTimeKind.Utc);
      }
    }
  }
}
