using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using UiaPeek.PathFinder.Models;
using UIAutomationClient;

namespace UiaPeek.PathFinder.Extensions;

/// <summary>
/// Contains extension methods for working with UI Automation elements.
/// </summary>
internal static class UiaExtensions
{
    /// <summary>
    /// Gets a dictionary of attributes for the specified UI Automation element within the default timeout period of 5 seconds.
    /// </summary>
    /// <param name="element">The <see cref="IUIAutomationElement"/> to get the attributes of.</param>
    /// <returns>
    /// A dictionary of attribute names and values for the specified element, or an empty dictionary if the operation fails.
    /// </returns>
    public static IDictionary<string, string> GetAttributes(this IUIAutomationElement element)
    {
        // Call the overloaded GetAttributes method with a default timeout of 5 seconds.
        return GetAttributes(element, timeout: TimeSpan.FromSeconds(5));
    }

    /// <summary>
    /// Gets a dictionary of attributes for the specified UI Automation element within the given timeout period.
    /// </summary>
    /// <param name="element">The <see cref="IUIAutomationElement"/> to get the attributes of.</param>
    /// <param name="timeout">The maximum amount of time to attempt to get the attributes.</param>
    /// <returns>
    /// A dictionary of attribute names and values for the specified element, or an empty dictionary if the operation fails.
    /// </returns>
    public static IDictionary<string, string> GetAttributes(this IUIAutomationElement element, TimeSpan timeout)
    {
        // Return an empty dictionary if the element is null.
        if (element == null)
        {
            return new Dictionary<string, string>();
        }

        // Formats the input string to be XML-safe by replacing special characters with their corresponding XML entities.
        static string FormatXml(string input)
        {
            // Check if the input string is null or empty.
            if (string.IsNullOrEmpty(input))
            {
                // Return an empty string if the input is null or empty.
                return string.Empty;
            }

            // Replace special characters with their corresponding XML entities and return the result.
            return input
                .Replace("&", "&amp;")     // Ampersand
                .Replace("\"", "&quot;")   // Double quote
                .Replace("'", "&apos;")    // Single quote
                .Replace("<", "&lt;")      // Less than
                .Replace(">", "&gt;")      // Greater than
                .Replace("\n", "&#xA;")    // Newline
                .Replace("\r", "&#xD;");   // Carriage return
        }

        // Formats the attributes of the UI Automation element into a dictionary.
        static Dictionary<string, string> FormatAttributes(IUIAutomationElement info) => new(StringComparer.OrdinalIgnoreCase)
        {
            ["AcceleratorKey"] = FormatXml(info.CurrentAcceleratorKey),
            ["AccessKey"] = FormatXml(info.CurrentAccessKey),
            ["AriaProperties"] = FormatXml(info.CurrentAriaProperties),
            ["AriaRole"] = FormatXml(info.CurrentAriaRole),
            ["AutomationId"] = FormatXml(info.CurrentAutomationId),
            ["Bottom"] = $"{info.CurrentBoundingRectangle.bottom}",
            ["ClassName"] = FormatXml(info.CurrentClassName),
            ["FrameworkId"] = FormatXml(info.CurrentFrameworkId),
            ["HelpText"] = FormatXml(info.CurrentHelpText),
            ["IsContentElement"] = info.CurrentIsContentElement == 1 ? "true" : "false",
            ["IsControlElement"] = info.CurrentIsControlElement == 1 ? "true" : "false",
            ["IsEnabled"] = info.CurrentIsEnabled == 1 ? "true" : "false",
            ["IsKeyboardFocusable"] = info.CurrentIsKeyboardFocusable == 1 ? "true" : "false",
            ["IsPassword"] = info.CurrentIsPassword == 1 ? "true" : "false",
            ["IsRequiredForForm"] = info.CurrentIsRequiredForForm == 1 ? "true" : "false",
            ["ItemStatus"] = FormatXml(info.CurrentItemStatus),
            ["ItemType"] = FormatXml(info.CurrentItemType),
            ["Left"] = $"{info.CurrentBoundingRectangle.left}",
            ["Name"] = FormatXml(info.CurrentName),
            ["NativeWindowHandle"] = $"{info.CurrentNativeWindowHandle}",
            ["Orientation"] = $"{info.CurrentOrientation}",
            ["ProcessId"] = $"{info.CurrentProcessId}",
            ["Right"] = $"{info.CurrentBoundingRectangle.right}",
            ["Top"] = $"{info.CurrentBoundingRectangle.top}"
        };

        // Calculate the expiration time for the timeout.
        DateTime expiration = DateTime.Now.Add(timeout);

        // Attempt to get the attributes until the timeout expires.
        while (DateTime.Now < expiration)
        {
            try
            {
                // Format and return the attributes of the element.
                return FormatAttributes(element);
            }
            catch (COMException)
            {
                // Ignore COM exceptions and continue attempting until the timeout expires.
            }
        }

        // Return an empty dictionary if the attributes could not be retrieved within the timeout.
        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets a UI Automation element based on the specified xpath relative to the root element.
    /// </summary>
    /// <param name="automation">The UI Automation instance.</param>
    /// <param name="xpath">The xpath expression specifying the location of the element relative to the root element.</param>
    /// <returns>
    /// The UI Automation element found based on the xpath.
    /// Returns null if the xpath expression is invalid, does not specify criteria, or the element is not found.
    /// </returns>
    public static IUIAutomationElement GetElement(this CUIAutomation8 automation, string xpath)
    {
        // Normalize the xpath by removing any leading "/Desktop" segment.
        xpath = xpath.Replace("/Desktop", "/");
        xpath = xpath.StartsWith("///") ? xpath.Replace("///", "//") : xpath;

        // Get the root UI Automation element.
        IUIAutomationElement automationElement = automation.GetRootElement();

        // Use the FindElement method for finding an element based on the specified xpath.
        return FindElement(automationElement, xpath).Element?.UIAutomationElement;
    }

    /// <summary>
    /// Gets a UI Automation element based on the specified xpath relative to the current element.
    /// </summary>
    /// <param name="automationElement">The current UI Automation element.</param>
    /// <param name="xpath">The xpath expression specifying the location of the element relative to the current element.</param>
    /// <returns>
    /// The UI Automation element found based on the xpath.
    /// Returns null if the xpath expression is invalid, does not specify criteria, or the element is not found.
    /// </returns>
    public static IUIAutomationElement GetElement(this IUIAutomationElement automationElement, string xpath)
    {
        // Use the FindElement method for finding an element based on the specified xpath.
        return FindElement(automationElement, xpath).Element.UIAutomationElement;
    }

    /// <summary>
    /// Gets the tag name of the UI Automation element with a default timeout of 5 seconds.
    /// </summary>
    /// <param name="element">The <see cref="IUIAutomationElement"/> to get the tag name of.</param>
    /// <returns>The tag name of the element, or an empty string if the operation fails.</returns>
    public static string GetTagName(this IUIAutomationElement element)
    {
        // Call the overloaded GetTagName method with a default timeout of 5 seconds.
        return GetTagName(element, timeout: TimeSpan.FromSeconds(5));
    }

    /// <summary>
    /// Gets the tag name of the UI Automation element within the specified timeout.
    /// </summary>
    /// <param name="element">The <see cref="IUIAutomationElement"/> to get the tag name of.</param>
    /// <param name="timeout">The maximum amount of time to attempt to get the tag name.</param>
    /// <returns>The tag name of the element, or an empty string if the operation fails.</returns>
    public static string GetTagName(this IUIAutomationElement element, TimeSpan timeout)
    {
        // Calculate the expiration time for the timeout.
        DateTime expires = DateTime.Now.Add(timeout);

        // Attempt to get the tag name until the timeout expires.
        while (DateTime.Now < expires)
        {
            try
            {
                // Get the control type field name corresponding to the element's current control type.
                string controlType = typeof(UIA_ControlTypeIds).GetFields()
                    .Where(f => f.FieldType == typeof(int))
                    .FirstOrDefault(f => (int)f.GetValue(null) == element.CurrentControlType)?.Name;

                // Extract and return the tag name from the control type field name.
                return Regex.Match(input: controlType, pattern: "(?<=UIA_).*(?=ControlTypeId)").Value;
            }
            catch (COMException)
            {
                // Ignore COM exceptions and continue attempting until the timeout expires.
            }
        }

        // Return an empty string if the tag name could not be retrieved within the timeout.
        return string.Empty;
    }

    /// <summary>
    /// Extracts element data from an <see cref="IUIAutomationElement"/>.
    /// </summary>
    /// <param name="element">The UI Automation element to extract data from.</param>
    /// <returns>An <see cref="ObservableCollection{ElementData}"/> containing element data.</returns>
    public static ObservableCollection<ElementDataModel> ExtractElementData(this IUIAutomationElement element)
    {
        // Get properties starting with "Current" from the UI Automation element type
        IDictionary<string, string> attributes = element.GetAttributes();

        // Create a collection of ElementData from the properties
        IEnumerable<ElementDataModel> collection = attributes.Select(i => new ElementDataModel { Property = i.Key, Value = i.Value });

        // Return the element data collection as an ObservableCollection
        return new ObservableCollection<ElementDataModel>(collection);
    }

    // Finds a UI automation element based on the given XPath expression.
    private static (int Status, ElementModel Element) FindElement(IUIAutomationElement applicationRoot, string xpath)
    {
        // Converts an IUIAutomationElement to an Element.
        static ElementModel ConvertToElement(IUIAutomationElement automationElement)
        {
            // Generate a unique ID for the element based on the AutomationId, or use a new GUID if AutomationId is empty.
            string automationId = automationElement.CurrentAutomationId;
            string id = string.IsNullOrEmpty(automationId)
                ? $"{Guid.NewGuid()}"
                : automationElement.CurrentAutomationId;

            // Create a Location object based on the current bounding rectangle of the UI Automation element.
            LocationModel location = new LocationModel
            {
                Bottom = automationElement.CurrentBoundingRectangle.bottom,
                Left = automationElement.CurrentBoundingRectangle.left,
                Right = automationElement.CurrentBoundingRectangle.right,
                Top = automationElement.CurrentBoundingRectangle.top
            };

            // Create a new Element object and populate its properties.
            ElementModel element = new ElementModel
            {
                Id = id,
                UIAutomationElement = automationElement,
                Location = location
            };

            // Return the created Element.
            return element;
        }

        // Convert the XPath expression to a UI Automation condition
        IUIAutomationCondition condition = XpathParser.ConvertToCondition(xpath);

        // Return 400 status code if the XPath expression is invalid
        if (condition == null)
        {
            return (400, default);
        }

        // Determine the search scope based on the XPath expression
        TreeScope scope = xpath.StartsWith("//")
            ? TreeScope.TreeScope_Descendants
            : TreeScope.TreeScope_Children;

        // Find the first element that matches the condition within the specified scope
        IUIAutomationElement element = applicationRoot.FindFirst(scope, condition);

        // Return the status and element: 404 if not found, 200 if found
        return element == null
            ? (404, default)
            : (200, ConvertToElement(element));
    }
}