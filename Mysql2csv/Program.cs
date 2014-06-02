using System;
using System.Collections.Generic;
using System.IO;
using System.Data;
using System.Linq;
using System.Text;
using MySql.Data.MySqlClient;

namespace synesis
{
	class Program
	{
		static string URL = string.Empty;
		static string Logfile;
		static string Inifile;
		static IList<string> LogContent = new List<string>();
		static private MySqlConnection conn;

		static void Main(string[] args)
		{
			try
			{
				string Profile = (args.Length > 0) ? args[0] : "default";
				Console.WriteLine(Profile);
				Logfile = Profile + "_mysql.log";
				Inifile = Profile + ".ini";

				if (File.Exists(Inifile) == false)
					throw new ArgumentException("Profile is not found - " + Inifile);

				Props prop = new Props();
				prop.Load(Inifile);

				URL = prop.Get("url");
				string CSV = prop.Get("csv");
				string SQL = prop.Get("sql");

				//test settings
				if (URL == string.Empty)
					throw new ArgumentException(@"Setting 'url' is not found in profile");
				if (CSV == string.Empty)
					throw new ArgumentException(@"Setting 'csv' is not found in profile");
				if (SQL == string.Empty)
					throw new ArgumentException(@"Setting 'sql' is not found in profile");
				if (File.Exists(SQL) == false)
					throw new ArgumentException("SQL is not found - " + SQL);

				//load sql
				string[] sql = File.ReadAllLines(SQL, Encoding.UTF8);
				Log("SQL is used - " + SQL);

				// connect to db
				Connect(true);
				Log("Connected - " + conn.State.ToString());

				//get data
				List<String> output = new List<String>();
				StringBuilder sb = new StringBuilder();

				string sql_string = String.Join(" ", sql);
				sql_string = SqlSpecial(sql_string);


				Connect(true);
				MySqlCommand cmd = new MySqlCommand(sql_string, conn);
				MySqlDataReader reader = cmd.ExecuteReader();
				Log("Records - " + reader.HasRows);
				int FieldCount = reader.FieldCount;
				int OutputCount = 0;
				while (reader.Read())
				{
					sb.Length = 0;
					for (int i = 0; i < FieldCount; i++)
					{
						sb.Append(reader.GetString(i).Trim());
						if (i < FieldCount - 1)
							sb.Append(";");
					}//for
					output.Add(sb.ToString());
					OutputCount++;
				}//while

				Log("Records output - " + OutputCount);
				reader.Close();

				File.WriteAllLines(CSV, output.ToArray());
			}//try
			catch (Exception e)
			{
				Log(e.Message);
			}//catch
			finally
			{
				Connect(false);
				Log("=========", true);
			}//finally
		}//function

		private static string SqlSpecial(String sSql)
		{
			string Ret = sSql;
			int iStart;
			int iEnd;
			string sSpec;

			sSpec = "#DEPARTM_";
			iStart = sSql.IndexOf(sSpec);
			if (iStart > 0)
			{
				iStart += sSpec.Length;
				iEnd = sSql.IndexOf("#", iStart);
				if (iEnd < 0)
				{
					Log("iEnd - not found");
					return Ret;
				}//if

				string DepName = sSql.Substring(iStart, iEnd - iStart);
				Log("DepName - " + DepName);

				List<int> lst = GetDepartmId(DepName, null);
				lst.Sort();
				Ret = sSql.Replace(sSpec + DepName + "#", ToCommaSeparated(lst, "0"));
				//lstMain.AddRange(lst);
				Log("SQL - " + Ret);
			}//if
			return Ret;
		}//function

		private static List<int> GetDepartmId(string sDepName, IList<int> parents)
		{
			List<int> Ret = new List<int>();
			List<int> ids = new List<int>();
			int PCount = 0;
			if (parents != null)
				PCount = parents.Count();


			string sSql = string.Empty;
			if (sDepName.Length > 0)
			{
				sSql = "select DEPWPID from departm where name = \'" + sDepName + "\'";
			}//if
			else
			{
				sSql = "select DEPWPID from departm where PARENTID in (" + ToCommaSeparated(parents, "0") + ")";
			}//else

			Connect(true);
			MySqlCommand cmd = new MySqlCommand(sSql, conn);
			MySqlDataReader reader = cmd.ExecuteReader();
			while (reader.Read())
			{
				ids.Add(reader.GetInt32(0));
			}//while
			Connect(false);

			Ret.AddRange(ids);
			if (Ret.Count() != PCount)
			{
				Ret.AddRange(GetDepartmId(string.Empty, ids));
			}//if

			return Ret;
		}//function

		public static string ToCommaSeparated(IList<int> ids, string Default)
		{
			if (ids == null)
				return Default;
			if (ids.Any() == false)
				return Default;


			return string.Join(",", ids.Distinct().Select(i => i.ToString()).ToArray());
		}//function

		public static void Log(String msg)
		{
			LogContent.Add(msg);
		}//function

		public static void Log(String msg, bool WithFlash)
		{
			LogContent.Add(msg);
			if (WithFlash)
				File.WriteAllLines(Logfile, LogContent.ToArray());
		}//function

		private static void Connect(bool Open)
		{
			if (Open)
			{
				conn = conn ?? new MySqlConnection(URL);
				if (conn.State != ConnectionState.Open)
					conn.Open();
			}//if
			else
			{
				if (conn != null && conn.State != ConnectionState.Closed)
					conn.Close();
			}//else
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
