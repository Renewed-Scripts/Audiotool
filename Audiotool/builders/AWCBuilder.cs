using System.IO;
using System.Text;
using System.Windows;
using System.Xml;
using Audiotool.model;
using CodeWalker.GameFiles;

namespace Audiotool.builders;

public static class AWCBuilder
{
    private static void GenerateNametable(string outputPath, string wavPath)
    {
        string[] filePaths = Directory.GetFiles(wavPath, "*.wav");

        if (filePaths.Length == 0)
        {
            return;
        }

        Stream content = File.Open(Path.Combine(outputPath, "awc.nametable"), FileMode.Create);
        BinaryWriter writer = new(content);

        foreach (string filePath in filePaths)
        {
            byte[] buf = Encoding.ASCII.GetBytes(Path.GetFileNameWithoutExtension(filePath) + char.MinValue);
            writer.Write(buf);
        }

        writer.Close();
        content.Close();
    }


    private static void BuildAWC(XmlDocument doc, string outputPath, string wavPath, string audioBank)
    {
        XmlNode? node = doc.SelectNodes("AudioWaveContainer")?[0];
        XmlNode? AWCNode = doc.GetElementsByTagName("AudioWaveContainer").Item(0);
        if (AWCNode == null || node == null)
        {
            MessageBox.Show("Unable to retrieve XML data!");
            return;
        }
        
        GenerateNametable(outputPath, wavPath);

        int fails = 0;

        do
        {
            try
            {
                AwcFile awc = new();
                if (Directory.Exists(wavPath))
                {
                    awc.ReadXml(node, wavPath);
                    byte[] file = awc.Save();
                    File.WriteAllBytes(Path.Combine(outputPath, $"{audioBank}.awc"), file);
                    break;
                }
            }
            catch (Exception)
            {
                Thread.Sleep(1000);
                fails++;
            }
        } while (fails < 5);

        if (fails == 5)
        {
            MessageBox.Show("Failed to build AWC with Codewalker in 5 attempts! Please manually build", "Unable to build AWC!");
        }

    }
    
    
    public static void GenerateXML(List<Audio> audioFiles, string outputPath, string wavPath, string audioBank)
    { 
        var doc = new XmlDocument();

        // <AudioWaveContainer>
        var root = doc.CreateElement("AudioWaveContainer");
        doc.AppendChild(root);

        // <Version value="1"/>
        var versionElement = doc.CreateElement("Version");
        versionElement.SetAttribute("value", "1");
        root.AppendChild(versionElement);

        // <ChunkIndices value="True"/>
        var chunkIndices = doc.CreateElement("ChunkIndices");
        chunkIndices.SetAttribute("value", "True");
        root.AppendChild(chunkIndices);

        // (Check if Any file is larger than 1.5 * 8 * 1024 * 1024)
        bool streamFormat = audioFiles.Any(a => a.FileSize > (1.5 * 8 * 1024 * 1024));

        // If so, we add <MultiChannel value="True"/>
        if (streamFormat)
        {
            var multiChannel = doc.CreateElement("MultiChannel");
            multiChannel.SetAttribute("value", "True");
            root.AppendChild(multiChannel);
        }

        // Create <Streams>, possibly adding "streamFormat" blocks
        var streamsElement = CreateStreamsElement(doc, streamFormat);
        root.AppendChild(streamsElement);

        // Generate and add audio file nodes
        AddAudioNodes(doc, streamsElement, audioFiles);

        // Save the document
        var settings = new XmlWriterSettings
        {
            Async = true,
            Indent = true,
            OmitXmlDeclaration = false,
            Encoding = Encoding.UTF8
        };
        
        using (XmlWriter writer = XmlWriter.Create(Path.Combine(outputPath, "output.awc.xml"), settings))
        {
            doc.Save(writer);
        }

        BuildAWC(doc, outputPath, wavPath, audioBank);
    }
    
    private static XmlElement CreateStreamsElement(XmlDocument doc, bool streamFormat)
    {
        var streamsElement = doc.CreateElement("Streams");

        if (streamFormat)
        {
            // <Item>
            var item = doc.CreateElement("Item");
            streamsElement.AppendChild(item);

            // <Name/> (empty)
            var nameElement = doc.CreateElement("Name");
            item.AppendChild(nameElement);

            // <Chunks>
            var chunksElement = doc.CreateElement("Chunks");
            item.AppendChild(chunksElement);

            // 1) streamformat block
            var chunk1 = doc.CreateElement("Item");
            chunksElement.AppendChild(chunk1);

            var type1 = doc.CreateElement("Type");
            type1.InnerText = "streamformat";
            chunk1.AppendChild(type1);

            var blockSize = doc.CreateElement("BlockSize");
            blockSize.SetAttribute("value", "524288");
            chunk1.AppendChild(blockSize);

            // 2) data block
            var chunk2 = doc.CreateElement("Item");
            chunksElement.AppendChild(chunk2);

            var type2 = doc.CreateElement("Type");
            type2.InnerText = "data";
            chunk2.AppendChild(type2);

            // 3) seektable block
            var chunk3 = doc.CreateElement("Item");
            chunksElement.AppendChild(chunk3);

            var type3 = doc.CreateElement("Type");
            type3.InnerText = "seektable";
            chunk3.AppendChild(type3);
        }

        return streamsElement;
    }
    
    private static void AddAudioNodes(XmlDocument doc, XmlElement streamsElement, IEnumerable<Audio> audioFiles)
    {
        foreach (Audio audio in audioFiles)
        {
            var nodes = audio.GenerateXML(doc);
            if (nodes?.Count > 0)
            {
                foreach (XmlNode node in nodes)
                {
                    streamsElement.AppendChild(node);
                }
            }
        }
    }
}