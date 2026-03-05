#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

namespace UiaPeek.PathFinder.Extensions;

public static class GeneralExtensions
{
    public static void AddRange<T>(this ObservableCollection<T> collection, IEnumerable<T> things)
    {
        foreach (var item in things)
        {
            collection.Add(item);
        }
    }

    public static XAttribute? GetDefaultNs(this Dictionary<string, XAttribute> namespaces)
    {
        var didGet = namespaces.TryGetValue("", out var val);

        return didGet ? val : null;
    }

    public static (Dictionary<string, XAttribute> namespaces, XmlNamespaceManager nsManager) ExtractNamespaces(this XContainer el, string defaultPrefix = "")
    {
        var namespaces = new Dictionary<string, XAttribute>();

        void Traverse(XContainer element)
        {
            var attrs = element switch {
                XElement e => e.Attributes(),
                XDocument d => d.Root?.Attributes() ?? throw new InvalidOperationException("Document has no root element."),
                _ => throw new OutOfMemoryException()
            };

            foreach (XAttribute attr in attrs) {
                if (attr.IsNamespaceDeclaration) {
                    string prefix = attr.Name.LocalName == "xmlns" ? defaultPrefix : attr.Name.LocalName;
                    if (!namespaces.ContainsKey(prefix)) {
                        namespaces[prefix] = attr;
                    }
                }
            }

            foreach (var child in element.Elements()) {
                Traverse(child);
            }
        }

        Traverse(el);

        XmlNameTable nt = el.CreateNavigator().NameTable;
        var nsManager = new XmlNamespaceManager(nt);

        foreach (var ns in namespaces) {
            nsManager.AddNamespace(ns.Key, ns.Value.Value);
        }

        return (namespaces, nsManager);
    }
}