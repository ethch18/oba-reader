using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.System;
using Windows.Web.Http;

namespace OneBusAway
{
    public sealed partial class MainPage : Page
    {
        private TranslationController controller;
        public MainPage()
        {
            InitializeComponent();
            Task.Run(() => this.controller = new TranslationController());
            buttonAdd.Content = new SymbolIcon(Symbol.Add);
            buttonAddRandom.Content = new SymbolIcon(Symbol.Shuffle);
            buttonAddPreset.Content = new SymbolIcon(Symbol.List);
            radioL1.IsChecked = true;
            checkEnglish.IsChecked = true;
            checkAutoFontSize.IsChecked = true;
            for (int i = 0; i < 100; i++)
            {
                RowDefinition row = new RowDefinition();
                row.Height = GridLength.Auto;
                gridMain.RowDefinitions.Add(row);

                TEXT_ROUTE_NUMBER[i] = new TextBlock();
                TEXT_ROUTE_NUMBER[i].FontSize = 48;
                TEXT_ROUTE_NUMBER[i].Margin = new Thickness(10, 0, 50, 0);
                gridMain.Children.Add(TEXT_ROUTE_NUMBER[i]);
                Grid.SetRow(TEXT_ROUTE_NUMBER[i], i + 1);

                STACK[i] = new StackPanel();
                gridMain.Children.Add(STACK[i]);
                Grid.SetRow(STACK[i], i + 1);
                Grid.SetColumn(STACK[i], 1);

                TEXT_DESTINATION[i] = new TextBlock();
                TEXT_DESTINATION[i].FontSize = 32;
                STACK[i].Children.Add(TEXT_DESTINATION[i]);

                TEXT_SMALL_TEXT[i] = new TextBlock();
                TEXT_SMALL_TEXT[i].FontSize = 12;
                STACK[i].Children.Add(TEXT_SMALL_TEXT[i]);

                TEXT_ARRIVAL[i] = new TextBlock();
                TEXT_ARRIVAL[i].FontSize = 48;
                TEXT_ARRIVAL[i].Margin = new Thickness(50, 0, 10, 0);
                TEXT_ARRIVAL[i].HorizontalAlignment = HorizontalAlignment.Right;
                gridMain.Children.Add(TEXT_ARRIVAL[i]);
                Grid.SetRow(TEXT_ARRIVAL[i], i + 1);
                Grid.SetColumn(TEXT_ARRIVAL[i], 2);
            }
            gridMain.RowDefinitions.Add(new RowDefinition());
            timer1.Tick += EVENTrefreshText;
            timer1.Interval = new TimeSpan(0, 0, 0, 0, 100);
            timer1.Start();
            timer10.Tick += EVENTrefreshData;
            timer10.Interval = new TimeSpan(0, 0, 0, SERVER_REFRESH);
            timer10.Start();
            timerToggle.Tick += EVENTtimerToggleLanguage;
            timerToggle.Interval = new TimeSpan(0, 0, 0, 5);
            addStation("1_558", true);
        }

        private List<string> STATIONS = new List<string>();     // stations user selected
        private List<string> ALL_STATIONS = new List<string>(); // all stations for an agency
        private string[] ROUTE_NUMBER = new string[100];
        private string[] DESTINATION = new string[100];
        private string[] VEHICLE_ID = new string[100];
        private long[] DELAY = new long[100];
        private long[] ARRIVAL = new long[100];
        private int MAX_STATIONS;
        private const int SERVER_REFRESH = 10; // interval to ping the server
        private HttpClient CLIENT = new HttpClient();
        private TextBlock[] TEXT_ROUTE_NUMBER = new TextBlock[100];
        private TextBlock[] TEXT_DESTINATION = new TextBlock[100];
        private TextBlock[] TEXT_SMALL_TEXT = new TextBlock[100];
        private TextBlock[] TEXT_ARRIVAL = new TextBlock[100];
        private StackPanel[] STACK = new StackPanel[100];
        private DispatcherTimer timer1 = new DispatcherTimer();  // used to refresh all text blocks
        private DispatcherTimer timer10 = new DispatcherTimer(); // used to ping the server
        private DispatcherTimer timerToggle = new DispatcherTimer(); // used to alternate between Chinese / English text
        private bool CHINESE = false;
        private Random RANDOM = new Random(); // an instance of an rng

