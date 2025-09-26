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
		private readonly static char[] sep = new char[] {' '};
		public static void ConstructFile(string infile, string outfile)
		{
			XmlDocument doc = new XmlDocument();
			using (StreamReader sr = new StreamReader(templateXml))
			{
				doc.Load(sr);
			}
			XmlNode publ = doc.GetElementsByTagName("AO:TxtPubl")[0];
			string textName = Path.GetFileNameWithoutExtension(infile);
			publ.InnerXml = SecurityElement.Escape(textName);
			XmlNode text = doc.GetElementsByTagName("text")[0];
			using (StreamReader sr = new StreamReader(infile))
			{
				int i = 0;
				string line;
				while ((line = sr.ReadLine()) != null)
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
					ConstructLineBreak(doc, text, textName, id);
					string[] words = lineText.Split(sep, StringSplitOptions.RemoveEmptyEntries);
					foreach (string word in words)
					{
						ConstructWord(doc, text, word);
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
		private static void ConstructLineBreak(XmlDocument doc, XmlNode text, string txtid, string lnr)
		{
			XmlElement lineElement = doc.CreateElement("lb");
			lineElement.AddAttribute("txtid", txtid);
			lineElement.AddAttribute("lnr", lnr);
			lineElement.AddAttribute("lg", "Hur");
			text.AppendChild(lineElement);
		}
		private static void ConstructWord(XmlDocument doc, XmlNode text, string word)
		{
			XmlElement wordElement = doc.CreateElement("w");
			wordElement.SetContent(word);
			XmlAttribute transAttribute = doc.CreateAttribute("trans");
			transAttribute.Value = "";
			wordElement.Attributes.Append(transAttribute);
			XmlAttribute mrp0selAttribute = doc.CreateAttribute("mrp0sel");
			mrp0selAttribute.Value = "";
			wordElement.Attributes.Append(mrp0selAttribute);
			text.AppendChild(wordElement);
			bool isHit = false;
			foreach(XmlNode childNode in wordElement.ChildNodes)
			{
				if (childNode.Name == "sGr")
				{
					isHit = true;
					break;
				}
			}
			if (isHit)
			{
				XmlAttribute langAttriubte = doc.CreateAttribute("lg");
				langAttriubte.Value = "Hit";
				wordElement.Attributes.Append(langAttriubte);
			}
		}
		const string rasur = "(Rasur)";
		private static HashSet<char> vowels = new HashSet<char>() {'a', 'e', 'i', 'u'};
		private static void SetContent(this XmlNode node, string word)
		{
			int i = 0;
			bool rasurOpen = false;
			string current = "";
			while (i < word.Length)
			{
				char character = word[i];
				char prev;
				string name;
				switch (character)
				{
					case '<':
						name = "laes_in";
						break;
					case '>':
						name = "laes_fin";
						break;
					case '〈':
						name = "laes_in";
						break;
					case '〉':
						name = "laes_fin";
						break;
					case '[':
						name = "del_in";
						break;
					case ']':
						name = "del_fin";
						break;
					case '⸢':
						name = "laes_in";
						break;
					case '⸣':
						name = "laes_fin";
						break;
					case '*':
						if (rasurOpen)
						{
							name = "ras_fin";
							rasurOpen = false;
						}
						else
						{
							name = "ras_in";
							rasurOpen = true;
						}
						break;
					default:
						name = null;
						break;
				}
				if (name != null)
				{
					if (current != "")
					{
						XmlText text = node.OwnerDocument.CreateTextNode(current);
						node.AppendChild(text);
						current = "";
					}
					XmlNode child = node.OwnerDocument.CreateElement(name);
					node.AppendChild(child);
				}
				else if (character == '°')
				{
					if (current != "")
					{
						XmlText text = node.OwnerDocument.CreateTextNode(current);
						node.AppendChild(text);
						current = "";
					}
					int start = i + 1;
					int end = word.IndexOf('°', start);
					string det = word.Substring(start, end - start);
					XmlNode child = node.OwnerDocument.CreateElement("d");
					child.InnerXml = det;
					node.AppendChild(child);
					i = end;
				}
				else if (char.IsUpper(character) && i + 1 < word.Length && word[i+1].IsSumeric())
				{
					if (current != "")
					{
						XmlText text = node.OwnerDocument.CreateTextNode(current);
						node.AppendChild(text);
						current = "";
					}
					int start = i;
					int end = i + 2;
					while (end < word.Length && word[end].IsSumeric())
					{
						end++;
					}
					string sumerogram = word.Substring(start, end - start);
					XmlNode child = node.OwnerDocument.CreateElement("sGr");
					child.InnerXml = sumerogram;
					node.AppendChild(child);
					i = end - 1;
				}
				else if (word.HasSubstring(rasur, i))
				{
					if (current != "")
					{
						XmlText text = node.OwnerDocument.CreateTextNode(current);
						node.AppendChild(text);
						current = "";
					}
					XmlNode child = node.OwnerDocument.CreateElement("gap");
					XmlAttribute attr = node.OwnerDocument.CreateAttribute("c");
					attr.Value = rasur;
					child.Attributes.Append(attr);
					node.AppendChild(child);
					i += rasur.Length - 1;
				}
				else if (i >= 1 && vowels.Contains(prev = word[i-1]) &&
						(prev == character || prev == 'u' && character == 'ú'))
				{
					if (current != "")
					{
						XmlText text = node.OwnerDocument.CreateTextNode(current);
						node.AppendChild(text);
						current = "";
					}
					XmlNode child = node.OwnerDocument.CreateElement("subscr");
					XmlAttribute attr = node.OwnerDocument.CreateAttribute("c");
					attr.Value = character.ToString();
					child.Attributes.Append(attr);
					node.AppendChild(child);
				}
				else if (character == '?' || character == '!')
				{
					if (current != "")
					{
						XmlText text = node.OwnerDocument.CreateTextNode(current);
						node.AppendChild(text);
						current = "";
					}
					node.CreateChild("corr", "c", character.ToString());
				}
				else if (char.IsDigit(character))
				{
					if (current != "")
					{
						XmlText text = node.OwnerDocument.CreateTextNode(current);
						node.AppendChild(text);
						current = "";
					}
					int start = i;
					int end = i + 1;
					while (end < word.Length && char.IsDigit(word[end]))
					{
						end++;
					}
					string noteId = word.Substring(start, end - start);
					node.CreateChild(
						"note",
						new Dictionary<string, string>() {{"n", noteId}, {"c", "Missing note " + noteId}}
					);
					i = end - 1;
				}
				else
				{
					current = current + character;
				}
				i++;
			}
			if (current != "")
			{
				XmlText text = node.OwnerDocument.CreateTextNode(current);
				node.AppendChild(text);
				current = "";
			}
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
	}
}
