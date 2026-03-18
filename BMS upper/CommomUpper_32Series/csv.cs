using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.IO;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using System.Threading;


namespace WindowsFormsApplication2
{
    class csv
    {
        private csv()
        {
        }
        //write a new file, existed file will be overwritten
        public static void WriteCSV(string filePathName, ushort line, string[][] ls)
        {
            WriteCSV(filePathName, false, line, ls);
        }
        //write a file, existed file will be overwritten if append = false
        public static void WriteCSV(string filePathName, bool append, ushort line, string[][] ls)
        {
            StreamWriter fileWriter = new StreamWriter(filePathName, append, Encoding.Default);
            for (int j = 0; j < line; j++)
                fileWriter.WriteLine(String.Join(",", ls[j]));
            fileWriter.Flush();
            fileWriter.Close();
            // MessageBox.Show("CSV file " + filePathName + " saved", "Information");
        }

        public static List<String[]> ReadCSV(string filePathName)
        {
            List<String[]> ls = new List<String[]>();
            StreamReader fileReader = new StreamReader(filePathName);
            string strLine = "";
            while (strLine != null)
            {
                strLine = fileReader.ReadLine();
                if (strLine != null && strLine.Length > 0)
                {
                    ls.Add(strLine.Split(','));
                    //Debug.WriteLine(strLine);
                }
            }
            fileReader.Close();
            return ls;
        }
    }
}


/*
using System;  
using System.Collections.Generic;  
using System.IO;  
using System.Text;

namespace WindowsFormsApplication2
{  
    /// <summary>  
    /// CSVUtil is a helper class handling csv files.  
    /// </summary>  
    public class CSVUtil  
    {  
        private CSVUtil()  
        {  
        }  
        //write a new file, existed file will be overwritten  
        public static void WriteCSV(string filePathName,List<String[]>ls)  
        {  
            WriteCSV(filePathName,false,ls);  
        }  
        //write a file, existed file will be overwritten if append = false  
        public static void WriteCSV(string filePathName,bool append, List<String[]> ls)  
        {  
            StreamWriter fileWriter=new StreamWriter(filePathName,append,Encoding.Default);  
            foreach(String[] strArr in ls)  
            {  
                fileWriter.WriteLine(String.Join(",",strArr));  
            }  
            fileWriter.Flush();  
            fileWriter.Close();  
              
        }  
        public static List<String[]> ReadCSV(string filePathName)  
        {  
            List<String[]> ls = new List<String[]>();  
            StreamReader fileReader=new   StreamReader(filePathName);    
            string strLine="";  
            while (strLine != null)  
            {  
                strLine = fileReader.ReadLine();  
                if (strLine != null && strLine.Length>0)  
                {  
                    ls.Add(strLine.Split(','));  
                    //Debug.WriteLine(strLine);  
                }  
            }   
            fileReader.Close();  
            return ls;  
        }  
          
    }  
}  
*/