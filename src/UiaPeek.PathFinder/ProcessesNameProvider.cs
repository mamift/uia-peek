using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using AutoCompleteTextBox.Editors;

namespace UiaPeek.PathFinder;

public class ProcessesNameProvider: ISuggestionProvider
{
    public ProcessesNameProvider(ObservableCollection<string> names)
    {
        Names = names;
        Names.CollectionChanged += NamesOnCollectionChanged;
    }

    private void NamesOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        
    }

    public ObservableCollection<string> Names { get; }

    public IEnumerable GetSuggestions(string filter)
    {
        return Names;
    }
}