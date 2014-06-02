using System;
using System.Collections.Generic;
using System.IO;
using Google.GData.Client;
using Google.GData.Spreadsheets;

namespace synesis
{
	class Program
	{
		static string Logfile;
		static string Inifile;
		static List<string> Log = new List<string>();

		static SpreadsheetsService service = null;
		static string[] ssCOLUMNS = null;

		static void Main(string[] args)
		{
			try
			{
				string Profile = (args.Length > 0) ? args[0] : "default";
				Console.WriteLine(Profile);
				Logfile = Profile + "_google.log";
				Inifile = Profile + ".ini";

				if (File.Exists(Inifile) == false)
					throw new ArgumentException("Profile is not found - " + Inifile);

				Props prop = new Props();
				prop.Load(Inifile);

				#region get settings
				string LOGIN = prop.Get("login");
				string PASSWORD = prop.Get("password");
				string SPREADSHEET = prop.Get("spreadsheet");
				string WORKSHEET = prop.Get("worksheet");
				string CSV = prop.Get("csv");
				string COLUMNS = prop.Get("columns");
				#endregion

				#region test settings
				if (LOGIN == string.Empty)
					throw new ArgumentException(@"Setting 'login' is not found in profile");
				if (PASSWORD == string.Empty)
					throw new ArgumentException(@"Setting 'password' is not found in profile");
				if (SPREADSHEET == string.Empty)
					throw new ArgumentException(@"Setting 'spreadsheet' is not found in profile");
				if (WORKSHEET == string.Empty)
					throw new ArgumentException(@"Setting 'worksheet' is not found in profile");
				if (CSV == string.Empty)
					throw new ArgumentException(@"Setting 'csv' is not found in profile");
				if (COLUMNS == string.Empty)
					throw new ArgumentException(@"Setting 'columns' is not found in profile");
				if (File.Exists(CSV) == false)
					throw new ArgumentException("CSV is not found - " + Inifile);
				#endregion

				#region spreadsheet get
				service = new SpreadsheetsService("SynesisIntegration-v1");
				service.setUserCredentials(LOGIN, PASSWORD);

				SpreadsheetEntry spreadsheet = null;
				SpreadsheetFeed feed = service.Query(new SpreadsheetQuery());
				foreach (SpreadsheetEntry item in feed.Entries)
				{
					if (item.Title.Text == SPREADSHEET)
					{
						Log.Add("SPREADSHEET is found - " + SPREADSHEET);
						spreadsheet = item;
						//spreadsheet.SaveToXml(new FileStream("spreadsheet.xml", FileMode.Create));
						break;
					}//if
				}//for
				if (spreadsheet == null)
					throw new ArgumentException("SPREADSHEET is not found - " + SPREADSHEET);
				#endregion

				#region worksheet get
				WorksheetEntry worksheet = null;
				WorksheetFeed wsFeed = spreadsheet.Worksheets;
				foreach (WorksheetEntry entry in wsFeed.Entries)
				{
					if (entry.Title.Text == WORKSHEET)
					{
						worksheet = entry;
						Log.Add("WORKSHEET is found - " + WORKSHEET);
						break;
					}//if
				}//for

				if (worksheet == null)
				{
					Log.Add("WORKSHEET is not found - " + WORKSHEET);
					worksheet = new WorksheetEntry();
					worksheet.Title.Text = WORKSHEET;
					service.Insert(wsFeed, worksheet);
					Log.Add("WORKSHEET is added - " + WORKSHEET);
				}//if
				#endregion

				//CLEAR worksheet
				worksheet.Rows = 1;
				worksheet.Update();

				// Fetch the list feed of the worksheet.
				AtomLink listFeedLink = worksheet.Links.FindService(GDataSpreadsheetsNameTable.ListRel, null);
				ListQuery listQuery = new ListQuery(listFeedLink.HRef.Content);
				ListFeed listFeed = service.Query(listQuery);

				//delimiter on extension
				char Delim = ';';
				if (CSV.EndsWith(".tsv"))
					Delim = '\t';

				//columns
				ssCOLUMNS = COLUMNS.Split(';');
				for (int i = 0; i < ssCOLUMNS.Length; i++)
					ssCOLUMNS[i] = ssCOLUMNS[i].ToLower();

				#region input rows
				string[] input = File.ReadAllLines(CSV);
				Log.Add("CSV " + CSV + " has " + input.Length + " items");
				int CountAdded = 0;
				foreach (string s in input)
				{
					if (AddRow(listFeed, s.Split(Delim)))
						CountAdded++;
					else
						Log.Add("Row is not compatible - " + s);
				}//for
				Log.Add("Rows inserted - " + CountAdded.ToString());
				#endregion

			}//try
			catch (Exception e)
			{
				Log.Add(e.Message);
			}//catch
			finally
			{
				File.WriteAllLines(Logfile, Log.ToArray());
			}//finally
		}//function

		static bool AddRow(ListFeed feed, string[] content)
		{
			if (content.Length != ssCOLUMNS.Length)
				return false;

			ListEntry row = new ListEntry();
			//fill row from content
			for (int i = 0; i < ssCOLUMNS.Length; i++)
			{
				row.Elements.Add(new ListEntry.Custom() { LocalName = ssCOLUMNS[i], Value = content[i] });
				//Log.Add("debug " + row.ToString());
			}//for

			//insert row
			service.Insert(feed, row);

			return true;
		}//function
	}//class


	public class Props
	{
		Dictionary<string, string> data = new Dictionary<string, string>();

		public void Load(string FileName)
		{
			int i;
			foreach (var s in File.ReadAllLines(FileName))
			{
				i = s.IndexOf("="); //as=ert
				if (i <= 0)
					continue;

				data.Add(s.Substring(0, i), s.Substring(i + 1));
			}//for
		}//function

		public string Get(string Key)
		{
			if (data.ContainsKey(Key))
				return data[Key];
			else
				return string.Empty;
		}//function
	}//class
}//ns