        /** Refreshes all text blocks. */
        private void EVENTrefreshText(object sender, object e)
        {
            int i = 0, j = 0;
            List<string> registeredRoutes = new List<string>();
            while (i < MAX_STATIONS && j < 100)
            {
                bool add = true;
                string route = "";
                if (toggleOneArrival.IsOn)
                {
                    route = ROUTE_NUMBER[j] + DESTINATION[j];
                    for (int k = 0; k < registeredRoutes.Count; k++)
                    {
                        if (registeredRoutes[k] == route)
                        {
                            add = false;
                            j++;
                        }
                    }
                }
                if (add)
                {
                    TEXT_ROUTE_NUMBER[i].Text = textCheck(ROUTE_NUMBER[j]);
                    TEXT_DESTINATION[i].Text = textCheck(DESTINATION[j]);
                    TEXT_SMALL_TEXT[i].Text = textCheck(ARRIVAL[j], VEHICLE_ID[j], DELAY[j]);
                    TEXT_ARRIVAL[i].Text = textCheck(ARRIVAL[j]);
                    i++;
                    registeredRoutes.Add(route);
                }
                if (!toggleOneArrival.IsOn)
                    j++;
            }
            while (i < MAX_STATIONS)
            {
                TEXT_ROUTE_NUMBER[i].Text = "";
                TEXT_DESTINATION[i].Text = "";
                TEXT_SMALL_TEXT[i].Text = "";
                TEXT_ARRIVAL[i].Text = "";
                i++;
            }
            if ((bool)checkAutoFontSize.IsChecked)
            {
                double height = ((Frame)Window.Current.Content).ActualHeight - 48;
                changeFontSize(0.7 * height / (MAX_STATIONS > 4 ? MAX_STATIONS : 4));
            }
        }

        /** Refreshes all data with the OneBusAway server. */
        private async void EVENTrefreshData(object sender, object e)
        {
            if (timer10.IsEnabled)
            {
                timer10.Stop();
                List<string> predicted = new List<string>();
                List<string> scheduled = new List<string>();
                List<string> routeNumber1 = new List<string>();
                List<string> routeNumber2 = new List<string>();
                List<string> destination = new List<string>();
                List<string> vehicleId = new List<string>();
                for (int i = 0; i < STATIONS.Count; i++)
                {
                    string url = "http://api.pugetsound.onebusaway.org/api/where/arrivals-and-departures-for-stop/"
                    + STATIONS[i] +
                    ".xml?key=09ce661b-1d78-4fc4-a77b-7c55455acadb&minutesBefore=1&minutesAfter=120&&includeReferences=false";
                    string webText = await websiteReader(url, true);
                    List<string> arrivalChunk = XMLReader(webText, "arrivalAndDeparture", MAX_STATIONS);
                    for (int j = 0; j < arrivalChunk.Count; j++)
                    {
                        predicted.Add(XMLReader(arrivalChunk[j], "predictedDepartureTime", 1)[0]);
                        scheduled.Add(XMLReader(arrivalChunk[j], "scheduledDepartureTime", 1)[0]);
                        routeNumber1.Add(XMLReader(arrivalChunk[j], "routeShortName", 1)[0]);
                        routeNumber2.Add(XMLReader(arrivalChunk[j], "routeLongName", 1)[0]);
                        destination.Add(XMLReader(arrivalChunk[j], "tripHeadsign", 1)[0]);
                        vehicleId.Add(XMLReader(arrivalChunk[j], "vehicleId", 1)[0]);
                    }
                }
                List<long> arrival = new List<long>();
                List<long> delay = new List<long>();
                for (int i = 0; i < predicted.Count; i++)
                {
                    long a = getLongFromString(predicted[i]);
                    if (a == 0)
                        a = getLongFromString(scheduled[i]);
                    delay.Add(getLongFromString(scheduled[i]) - getLongFromString(predicted[i]));
                    arrival.Add(a);
                }
                List<int> order = createSortOrder(arrival);
                List<string> routeNumber = new List<string>();
                for (int i = 0; i < routeNumber1.Count; i++)
                {
                    string s = routeNumber1[i];
                    if (s == "")
                        s = routeNumber2[i];
                    s = s.Replace("0E", "0X");
                    s = s.Replace("1E", "1X");
                    s = s.Replace("2E", "2X");
                    s = s.Replace("3E", "3X");
                    s = s.Replace("4E", "4X");
                    s = s.Replace("5E", "5X");
                    s = s.Replace("6E", "6X");
                    s = s.Replace("7E", "7X");
                    s = s.Replace("8E", "8X");
                    s = s.Replace("9E", "9X");
                    routeNumber.Add(s);
                }
                ARRIVAL = sortUsingGivenOrder(arrival, order);
                ROUTE_NUMBER = sortUsingGivenOrder(routeNumber, order);
                DESTINATION = sortUsingGivenOrder(destination, order);
                VEHICLE_ID = sortUsingGivenOrder(vehicleId, order);
                DELAY = sortUsingGivenOrder(delay, order);
                timer10.Start();
            }
        }

