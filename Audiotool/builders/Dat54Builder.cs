using Audiotool.model;
using CodeWalker.GameFiles;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml;
using System.Xml.Linq;

namespace Audiotool.builders;

public static class Dat54Builder
{

    public static void LoadAndSaveRelFromXmlDocument(XmlDocument doc, string outputFolder, string audioDataFileName = "audioexample_sounds")
    {
        RelFile rel = XmlRel.GetRel(doc);
        if (rel == null)
        {
            MessageBox.Show("Failed to build rel");
            return;
        }

        // why the fuck did they name it save when it just returns the fucking bytes for the file?!?!?
        byte[] relData = rel.Save();
        if (relData == null || relData.Length == 0)
        {
            MessageBox.Show("No data generated from rel.Save()");
            return;
        }

        string finalPath = Path.Combine(outputFolder, $"{audioDataFileName}.dat54.rel");
        File.WriteAllBytes(finalPath, relData);
    }


    public static void ConstructDat54(
        List<Audio> audioFiles,
        string outputFolder,
        string audioBankName = "output",
        string soundsetName = "special_soundset",
        string audioDataFileName = "audioexample_sounds")
    {

        string dataDirectory = Path.Combine(outputFolder, "data");
        string outputFileName = $"{audioDataFileName}.dat54.rel.xml";

        var doc = new XmlDocument();
        var dat54Elem = doc.CreateElement("Dat54");
        doc.AppendChild(dat54Elem);

        var versionElem = doc.CreateElement("Version");
        versionElem.SetAttribute("value", "7314721");
        dat54Elem.AppendChild(versionElem);

        var containerPathsElem = doc.CreateElement("ContainerPaths");
        dat54Elem.AppendChild(containerPathsElem);

        var containerPathsItem = doc.CreateElement("Item");
        containerPathsItem.InnerText = @$"audiodirectory\{audioBankName}";
        containerPathsElem.AppendChild(containerPathsItem);

        var itemsElem = doc.CreateElement("Items");
        dat54Elem.AppendChild(itemsElem);

        // For each audio file, create <Item type="SimpleSound"> more research is needed to figure out other types but IDGAF rn I just want to die
        foreach (Audio audio in audioFiles)
        {
            var soundItem = doc.CreateElement("Item");
            soundItem.SetAttribute("type", "SimpleSound");
            itemsElem.AppendChild(soundItem);

            var nameElem = doc.CreateElement("Name");
            nameElem.InnerText = audio.FileName + "_sp";
            soundItem.AppendChild(nameElem);

            var headerElem = doc.CreateElement("Header");
            soundItem.AppendChild(headerElem);

            var flagsElem = doc.CreateElement("Flags");
            flagsElem.SetAttribute("value", "0x00008004");
            headerElem.AppendChild(flagsElem);

            var volumeElem = doc.CreateElement("Volume");
            volumeElem.SetAttribute("value", audio.Volume.ToString());
            headerElem.AppendChild(volumeElem);

            var categoryElem = doc.CreateElement("Category");
            categoryElem.InnerText = audio.Volume > 200 ? "scripted_louder" : "scripted";
            headerElem.AppendChild(categoryElem);

            var containerNameElem = doc.CreateElement("ContainerName");
            containerNameElem.InnerText = $"audiodirectory/{audioBankName}";
            soundItem.AppendChild(containerNameElem);

            // <FileName> (e.g., "button_click" without extension or "button_click.wav" if needed)
            var fileNameElem = doc.CreateElement("FileName");

            // Decide if you want "FileName + FileExtension" or just "FileName"
            fileNameElem.InnerText = audio.FileName;
            soundItem.AppendChild(fileNameElem);

            var waveSlotElem = doc.CreateElement("WaveSlotNum");
            waveSlotElem.SetAttribute("value", "0");
            soundItem.AppendChild(waveSlotElem);
        }

        var soundSetItem = doc.CreateElement("Item");
        soundSetItem.SetAttribute("type", "SoundSet");
        itemsElem.AppendChild(soundSetItem);

        var soundSetNameElem = doc.CreateElement("Name");
        soundSetNameElem.InnerText = soundsetName;
        soundSetItem.AppendChild(soundSetNameElem);

        var soundSetHeader = doc.CreateElement("Header");
        soundSetItem.AppendChild(soundSetHeader);

        var soundSetFlags = doc.CreateElement("Flags");
        soundSetFlags.SetAttribute("value", "0xAAAAAAAA");
        soundSetHeader.AppendChild(soundSetFlags);

        var soundSetsElem = doc.CreateElement("SoundSets");
        soundSetItem.AppendChild(soundSetsElem);

        foreach (var audio in audioFiles)
        {
            var setItem = doc.CreateElement("Item");

            var scriptNameElem = doc.CreateElement("ScriptName");
            scriptNameElem.InnerText = audio.FileName;
            setItem.AppendChild(scriptNameElem);

            var childSoundElem = doc.CreateElement("ChildSound");
            childSoundElem.InnerText = audio.FileName + "_sp";
            setItem.AppendChild(childSoundElem);

            soundSetsElem.AppendChild(setItem);
        }

        string finalXmlPath = Path.Combine(dataDirectory, outputFileName);
        doc.Save(finalXmlPath);



        LoadAndSaveRelFromXmlDocument(doc, dataDirectory, audioDataFileName);
    }
}
