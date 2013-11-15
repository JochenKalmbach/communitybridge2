using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;

namespace CommunityBridge2
{
  public static class IniHelper
  {
    [DllImport("KERNEL32.DLL", EntryPoint = "GetPrivateProfileStringW", SetLastError = true, CharSet = CharSet.Unicode, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
    private static extern int GetPrivateProfileString(string lpAppName, string lpKeyName, string lpDefault, StringBuilder lpReturnString, int nSize, string lpFilename);

    [DllImport("KERNEL32.DLL", EntryPoint = "WritePrivateProfileStringW", SetLastError = true, CharSet = CharSet.Unicode, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
    private static extern int WritePrivateProfileString(string lpAppName, string lpKeyName, string lpString, string lpFilename);

    [DllImport("KERNEL32.DLL", EntryPoint = "GetPrivateProfileSectionW", SetLastError = true, CharSet = CharSet.Unicode, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
    private static extern uint GetPrivateProfileSection(string lpAppName, IntPtr lpReturnString, uint nSize, string lpFilename);

    [DllImport("KERNEL32.DLL", EntryPoint = "GetPrivateProfileSectionNamesW", SetLastError = true, CharSet = CharSet.Unicode, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
    private static extern uint GetPrivateProfileSectionNames(IntPtr lpszReturnBuffer, uint nSize, string lpFileName);

    public static string GetString(string section, string key, string iniFile)
    {
      var sb = new StringBuilder(1024);
      // TODO: Erlaube größere Texte als 1024 Zeichen... siehe Doku zu "GetPrivateProfileString"
      GetPrivateProfileString(section, key, string.Empty, sb, sb.Capacity, iniFile);
      return sb.ToString();
    }
    public static int? GetInt32(string section, string key, string iniFile)
    {
      string s = GetString(section, key, iniFile);
      int res;
      if (int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out res))
        return res;
      return null;
    }
    public static DateTime? GetDateTime(string section, string key, string iniFile)
    {
      string s = GetString(section, key, iniFile);
      DateTime res;
      if (DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out res))
        return res;
      return null;
    }

    public static bool SetString(string section, string key, string value, string iniFile)
    {
      return WritePrivateProfileString(section, key, value, iniFile) >= 0;
    }
    public static bool SetInt32(string section, string key, int value, string iniFile)
    {
      return WritePrivateProfileString(section, key, value.ToString(CultureInfo.InvariantCulture), iniFile) >= 0;
    }
    public static bool SetDateTime(string section, string key, DateTime value, string iniFile)
    {
      return WritePrivateProfileString(section, key, value.ToString("o", CultureInfo.InvariantCulture), iniFile) >= 0;
    }


    public static string[] GetSectionNamesFromIni(string iniFile)
    {
      // TODO: Erlaube größere Texte als 1024 Zeichen... siehe Doku zu "GetPrivateProfileSectionNames"
      const uint maxBuffer = 32767;
      IntPtr pReturnedString = Marshal.AllocCoTaskMem((int)maxBuffer);
      uint bytesReturned = GetPrivateProfileSectionNames(pReturnedString, (int)maxBuffer * sizeof(char), iniFile);
      if (bytesReturned == 0)
      {
        Marshal.FreeCoTaskMem(pReturnedString);
        return null;
      }
      string local = Marshal.PtrToStringUni(pReturnedString, (int)bytesReturned);
      Marshal.FreeCoTaskMem(pReturnedString);
      //use of Substring below removes terminating null for split
      return local.Substring(0, local.Length - 1).Split('\0');
    }

    public static bool GetSectionFromIni(string section, string iniFileName, out string[] keys)
    {
      keys = null;

      if (!System.IO.File.Exists(iniFileName))
      {
        keys = new string[0];
        return false;
      }

      // Ermittle immer den ganzen Pfad, sonst wird womöglich nur in system32 nachgeschaut
      string fn = System.IO.Path.GetFullPath(iniFileName);

      IntPtr pReturnedString;
      uint bufSize = 32767;
      bool bufferTooSmal;
      uint bytesReturned;
      do
      {
        bufferTooSmal = false;
        pReturnedString = Marshal.AllocCoTaskMem((int)bufSize * sizeof(char));
        bytesReturned = GetPrivateProfileSection(section, pReturnedString, bufSize, fn);

        if (bytesReturned == 0)
        {
          Marshal.FreeCoTaskMem(pReturnedString);
          keys = new string[0];
          return false;
        }

        if (bytesReturned == bufSize - 2)
        {
          // Buffer too small
          Marshal.FreeCoTaskMem(pReturnedString);
          bufSize *= 4;  // vergrößere den Buffer und probiere es nochmals
          bufferTooSmal = true;
        }
      } while (bufferTooSmal);

      //bytesReturned -1 to remove trailing \0
      // NOTE: Calling Marshal.PtrToStringAuto(pReturnedString) will 
      //       result in only the first pair being returned
      string returnedString = Marshal.PtrToStringUni(pReturnedString, (int)bytesReturned - 1);

      keys = returnedString.Split('\0');

      Marshal.FreeCoTaskMem(pReturnedString);
      return true;
    }
  }
}
