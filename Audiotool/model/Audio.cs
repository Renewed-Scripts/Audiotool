using System.Xml;

namespace Audiotool.model;

public struct Marker
{
    public string Name; // trackid, beat, dj, rockout, etc...
    public string Value; // This can be a string, but also an int. So we will have to check if the value is an int, then set it as an attribute, otherwise its an InnerText value
    public int SampleOffset; //How many samples from the first sample before this marker is acted upon.
}

public class Audio
{
    // Audio information
    public string FilePath { get; set; }
    public ulong FileSize { get; set; }
    public string FileName { get; set; }
    public TimeSpan Duration { get; set; }
    public int Samples { get; set; }
    public int SampleRate { get; set; }
    public string FileExtension { get; set; }
    public string Codec { get; set; }
    public int Channels { get; set; }
    
    // Audio settings
    public int Headroom { get; set; } = -200;
    public int PlayBegin { get; set; } = 0;
    public int PlayEnd { get; set; } = 0;
    public int LoopBegin { get; set; } = 0;
    public int LoopEnd { get; set; } = 0;
    public int LoopPoint { get; set; } = -1;
    public int Peak { get; set; } = 0;
    public int Volume { get; set; } = 200;
    
    public List<Marker> Markers { get; set; } = [];
    
    
    
    
    private bool streamFormat;
    
    
    public List<XmlNode> GenerateXML(XmlDocument doc)
    {
        if (FileSize > (1.5 * 8 * 1024 * 1024))
        {
            streamFormat = true;
        }

        int itemCount = streamFormat ? 2 : 1;

        var nodes = new List<XmlNode>();

        for (int i = 0; i < itemCount; i++)
        {
            XmlElement itemElement = doc.CreateElement("Item");

            AppendBasicFileInfo(doc, itemElement, i);

            // If NOT streamFormat, we handle “non-stream” chunks + “format” node
            if (!streamFormat)
            {
                AppendNonStreamFormatData(doc, itemElement);
            }
            else
            {
                AppendStreamFormatData(doc, itemElement);
                if (Markers.Count > 0)
                {
                    XmlElement markersChunks = doc.CreateElement("Chunks");
                    AppendMarkerItems(doc, markersChunks);
                    itemElement.AppendChild(markersChunks);
                }
            }

            nodes.Add(itemElement);
        }

        return nodes;
    }

    private void AppendBasicFileInfo(XmlDocument doc, XmlElement itemElement, int index)
    {
        // <Name>FileName + maybe "_left" or "_right"</Name>
        var nameElement = doc.CreateElement("Name");
        if (streamFormat)
        {
            nameElement.InnerText = index == 0 
                ? FileName + "_left"
                : FileName + "_right";
        }
        else
        {
            nameElement.InnerText = FileName;
        }
        itemElement.AppendChild(nameElement);

        // <FileName>FileName.wav</FileName>
        var fileNameElement = doc.CreateElement("FileName");
        fileNameElement.InnerText = $"{FileName}.wav";
        itemElement.AppendChild(fileNameElement);
    }
    
