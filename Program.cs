using System;
using System.IO;
using System.Xml;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Security;

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
			publ.InnerXml = textName.Escape();
			XmlNode text = doc.GetElementsByTagName("text")[0];
			using (StreamReader sr = new StreamReader(infile))
			{
				int i = 0;
				string line;
				while ((line = sr.ReadLine()) != null)
				{
                    if (line.Trim() != String.Empty)
                    {
                        string[] split = line.Split('\t');
                        string id, lineText;
                        if (split.Length == 2)
                        {
                            id = split[0];
                            lineText = split[1];
                        }
                        else if (split.Length == 1)
                        {
                            id = i.ToString();
                            i++;
                            lineText = line;
                        }
                        else
                        {
                            throw new ArgumentException(line);
                        }
                        ConstructLineBreak(text, textName, id);
                        IEnumerable<string> words = from word in tokenBoundary.Split(lineText)
                                            where word != String.Empty
                                            select word;
                        foreach (string word in words)
                        {
                            ConstructWord(doc, text, word);
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
		private static void ConstructWord(XmlDocument doc, XmlNode text, string word)
		{
			XmlElement wordElement = doc.CreateElement("w");
			wordElement.SetContent(word.ToLower());
			wordElement.AddAttribute("trans", "");
			wordElement.AddAttribute("mrp0sel", "");
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
            wordElement.AddAttribute("mrp1", placeholderAnalysis);
            wordElement.AddAttribute("firstAnalysisIsPlaceholder", "true");
			text.AppendChild(wordElement);

			if (isHit)
			{
				wordElement.AddAttribute("lg", "Hit");
			}
		}
		private static void SetContent(this XmlNode node, string word)
		{
            XmlText text = node.OwnerDocument.CreateTextNode(word);
            node.AppendChild(text);
		}
		private static bool IsSumeric(this char character)
		{
			return char.IsUpper(character) || character == '.';
		}
		private static bool HasSubstring(this string word, string s, int i)
		{
			return i + s.Length <= word.Length &&  word.Substring(i, s.Length) == s;
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
		private static void CreateChild(this XmlNode node, string name)
		{
			XmlNode child = node.OwnerDocument.CreateElement(name);
			node.AppendChild(child);
		}
		private static void CreateChild(this XmlNode node, string name, string attr, string value)
		{
			XmlNode child = node.OwnerDocument.CreateElement(name);
			child.AddAttribute(attr, value);
			node.AppendChild(child);
		}
		private static void CreateChild(this XmlNode node, string name, Dictionary<string, string> attrs)
		{
			XmlNode child = node.OwnerDocument.CreateElement(name);
			child.AddAttributes(attrs);
			node.AppendChild(child);
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
