using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace synesis
{
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

				data.Add(s.Substring(0, i), s.Substring(i+1));
			}//for
		}//function
      
		public string Get(string Key)
		{
			if (data.ContainsKey(Key))
				return data[Key];
			else
				return string.Empty;
		}//function

		public string Get(string Key, string Default)
		{
			if (data.ContainsKey(Key))
				return data[Key];
			else
				return Default;
		}//function

	}//class
}//ns