    private void AppendNonStreamFormatData(XmlDocument doc, XmlElement itemElement)
    {
        // <Chunks>
        XmlElement chunksElement = doc.CreateElement("Chunks");
        itemElement.AppendChild(chunksElement);

        // <Item><Type>peak</Type></Item>
        var peakItem = doc.CreateElement("Item");
        var peakType = doc.CreateElement("Type");
        peakType.InnerText = "peak";
        peakItem.AppendChild(peakType);
        chunksElement.AppendChild(peakItem);

        // <Item><Type>data</Type></Item>
        var dataItem = doc.CreateElement("Item");
        var dataType = doc.CreateElement("Type");
        dataType.InnerText = "data";
        dataItem.AppendChild(dataType);
        chunksElement.AppendChild(dataItem);

        // "Entry" node for format info:
        // <Item>
        //   <Type>format</Type>
        //   <Codec>...</Codec>
        //   <Samples value="..."/>
        //   <SampleRate value="..."/>
        //   ...
        //   <Peak unk="..."/>
        // </Item>
        var entryElement = doc.CreateElement("Item");
        chunksElement.AppendChild(entryElement);

        // <Type>format</Type>
        var typeElement = doc.CreateElement("Type");
        typeElement.InnerText = "format";
        entryElement.AppendChild(typeElement);

        // <Codec>...</Codec>
        var codecElement = doc.CreateElement("Codec");
        codecElement.InnerText = Codec; // Could be MP3, WAV, etc.
        entryElement.AppendChild(codecElement);

        // <Samples value="..."/>
        AppendAttributeElement(doc, entryElement, "Samples", "value", Samples.ToString());
        // <SampleRate value="..."/>
        AppendAttributeElement(doc, entryElement, "SampleRate", "value", SampleRate.ToString());
        // <Headroom value="..."/>
        AppendAttributeElement(doc, entryElement, "Headroom", "value", Headroom.ToString());

        // Additional fields only for non-streamFormat
        AppendAttributeElement(doc, entryElement, "PlayBegin", "value", PlayBegin.ToString());
        AppendAttributeElement(doc, entryElement, "PlayEnd", "value", PlayEnd.ToString());
        AppendAttributeElement(doc, entryElement, "LoopBegin", "value", LoopBegin.ToString());
        AppendAttributeElement(doc, entryElement, "LoopEnd", "value",  LoopEnd.ToString());
        AppendAttributeElement(doc, entryElement, "LoopPoint", "value", LoopPoint.ToString());

        // <Peak unk="..."/>
        var peakUnkElement = doc.CreateElement("Peak");
        peakUnkElement.SetAttribute("unk", Peak.ToString());
        entryElement.AppendChild(peakUnkElement);
    }

    /// <summary>
    /// For streamFormat == true, we create a <StreamFormat> node with:
    ///   - <Codec>ADPCM</Codec>
    ///   - <Samples value="..."/>
    ///   - <SampleRate value="..."/>
    ///   - <Headroom value="..."/>
    /// </summary>
    private void AppendStreamFormatData(XmlDocument doc, XmlElement itemElement)
    {
        // <StreamFormat>
        var streamFormatElement = doc.CreateElement("StreamFormat");
        itemElement.AppendChild(streamFormatElement);

        // <Codec>ADPCM</Codec>
        var codecElement = doc.CreateElement("Codec");
        codecElement.InnerText = "ADPCM"; // Required for streamFormat
        streamFormatElement.AppendChild(codecElement);

        // <Samples value="..."/>
        AppendAttributeElement(doc, streamFormatElement, "Samples", "value", Samples.ToString());
        // <SampleRate value="..."/>
        AppendAttributeElement(doc, streamFormatElement, "SampleRate", "value", SampleRate.ToString());
        // <Headroom value="..."/>
        AppendAttributeElement(doc, streamFormatElement, "Headroom", "value", Headroom.ToString());
    }

    /// <summary>
    /// Appends all <Marker> nodes inside a <Chunks> element (already created).
    /// Each marker is <Item><Name>..</Name><Value>..</Value><SampleOffset value="..."/></Item>.
    /// </summary>
    private void AppendMarkerItems(XmlDocument doc, XmlElement markersChunks)
    {
        foreach (Marker marker in Markers)
        {
            // <Item>
            XmlElement markerItem = doc.CreateElement("Item");

            // <Name>marker.Name</Name>
            var markerName = doc.CreateElement("Name");
            markerName.InnerText = marker.Name;
            markerItem.AppendChild(markerName);

            // <Value> or <Value value="...">
            var markerValue = doc.CreateElement("Value");
            if (int.TryParse(marker.Value, out _))
            {
                markerValue.SetAttribute("value", marker.Value);
            }
            else
            {
                markerValue.InnerText = marker.Value;
            }
            markerItem.AppendChild(markerValue);

            // <SampleOffset value="..." />
            AppendAttributeElement(doc, markerItem, "SampleOffset", "value", marker.SampleOffset.ToString());

            markersChunks.AppendChild(markerItem);
        }
    }

    /// <summary>
    /// Helper to create an element with a single attribute and append it to a parent.
    /// E.g., AppendAttributeElement(doc, parent, "Samples", "value", "1234") -> 
    ///   <Samples value="1234"/> 
    /// </summary>
    private void AppendAttributeElement(XmlDocument doc, XmlElement parent, 
                                        string elementName, 
                                        string attributeName, 
                                        string attributeValue)
    {
        var element = doc.CreateElement(elementName);
        element.SetAttribute(attributeName, attributeValue);
        parent.AppendChild(element);
    }
}