        private void EVENTbuttonAdd(object sender, RoutedEventArgs e)
        {
            addStation(textBox.Text);
            textBox.Text = "";
        }

        private void EVENTbuttonClear(object sender, RoutedEventArgs e)
        {
            STATIONS.Clear();
            richTextBox.Blocks.Clear();
            EVENTrefreshData(null, null);
        }

        private async void EVENTbuttonAddRandom(object sender, RoutedEventArgs e)
        {
            enableControls(false);
            // agencies:
            // 1 - King County Metro
            // 3 - Pierce Transit
            // 19 - Intercity Transit
            // 23 - City of Seattle (no stops)
            // 29 - Community Transit
            // 40 - Sound Transit
            // 95 - Washington State Ferries
            // 97 - Everett Transit
            // 98 - Seattle Children's Hostpital Shuttle
            // 99 - GO Transit
            // KMD - Kingcounty Marine Division (no stops)
            if (ALL_STATIONS.Count == 0)
            {
                string s = await websiteReader("http://api.pugetsound.onebusaway.org/api/where/stop-ids-for-agency/1.xml?key=09ce661b-1d78-4fc4-a77b-7c55455acadb&&includeReferences=false", true);
                s += await websiteReader("http://api.pugetsound.onebusaway.org/api/where/stop-ids-for-agency/3.xml?key=09ce661b-1d78-4fc4-a77b-7c55455acadb&&includeReferences=false", true);
                s += await websiteReader("http://api.pugetsound.onebusaway.org/api/where/stop-ids-for-agency/19.xml?key=09ce661b-1d78-4fc4-a77b-7c55455acadb&&includeReferences=false", true);
                s += await websiteReader("http://api.pugetsound.onebusaway.org/api/where/stop-ids-for-agency/29.xml?key=09ce661b-1d78-4fc4-a77b-7c55455acadb&&includeReferences=false", true);
                s += await websiteReader("http://api.pugetsound.onebusaway.org/api/where/stop-ids-for-agency/40.xml?key=09ce661b-1d78-4fc4-a77b-7c55455acadb&&includeReferences=false", true);
                s += await websiteReader("http://api.pugetsound.onebusaway.org/api/where/stop-ids-for-agency/95.xml?key=09ce661b-1d78-4fc4-a77b-7c55455acadb&&includeReferences=false", true);
                s += await websiteReader("http://api.pugetsound.onebusaway.org/api/where/stop-ids-for-agency/97.xml?key=09ce661b-1d78-4fc4-a77b-7c55455acadb&&includeReferences=false", true);
                s += await websiteReader("http://api.pugetsound.onebusaway.org/api/where/stop-ids-for-agency/98.xml?key=09ce661b-1d78-4fc4-a77b-7c55455acadb&&includeReferences=false", true);
                s += await websiteReader("http://api.pugetsound.onebusaway.org/api/where/stop-ids-for-agency/99.xml?key=09ce661b-1d78-4fc4-a77b-7c55455acadb&&includeReferences=false", true);
                ALL_STATIONS = XMLReader(s, "string");
            }
            int a = RANDOM.Next(0, ALL_STATIONS.Count - 1);
            addStation(ALL_STATIONS[a]);
        }

