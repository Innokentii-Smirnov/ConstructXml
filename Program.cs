using System;
using System.IO;
using System.Xml;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace XmlConstruction
{
	static class Program
	{
		const string source = "input";
		const string target = "output";
		static void Main()
		{
			XmlConstructor.ConstructDirectory(source, target);
		}
	}
	public static class XmlConstructor
	{
		const string templateXml = "template.xml";
        const string punctPos = "PUNCT";
        static Regex tokenBoundary = new Regex(@"\s+|(?<=\w)(?=\W)|(?<=\W)(?=\w)|(?<=\W)(?=\W)");
        static Regex punct = new Regex(@"\W");
		public static void ConstructDirectory(string source, string target)
		{
			List<string> directories = new List<string>(Directory.GetDirectories(source, "*", SearchOption.AllDirectories));
            directories.Add(source);
			foreach (string directory in directories)
			{
				foreach (string infile in Directory.GetFiles(directory))
				{
					Console.WriteLine(infile);
					string outfileName = Path.GetFileNameWithoutExtension(infile) + ".xml";
					string outfile = Path.Combine(directory.Replace(source, target), outfileName);
					ConstructFile(infile, outfile);
				}
			}
		}
		public static void ConstructFile(string infile, string outfile)
		{
			XmlDocument doc = new XmlDocument();
			using (StreamReader sr = new StreamReader(templateXml))
			{
				doc.Load(sr);
			}
			XmlNode publ = doc.GetElementsByTagName("AO:TxtPubl")[0];
			string textName = Path.GetFileNameWithoutExtension(infile);
            publ.RemoveAll();
			publ.SetContent(textName.Escape());
			XmlNode text = doc.GetElementsByTagName("text")[0];
			using (StreamReader sr = new StreamReader(infile))
			{
				int i = 0;
				string line;
				while ((line = sr.ReadLine()) != null)
				{
                    string lineText = line.Trim();
                    if (lineText != String.Empty)
                    {
                        string id = i.ToString();
                        i++;
                        ConstructLineBreak(text, textName, id);
                        IEnumerable<string> words = from word in tokenBoundary.Split(lineText)
                                            where word != String.Empty
                                            select word;
                        foreach (string word in words)
                        {
                            ConstructWord(text, word);
                        }
                    }
				}
			}
			string directory = Path.GetDirectoryName(outfile);
			if (!Directory.Exists(directory))
			{
				Directory.CreateDirectory(directory);
			}
			using (StreamWriter sw = new StreamWriter(outfile))
			{
				doc.Save(sw);
			}
		}
		private static void ConstructLineBreak(XmlNode text, string txtid, string lnr)
		{
            Dictionary<string, string> attributes = new Dictionary<string, string>()
            {
              {"txtid", txtid},
              {"lnr", lnr},
              {"lg", "Hur"}
            };
			text.CreateChild("lb", attributes);
		}
		private static void ConstructWord(XmlNode text, string word)
		{
            string placeholderAnalysis;
            bool isHit = false;
            if (punct.IsMatch(word))
            {
              placeholderAnalysis = String.Format(" {0} @ @ @ {1} @ ", word, punctPos);
              isHit = true;
            }
            else
            {
              placeholderAnalysis = String.Format(" {0} @ @ @ @ ", word.ToLower());
            }
            Dictionary<string, string> attributes = new Dictionary<string, string>()
            {
              {"trans", ""},
              {"mrp0sel", ""},
              {"mrp1", placeholderAnalysis},
              {"firstAnalysisIsPlaceholder", "true"}
            };
            if (isHit)
            {
              attributes.Add("lg", "Hit");
            }
            XmlNode wordElement = text.CreateChild("w", attributes);
            wordElement.SetContent(word.ToLower());
		}
		private static void SetContent(this XmlNode node, string word)
		{
            XmlText text = node.OwnerDocument.CreateTextNode(word);
            node.AppendChild(text);
		}
		private static void AddAttribute(this XmlNode node, string name, string value)
		{
			XmlAttribute attribute = node.OwnerDocument.CreateAttribute(name);
			attribute.Value = value;
			node.Attributes.Append(attribute);
		}
		private static void AddAttributes(this XmlNode node, Dictionary<string, string> attrs)
		{
			foreach (KeyValuePair<string, string> pair in attrs)
			{
				node.AddAttribute(pair.Key, pair.Value);
			}
		}
		private static XmlNode CreateChild(this XmlNode node, string name)
		{
			XmlNode child = node.OwnerDocument.CreateElement(name);
			node.AppendChild(child);
            return child;
		}
		private static XmlNode CreateChild(this XmlNode node, string name, string attr, string value)
		{
			XmlNode child = node.OwnerDocument.CreateElement(name);
			child.AddAttribute(attr, value);
			node.AppendChild(child);
            return child;
		}
		private static XmlNode CreateChild(this XmlNode node, string name, Dictionary<string, string> attrs)
		{
			XmlNode child = node.OwnerDocument.CreateElement(name);
			child.AddAttributes(attrs);
			node.AppendChild(child);
            return child;
		}
		private static string Escape(this string s)
        {
          s = s.Replace("&", "-");
          s = s.Replace("<", "(");
          s = s.Replace(">", ")");
          s = s.Replace("\"", String.Empty);
          s = s.Replace("'", String.Empty);
          return s;
        }
	}
}
