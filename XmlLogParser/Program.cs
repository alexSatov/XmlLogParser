using System;
using System.Threading.Tasks;
using System.Xml;

namespace XmlLogParser
{
	public class Program
	{
		public static void Main(string[] args)
		{
			var tasks = new[]
			{
				Task.Run(() => ParseLogAsync("../../../export_13012020_01.html", "RegionPartnerLink")),
				Task.Run(() => ParseLogAsync("../../../export_13012020_02.html", "RegionPartnerLink")),
				Task.Run(() => ParseLogAsync("../../../export_14012020_01.html", "RegionPartnerLink")),
				Task.Run(() => ParseLogAsync("../../../export_14012020_02.html", "RegionPartnerLink")),
				Task.Run(() => ParseLogAsync("../../../export_15012020_01.html", "RegionPartnerLink")),
				Task.Run(() => ParseLogAsync("../../../export_15012020_02.html", "RegionPartnerLink")),
				Task.Run(() => ParseLogAsync("../../../export_16012020_01.html", "RegionPartnerLink")),
				Task.Run(() => ParseLogAsync("../../../export_16012020_02.html", "RegionPartnerLink"))
			};

			Task.WaitAll(tasks);
		}

		public static async Task ParseLogAsync(string filename, string tableName)
		{
			var outputFilename = filename.Insert(filename.LastIndexOf('/') + 1, tableName + "_");
			var readerSettings = new XmlReaderSettings { Async = true };
			var writerSettings = new XmlWriterSettings { Async = true };

			using (var logReader = XmlReader.Create(filename, readerSettings))
			using (var logWriter = XmlWriter.Create(outputFilename, writerSettings))
			{
				await logWriter.WriteRawAsync("<?xml version=\"1.0\" encoding=\"utf-16\"?>");
				await ReadNextElement(logReader);

				while (logReader.NodeType != XmlNodeType.None)
				{
					if (logReader.NodeType == XmlNodeType.Element && logReader.Name == "html")
					{
						await logWriter.WriteStartElementAsync(null, "html", null);
						await ReadHtmlAsync(logReader, logWriter, tableName);
						await logWriter.WriteEndElementAsync();
						continue;
					}

					var rawElement = await logReader.ReadOuterXmlAsync();
					await logWriter.WriteRawAsync(rawElement);
				}
			}
		}

		private static async Task ReadHtmlAsync(XmlReader reader, XmlWriter writer, string tableName)
		{
			await ReadNextElement(reader);

			while (reader.NodeType != XmlNodeType.None)
			{
				if (reader.NodeType == XmlNodeType.Element && reader.Name == "body")
				{
					await writer.WriteStartElementAsync(null, "body", null);
					await ReadBodyAsync(reader, writer, tableName);
					await writer.WriteEndElementAsync();
					continue;
				}

				var rawElement = await reader.ReadOuterXmlAsync();
				await writer.WriteRawAsync(rawElement);
			}
		}

		private static async Task ReadBodyAsync(XmlReader reader, XmlWriter writer, string tableName)
		{
			await ReadNextElement(reader);

			while (reader.NodeType != XmlNodeType.None)
			{
				if (reader.NodeType == XmlNodeType.Element && reader.Name == "table")
				{
					await writer.WriteStartElementAsync(null, "table", null);
					await writer.WriteAttributeStringAsync(null, "class", null, "operations");
					await writer.WriteAttributeStringAsync(null, "cellspacing", null, "0");
					await ReadOperationsTableAsync(reader, writer, tableName);
					await writer.WriteEndElementAsync();
					continue;
				}

				var rawElement = await reader.ReadOuterXmlAsync();
				await writer.WriteRawAsync(rawElement);
			}
		}

		private static async Task ReadOperationsTableAsync(XmlReader reader, XmlWriter writer, string tableName)
		{
			using (var tableReader = reader.ReadSubtree())
			{
				await ReadNextElement(tableReader);
				await CheckTrAsync(tableReader);

				while (tableReader.NodeType != XmlNodeType.None)
				{
					var headerRow = await tableReader.ReadOuterXmlAsync();
					if (!headerRow.Contains("Object"))
						continue;

					if (tableReader.Name != "tr")
						tableReader.ReadToNextSibling("tr");

					var dataRow = await tableReader.ReadOuterXmlAsync();

					if (tableReader.Name != "tr")
						tableReader.ReadToNextSibling("tr");

					var innerDataRow = await tableReader.ReadOuterXmlAsync();

					if (dataRow.Contains(tableName))
						await writer.WriteRawAsync(headerRow + dataRow + innerDataRow);
				}
			}

			reader.ReadEndElement();
		}

		private static async Task<bool> ReadNextElement(XmlReader reader)
		{
			while (await reader.ReadAsync())
			{
				if (reader.NodeType == XmlNodeType.Element)
					return true;
			}

			return false;
		}

		private static async Task CheckTrAsync(XmlReader reader)
		{
			if (reader.Name == "tr")
				return;

			await ReadNextElement(reader);

			if (reader.Name != "tr")
				throw new Exception("Only 'tr' elements expected");
		}
	}
}