        private void EVENTbuttonAddPreset1(object sender, RoutedEventArgs e)
        {
            addStation("1_16101", true);
            addStation("1_16102", true);
            addStation("1_16103", true);
            addStation("1_16104", true);
            addStation("29_2875", true);
            addStation("1_16106", true);
            addStation("29_2876", true);
            addStation("1_16100", true);
            addStation("1_16112", true);
        }

        private void EVENTbuttonAddPreset2(object sender, RoutedEventArgs e)
        {
            addStation("1_41255", true);
            addStation("1_18210", true);
            addStation("1_18630", true);
        }

        private void EVENTbuttonAddPreset3(object sender, RoutedEventArgs e)
        {
            addStation("1_29247", true);
            addStation("1_29405", true);
            addStation("1_25765", true);
            addStation("1_99604", true);
            addStation("3_19473", true);
        }

        private void EVENTbuttonAddPreset4(object sender, RoutedEventArgs e)
        {
            addStation("1_1120", true);
            addStation("1_1121", true);
            addStation("1_700", true);
            addStation("1_570", true);
            addStation("1_430", true);
            addStation("1_431", true);
            addStation("1_433", true);
            addStation("1_1108", true);
            addStation("1_1109", true);
            addStation("1_1110", true);
            addStation("1_760", true);
            addStation("1_280", true);
            addStation("1_575", true);
            addStation("1_578", true);
        }

        private void EVENTbuttonRefresh(object sender, RoutedEventArgs e)
        {
            EVENTrefreshData(null, null);
        }

        private void EVENTtimerToggleLanguage(object sender, object e)
        {
            CHINESE = !CHINESE;
        }

