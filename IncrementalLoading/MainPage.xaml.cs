using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=391641

namespace IncrementalLoading
{
    public interface IIncrementalSource<T>
    {
        Task<IEnumerable<T>> GetPagedItems(int pageIndex, int pageSize);
    }

    public class IncrementalLoadingCollection<T, I> : ObservableCollection<I>, ISupportIncrementalLoading where T : IIncrementalSource<I>, new()
    {
        private T source;
        private int itemsPerPage;
        private bool hasMoreItems;
        private int currentPage;

        public IncrementalLoadingCollection(int itemsPerPage = 10)
        {
            this.source = new T();
            this.itemsPerPage = itemsPerPage;
            this.hasMoreItems = true;
        }

        public bool HasMoreItems
        {
            get { return hasMoreItems; }
        }

        public IAsyncOperation<LoadMoreItemsResult> LoadMoreItemsAsync(uint count)
        {
            var dispatcher = Window.Current.Dispatcher;

            return AsyncInfo.Run(async cancel =>
            {
                var result = await source.GetPagedItems(currentPage++, itemsPerPage);
                uint resultCount = (uint)(result == null ? 0 : result.Count());

                if (resultCount == 0)
                {
                    hasMoreItems = false;
                }
                else
                {
                    await Task.WhenAll(Task.Delay(10), dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        foreach (I item in result)
                            this.Add(item);
                    }).AsTask());
                }

                return new LoadMoreItemsResult() { Count = resultCount };   
            });
        }
    }

    public class DatabaseNotificationModelSource : IIncrementalSource<string>
    {
        private List<string> items;

        public async Task<IEnumerable<string>> GetPagedItems(int pageIndex, int pageSize)
        {
            System.Diagnostics.Debug.WriteLine("Loading page {0}", pageIndex);

            if (items == null)
            {
                // simulate waiting for the network
                //await Task.Delay(1000);

                items = new List<string>();
                for (var i = 0; i < 1000; i++)
                    items.Add(i.ToString());
            }

            //return await Task.Run<IEnumerable<string>>(() =>
            //{
            //    var result = (from p in items select p).Skip(pageIndex * pageSize).Take(pageSize);
            //    return result;
            //});

            return items.Skip(pageIndex * pageSize).Take(pageSize);
        }
    }

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            var source = new IncrementalLoadingCollection<DatabaseNotificationModelSource, string>();
            listview.ItemsSource = source;
        }
    }
}
