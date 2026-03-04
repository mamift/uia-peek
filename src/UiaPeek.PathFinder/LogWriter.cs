#nullable enable
using ExtendedXmlSerializer;
using ExtendedXmlSerializer.Configuration;
using System;
using System.IO;
using System.IO.Pipes;
using System.Xml.Linq;
using UiaPeek.Domain.Models;

namespace UiaPeek.PathFinder;

public class LogWriter: IDisposable
{
    private bool _isDisposed;
    public string FilePath { get; }

    public XDocument Document { get; }

    public bool IsDisposed
    {
        get { return _isDisposed; }
    }

    public LogWriter(string? filePath = null)
    {
        FilePath ??= Path.Combine(Environment.CurrentDirectory, "PathCaptureLog.xml");
        
        if (!File.Exists(FilePath))
        {
            Document = new XDocument(new XElement("PathLog"));
            Document.Save(File.Create(FilePath));
        }
        else
        {
            Document = XDocument.Load(FilePath);
        }

        this.Serializer = new ConfigurationContainer().UseOptimizedNamespaces().UseAutoFormatting()
            .EnableReferences()
            .Type<UiaChainModel>().Member(e => e.Locator).Verbatim()
            .Create();
    }

    public IExtendedXmlSerializer Serializer { get; }

    public void Write(string data)
    {
        Document.Root?.Add(new XElement("Data", data));
    }

    public void SerializeAndWrite<T>(T data, string? processName)
    {
        XDocument doc = XDocument.Load(FilePath);

        var serialized = Serializer.Serialize(data);
        var dataElement = new XElement("Data", XElement.Parse(serialized));
        dataElement.SetAttributeValue("ProcessName", processName);
        doc.Root?.Add(dataElement);
    }

    public void Save() => Document.Save(FilePath, SaveOptions.None);

    public void Dispose()
    {
        Document.Save(FilePath, SaveOptions.None);
        _isDisposed = true;
    }

    public void SafeDispose()
    {
        if (_isDisposed) return;
        this.Dispose();
    }
}