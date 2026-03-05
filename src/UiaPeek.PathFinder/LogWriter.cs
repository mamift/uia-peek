#nullable enable
using ExtendedXmlSerializer;
using ExtendedXmlSerializer.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.InteropServices.Marshalling;
using System.Xml.Linq;
using System.Xml.XPath;
using UiaPeek.Domain.Models;
using UiaPeek.PathFinder.Extensions;
using UIAutomationClient;

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

        var memberConfiguration = new ConfigurationContainer().UseOptimizedNamespaces()
            .EnableReferences()
            .AllowMultipleReferences()
            .Type<UiaNodeModel>().Member(e => e.Element).Ignore();

        this.Serializer = memberConfiguration
            .Type<IUIAutomationElement>().Ignore()
            .Type<UiaChainModel>().Member(e => e.Path).Ignore()
            .Create();
    }

    public IExtendedXmlSerializer Serializer { get; }

    public void Write(string data)
    {
        Document.Root?.Add(new XElement("Data", data));
    }

    public void SerializeAndWrite<T>(T data, string? processName) where T: class
    {
        if (data is null) throw new ArgumentNullException(nameof(data));

        string locatorElXpath = "*[namespace-uri()='clr-namespace:UiaPeek.Domain.Models;assembly=UiaPeek.Domain' and local-name()='UiaChainModel'][1]/*[namespace-uri()='clr-namespace:UiaPeek.Domain.Models;assembly=UiaPeek.Domain' and local-name()='Locator']";

        string docRootElXpath = "/PathLog/Data/" + locatorElXpath;

        List<XElement> existingLocatorElValues = ((IEnumerable<object>)Document.XPathEvaluate(docRootElXpath)).Cast<XElement>().DistinctBy(a => a.Value).ToList();

        string? serializedXmlString = Serializer.Serialize(data);
        XElement dataContents = XElement.Parse(serializedXmlString);
        var (nss, _) = dataContents.ExtractNamespaces();
        XAttribute? defaultNamespace = nss.GetDefaultNs();

        List<XElement> newLocatorElValues = dataContents.Descendants(XName.Get("Locator", defaultNamespace!.Value)).ToList();

        var anyElMatched = existingLocatorElValues.Any(a => newLocatorElValues.Any(aa => aa.Value == a.Value));

        if (anyElMatched)
        {
            return;
        }

        XElement dataElement = new XElement("Data", dataContents);

        dataElement.SetAttributeValue("ProcessName", processName);
        dataElement.SetAttributeValue("DateTime", DateTime.Now);

        Document.Root?.Add(dataElement);
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