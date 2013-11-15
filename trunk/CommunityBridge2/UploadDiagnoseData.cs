using System;
using System.Collections;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Text;

namespace CommunityBridge2
{
  internal class UploadDiagnoseData
  {
    /// <summary>
    /// Sends the specified zip-file to the server and returns an error code or "null" if sucessfull!
    /// </summary>
    /// <param name="zipFile"></param>
    /// <param name="unhandledException"> </param>
    /// <param name="userEmail"> </param>
    /// <param name="userComment"> </param>
    /// <param name="userSendEmail"> </param>
    /// <returns></returns>
    /// <remarks>
    /// See also: http://technet.rapaport.com/Info/LotUpload/SampleCode/Full_Example.aspx
    /// </remarks>
    public static string DoIt(string zipFile, string unhandledException, string userEmail, string userComment, bool userSendEmail)
    {
      try
      {
        var formData = new NameValueCollection();
        const string url = "http://communitybridge.kalmbach.eu/diag/logupload.php";

        string boundary = "----------------------------" + DateTime.Now.Ticks.ToString("x");
        var webRequest = WebRequest.Create(url);

        webRequest.Method = "POST";
        webRequest.ContentType = "multipart/form-data; boundary=" + boundary;

        string filePath = zipFile; //  "C:\\a.zip";
        formData["upload"] = "1";
        formData["auto"] = "1";
        formData["version"] = UserSettings.ProductNameWithVersion;
        formData["userSendEmail"] = userSendEmail.ToString(System.Globalization.CultureInfo.InvariantCulture);
        if (string.IsNullOrEmpty(userEmail) == false)
          formData["userEmail"] = userEmail;
        if (string.IsNullOrEmpty(userComment) == false)
          formData["userComment"] = userComment;
        if (string.IsNullOrEmpty(unhandledException) == false)
          formData["unhandledException"] = unhandledException;

        Stream postDataStream = GetPostStream(filePath, formData, boundary);

        webRequest.ContentLength = postDataStream.Length;
        Stream reqStream = webRequest.GetRequestStream();

        postDataStream.Position = 0;

        byte[] buffer = new byte[1024];
        int bytesRead = 0;

        while ((bytesRead = postDataStream.Read(buffer, 0, buffer.Length)) != 0)
        {
          reqStream.Write(buffer, 0, bytesRead);
        }

        postDataStream.Close();
        reqStream.Flush();
        reqStream.Close();

        var sr = new StreamReader(webRequest.GetResponse().GetResponseStream());
        string result = sr.ReadToEnd();
        if (string.Equals(result, "OK", StringComparison.OrdinalIgnoreCase))
        {
          return null;
        }
        return result;
      }
      catch (Exception exp)
      {
        return "EXCEPTION: " + exp.Message;
      }
    }

    private static Stream GetPostStream(string filePath, NameValueCollection formData, string boundary)
    {
      Stream postDataStream = new System.IO.MemoryStream();

      //adding file data
      string fileHeaderTemplate = Environment.NewLine + "--" + boundary + Environment.NewLine +
                                  "Content-Disposition: form-data; name=\"{0}\"; filename=\"{1}\"" +
                                  Environment.NewLine + "Content-Type: application/x-zip-compressed" +
                                  Environment.NewLine +
                                  Environment.NewLine;


      var fileInfo = new FileInfo(filePath);
      byte[] fileHeaderBytes = System.Text.Encoding.UTF8.GetBytes(
        string.Format(fileHeaderTemplate, "NeueListe", fileInfo.FullName));
      postDataStream.Write(fileHeaderBytes, 0, fileHeaderBytes.Length);
      FileStream fileStream = fileInfo.OpenRead();
      byte[] buffer = new byte[1024];
      int bytesRead = 0;
      while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) != 0)
      {
        postDataStream.Write(buffer, 0, bytesRead);
      }
      fileStream.Close();

      //adding form data
      string formDataHeaderTemplate = Environment.NewLine + "--" + boundary + Environment.NewLine +
                                      "Content-Disposition: form-data; name=\"{0}\";" + Environment.NewLine +
                                      Environment.NewLine + "{1}";

      foreach (string key in formData.Keys)
      {
        byte[] formItemBytes = Encoding.UTF8.GetBytes(string.Format(formDataHeaderTemplate,
                                                                                key, formData[key]));
        postDataStream.Write(formItemBytes, 0, formItemBytes.Length);
      }

      byte[] endBoundaryBytes = Encoding.UTF8.GetBytes(Environment.NewLine + "--" + boundary + "--");
      postDataStream.Write(endBoundaryBytes, 0, endBoundaryBytes.Length);

      return postDataStream;
    }
  }
}