        /** Reads the raw text from a website from a given url. */
        private async Task<string> websiteReader(string url, bool forceSuccess)
        {
            string input = "", message = "";
            int i = 0;
            while (!input.Contains("<code>200</code>") && (forceSuccess || i < 5))
            {
                await Task.Delay(500);
                try
                {
                    CLIENT.DefaultRequestHeaders.IfModifiedSince = new DateTimeOffset(DateTime.Now);
                    input = await CLIENT.GetStringAsync(new Uri(url));
                    errorText.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 0, 0));
                    message = "Data last updated: ";
                }
                catch (Exception)
                {
                    errorText.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
                    message = "Failed to update! ";
                }
                errorText.Text = message + DateTime.Now.ToString();
                i++;
            }
            return input;
        }

        /** Reads the first n occurances enclosed by the specified tag. */
        private List<string> XMLReader(string text, string tag, int n = -1)
        {
            List<string> returnList = new List<string>();
            int i = 0;
            while (text.Contains("<" + tag + ">") && text.Contains("</" + tag + ">")
                && (n <= 0 || i < n))
            {
                int tagLength = tag.Length;
                int begin = text.IndexOf("<" + tag + ">") + tagLength + 2;
                int end = text.IndexOf("</" + tag + ">");
                string s = text.Substring(begin, end - begin);
                s = s.Replace("amp;", "");
                text = text.Substring(end + tagLength + 4);
                returnList.Add(s);
                i++;
            }
            if (n == 1 && returnList.Count == 0)
                returnList.Add("");
            return returnList;
        }

        /** Converts a string of numbers to a long. */
        private long getLongFromString(string input)
        {
            long a = 0;
            long j = 1;
            for (int i = input.Length - 1; i >= 0; i--)
            {
                try
                {
                    char c = input.ToCharArray(i, 1)[0];
                    a += (c - '0') * j;
                    j *= 10;
                }
                catch (Exception) { }
            }
            return a;
        }

        List<int> createSortOrder(List<long> item)
        {
            List<int> order = new List<int>();
            for (int i = 0; i < item.Count; i++)
            {
                long current = item[i];
                int k = 0;
                for (int j = 0; j < item.Count; j++)
                {
                    if (item[j] < current)
                        k++;
                    if (item[j] == current && j < i)
                        k++;
                }
                order.Add(k);
            }
            return order;
        }

        T[] sortUsingGivenOrder<T>(List<T> item, List<int> order)
        {
            T[] sorted = new T[100];
            for (int i = 0; i < order.Count; i++)
            {
                int position = order[i];
                if (position < 100 && i < item.Count)
                    sorted[position] = item[i];
            }
            return sorted;
        }

        /** Sets text to correct case and translates if needed. */
        private string textCheck(string text)
        {
            if (text == null)
                text = "";
            if (CHINESE)
                text = translate(text);
            if ((bool)radioL2.IsChecked)
                text = text.ToLower();
            if ((bool)radioL3.IsChecked)
                text = text.ToUpper();
            return text;
        }

        /** Overload for arrival seconds */
        private string textCheck(long arrival)
        {
            if (arrival == 0)
                return "";
            double timeDiff = arrival - (DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds;
            return textCheck(timeDiffToString(timeDiff, 1));
        }

        /** Overload for arrival clock time */
        private string textCheck(long arrival, string vehicleId, double delay)
        {
            if (arrival == 0)
                return "";
            string timeString = (new DateTime(1970, 1, 1) +
                TimeSpan.FromSeconds(arrival / 1000D)).ToLocalTime().ToString();
            timeString = timeString.Substring(timeString.IndexOf(" ") + 1);
            if (CHINESE)
            {
                bool am = timeString.Substring(timeString.IndexOf(" ")) == " AM";
                timeString = (am ? "上午 " : "下午 ") + timeString.Remove(timeString.IndexOf(" "));
            }
            return textCheck(" " + timeString + "     " + timeDiffToString(delay, 2) + "     " + vehicleId);
        }

        private string timeDiffToString(double timeDiff, int options = 0)
        {
            double time = Math.Abs(timeDiff) / 60000D;
            if (options == 1)
            {
                if (timeDiff < 0)
                    return CHINESE ? "已離開" : "Gone";
                else if (Math.Round(time) <= 1)
                    return CHINESE ? "即將到達" : "Arriving ";
            }
            string s;
            if (toggleSeconds.IsOn)
            {
                double min = Math.Floor(time);
                double sec = Math.Floor((time - min) * 60);
                s = min.ToString() + (sec < 10 ? ":0" : ":") + sec.ToString();
            }
            else
                s = Math.Round(time).ToString() + (CHINESE ? "分" : "min");
            if (options == 2)
            {
                if (timeDiff > 100000000)
                    return "";
                if (Math.Round(time) <= 1)
                    return CHINESE ? "準時到達" : "On time";
                if (CHINESE)
                    s = (timeDiff < 0 ? "延遲" : "提早") + s;
                else
                    s = s + (timeDiff < 0 ? " delay" : " early");
            }
            return s;
        }

        private async void addStation(string station, bool forceSuccess = false)
        {
            enableControls(false);
            if (!station.Contains("_"))
                station = "1_" + station;
            string url = "http://api.pugetsound.onebusaway.org/api/where/stop/"
                + station +
                ".xml?key=09ce661b-1d78-4fc4-a77b-7c55455acadb&&includeReferences=false";
            string text = "";
            text = await websiteReader(url, forceSuccess);
            List<string> stop = XMLReader(text, "name", 1);
            if (stop[0] != "")
            {
                Paragraph paragraph = new Paragraph();
                Run run = new Run();
                run.Text = station + " (" + stop[0] + ")";
                paragraph.Inlines.Add(run);
                richTextBox.Blocks.Add(paragraph);
                STATIONS.Add(station);
                EVENTrefreshData(null, null);
            }
            else
            {
                ContentDialog errorDialog = new ContentDialog
                {
                    Title = "Could not fetch stop!",
                    Content = "Please try again.",
                    CloseButtonText = "OK",
                    PrimaryButtonText = "Find more stops",
                    DefaultButton = ContentDialogButton.Close
                };
                try
                {
                    ContentDialogResult result = await errorDialog.ShowAsync();
                    if (result == ContentDialogResult.Primary)
                    {
                        await Launcher.LaunchUriAsync(new Uri("http://pugetsound.onebusaway.org/where/standard/"));
                    }
                }
                catch { }
            }
            enableControls(true);
        }

        private void EVENTtextBoxKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter && textBox.Text != "")
                EVENTbuttonAdd(null, null);
        }

        private void EVENTcheckBoxLanguageChanged(object sender, RoutedEventArgs e)
        {
            if ((bool)checkChinese.IsChecked && (bool)checkEnglish.IsChecked)
            {
                timerToggle.Start();
                CHINESE = !CHINESE;
                checkChinese.IsEnabled = true;
                checkEnglish.IsEnabled = true;
            }
            else
            {
                timerToggle.Stop();
                CHINESE = (bool)checkChinese.IsChecked;
                if (CHINESE)
                    checkChinese.IsEnabled = false;
                else
                    checkEnglish.IsEnabled = false;
            }
        }

        private void EVENTcheckBoxFontSizeChanged(object sender, RoutedEventArgs e)
        {
            bool auto = (bool)checkAutoFontSize.IsChecked;
            sliderFontSize.IsEnabled = !auto;
            if (!auto)
                changeFontSize(sliderFontSize.Value);
        }

        private void EVENTflyoutOpened(object sender, object e)
        {
            textBox.Focus(FocusState.Keyboard);
        }

        private void EVENTsliderMaxArrivalsChanged(object sender, Windows.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            MAX_STATIONS = (int)sliderMaxArrivals.Value;
            EVENTrefreshData(null, null);
        }

        private void EVENTsliderFontSizeValueChanged(object sender, Windows.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            changeFontSize(sliderFontSize.Value);
        }

        private void enableControls(bool enable)
        {
            buttonAdd.IsEnabled = enable;
            buttonAddRandom.IsEnabled = enable;
            buttonClear.IsEnabled = enable;
            buttonAddPreset.IsEnabled = enable;
            buttonPreset1.IsEnabled = enable;
            buttonPreset2.IsEnabled = enable;
            buttonPreset3.IsEnabled = enable;
            buttonPreset4.IsEnabled = enable;
            textBox.IsEnabled = enable;
            sliderMaxArrivals.IsEnabled = enable;
            progressBar.Visibility = enable ? Visibility.Collapsed : Visibility.Visible;
            textBox.Focus(FocusState.Keyboard);
        }

        private void changeFontSize(double size)
        {
            for (int i = 0; i < 100; i++)
            {
                try
                {
                    if (i < MAX_STATIONS)
                    {
                        TEXT_ROUTE_NUMBER[i].FontSize = size;
                        TEXT_DESTINATION[i].FontSize = 0.75 * size;
                        TEXT_SMALL_TEXT[i].FontSize = 0.25 * size;
                        TEXT_ARRIVAL[i].FontSize = size;
                    }
                    else
                    {
                        TEXT_ROUTE_NUMBER[i].Text = "";
                        TEXT_DESTINATION[i].Text = "";
                        TEXT_SMALL_TEXT[i].Text = "";
                        TEXT_ARRIVAL[i].Text = "";
                    }
                }
                catch { }
            }
        }

        private string translate(string s)
        {
            s = this.controller.Gettext(s.ToLower());

            // 1
            if (!s.Contains("e 綫") && !s.Contains("e1"))
            {
                s = s.Replace(" n", " 北");
                s = s.Replace(" e", " 東");
                s = s.Replace(" s", " 南");
                s = s.Replace(" w", " 西");
                s = s.Replace("n ", "北 ");
                s = s.Replace("e ", "東 ");
                s = s.Replace("s ", "南 ");
                s = s.Replace("w ", "西 ");
            }

            return s.Trim().ToUpper();
        }
    }
}
