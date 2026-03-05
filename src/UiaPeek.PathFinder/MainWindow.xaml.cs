using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Xml.Linq;
using UiaPeek.PathFinder.Extensions;
using UIAutomationClient;

namespace UiaPeek.PathFinder
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    [DependencyPropertyGenerator.DependencyProperty("ShowProcessesListBox", typeof(bool), DefaultValue = false)]
    [DependencyPropertyGenerator.DependencyProperty("Processes", typeof(ObservableCollection<string>), DefaultValueExpression = "new()")]
    [DependencyPropertyGenerator.DependencyProperty("ProcessesNamesProvider", typeof(ProcessesNameProvider))]
    public partial class MainWindow : Window
    {
        protected override void OnClosing(CancelEventArgs e)
        {
            this.LocatorTabView.OnClosing(e);
            base.OnClosing(e);
        }
    }

    /// <summary>
    /// Factory class for creating a Document Object Model (DOM) representation of UI Automation elements.
    /// </summary>
    /// <param name="rootElement">The root UI Automation element.</param>
    internal class DocumentObjectModelFactory(IUIAutomationElement rootElement)
    {
        // The root UI Automation element used as the starting point for creating the DOM.
        private readonly IUIAutomationElement _rootElement = rootElement;

        /// <summary>
        /// Initializes a new instance of the <see cref="DocumentObjectModelFactory"/> class.
        /// </summary>
        public DocumentObjectModelFactory()
            : this(new CUIAutomation8().GetRootElement())
        { }

        /// <summary>
        /// Creates a new XML document representing the UI Automation element tree.
        /// </summary>
        /// <returns>A new <see cref="XDocument"/> representing the UI Automation element tree.</returns>
        public XDocument New()
        {
            // Create a new instance of the UI Automation object.
            CUIAutomation8 automation = new CUIAutomation8();

            // Use the root element if available; otherwise, get the desktop root element.
            IUIAutomationElement element = _rootElement ?? automation.GetRootElement();

            // Create the XML document from the UI Automation element tree.
            return New(automation, element, addDesktop: true);
        }

        /// <summary>
        /// Creates a new XML document representing the UI Automation element tree for the specified element.
        /// </summary>
        /// <param name="element">The UI Automation element.</param>
        /// <returns>A new <see cref="XDocument"/> representing the UI Automation element tree.</returns>
        public static XDocument New(IUIAutomationElement element)
        {
            // Create a new instance of the UI Automation object.
            CUIAutomation8 automation = new CUIAutomation8();

            // Create the XML document from the UI Automation element tree.
            return New(automation, element, addDesktop: true);
        }

        /// <summary>
        /// Creates a new XML document representing the UI Automation element tree for the specified automation object and element.
        /// </summary>
        /// <param name="automation">The UI Automation object.</param>
        /// <param name="element">The UI Automation element.</param>
        /// <returns>A new <see cref="XDocument"/> representing the UI Automation element tree.</returns>
        public static XDocument New(CUIAutomation8 automation, IUIAutomationElement element)
        {
            return New(automation, element, addDesktop: true);
        }

        /// <summary>
        /// Creates a new XML document representing the DOM structure of the given UI automation element.
        /// </summary>
        /// <param name="automation">The UI Automation instance to be used.</param>
        /// <param name="element">The UI Automation element to be processed.</param>
        /// <param name="addDesktop">Flag indicating whether to wrap the XML in a desktop tag.</param>
        /// <returns>An XDocument representing the XML structure of the UI element.</returns>
        public static XDocument New(CUIAutomation8 automation, IUIAutomationElement element, bool addDesktop)
        {
            // Register and generate XML data for the new DOM.
            List<string> xmlData = Register(automation, element);

            // Construct the XML body with the tag name, attributes, and registered XML data.
            string xmlBody = string.Join("\n", xmlData);

            // Combine the XML data into a single XML string.
            string xml = addDesktop ? "<Desktop>" + xmlBody + "</Desktop>" : xmlBody;

            try
            {
                // Parse and return the XML document.
                return XDocument.Parse(xml);
            }
            catch (Exception e)
            {
                // Handle any parsing exceptions and return an error XML document.
                return XDocument.Parse($"<Desktop><Error>{e.GetBaseException().Message}</Error></Desktop>");
            }
        }

        // Registers and generates XML data for the new DOM.
        private static List<string> Register(CUIAutomation8 automation, IUIAutomationElement element)
        {
            // Initialize a list to store XML data.
            List<string> xml = new List<string>();

            // Get the tag name and attributes of the element.
            string tagName = element.GetTagName();
            string attributes = GetElementAttributes(element);

            // Add the opening tag with attributes to the XML list.
            xml.Add($"<{tagName} {attributes}>");

            // Create a condition to find all child elements.
            IUIAutomationCondition condition = automation.CreateTrueCondition();
            IUIAutomationTreeWalker treeWalker = automation.CreateTreeWalker(condition);
            IUIAutomationElement childElement = treeWalker.GetFirstChildElement(element);

            // Recursively process child elements.
            while (childElement != null)
            {
                List<string> nodeXml = Register(automation, childElement);
                xml.AddRange(nodeXml);
                childElement = treeWalker.GetNextSiblingElement(childElement);
            }

            // Add the closing tag to the XML list.
            xml.Add($"</{tagName}>");

            // Return the complete XML data list.
            return xml;
        }

        // Gets the attributes of the specified UI Automation element as a string.
        private static string GetElementAttributes(IUIAutomationElement element)
        {
            // Get the attributes of the element.
            IDictionary<string, string> attributes = element.GetAttributes();

            // Get the runtime ID of the element and serialize it to a JSON string.
            IEnumerable<int> runtime = element.GetRuntimeId().OfType<int>();
            string id = JsonSerializer.Serialize(runtime);
            attributes.Add("id", id);

            // Initialize a list to store attribute strings.
            List<string> xmlNode = new List<string>();
            foreach (KeyValuePair<string, string> item in attributes)
            {
                // Skip attributes with empty or whitespace-only keys or values.
                if (string.IsNullOrEmpty(item.Key) || string.IsNullOrEmpty(item.Value))
                {
                    continue;
                }

                // Add the attribute to the XML node list.
                xmlNode.Add($"{item.Key}=\"{item.Value}\"");
            }

            // Join the XML node representations into a single string and return it.
            return string.Join(" ", xmlNode);
        }
    }
}
